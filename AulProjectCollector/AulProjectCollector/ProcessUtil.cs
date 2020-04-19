using ExoUtil;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AulProjectCollector
{
    public class ProcessUtil
    {
        public double CurrentProgress { get; set; }
        private SortedDictionary<string, string> FontMap { get; set; }

        public ProcessUtil()
        {
            CurrentProgress = 0;
            FontMap = new FontLoader().LoadSystemFonts();
        }

        public string Process(string[] files)
        {
            CurrentProgress = 0;
            string message;

            if (files.Length != 0)
            {
                string fstFile = files[0];

                if (fstFile.EndsWith(".exo"))
                {
                    List<string> exos = new List<string>();
                    foreach(string file in files)
                    {
                        if (file.EndsWith(".exo"))
                            exos.Add(file);
                    }
                    message = Archive(exos.ToArray());
                }
                else if (fstFile.EndsWith(".auz") || fstFile.EndsWith(".zip"))
                {
                    message = ReleaseArchive(fstFile);
                }
                else
                {
                    message = "不支持的格式";
                }
            }
            else
            {
                message = "输入为空";
            }

            CurrentProgress = -1;

            return message;
        }

        private string Archive(string[] exoFiles)
        {
            Console.WriteLine("[{0}] [Info] Start archiving - {1}", GetType().Name, exoFiles.Length);
            StringBuilder messageBuilder = new StringBuilder();

            // Exo parser
            List<Exo> exos = new List<Exo>();
            foreach(string exoFile in exoFiles)
            {
                Exo exo = new Exo(exoFile, Encoding.Default);
                exos.Add(exo);
            }
            

            // Create hash set for file paths
            HashSet<string> filePathsSet = new HashSet<string>();
            HashSet<string> fontNamesSet = new HashSet<string>();
            foreach(Exo exo in exos)
            {
                foreach (Exo.Exedit.Item item in exo.MainExedit.Items)
                {
                    foreach (Exo.Exedit.Item.SubItem subItem in item.SubItems)
                    {
                        // Find files
                        if (subItem.Name == "Image file" || subItem.Name == "Video file" || subItem.Name == "Audio file")
                        {
                            if (subItem.Params.ContainsKey("file"))
                            {
                                string filePath = subItem.Params["file"];
                                if (filePathsSet.Add(filePath))
                                {
                                    Console.WriteLine("[{0}] [Info] File found in the project: {1}", GetType().Name, filePath);
                                }
                            }
                        }

                        // Find fonts
                        if (subItem.Name == "Text")
                        {
                            if (subItem.Params.ContainsKey("file"))
                            {
                                string font = subItem.Params["font"];
                                if (fontNamesSet.Add(font))
                                {
                                    Console.WriteLine("[{0}] [Info] Font found in the project: {1}", GetType().Name, font);
                                }
                            }
                        }
                    }
                }
            }

            // Create list for FileInfos & calc total size
            long totalLength = 0;
            List<FileInfo> fileInfosList = new List<FileInfo>();
            foreach (string filePath in filePathsSet)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    fileInfosList.Add(fileInfo);
                    totalLength += fileInfo.Length;
                    Console.WriteLine("[{0}] [Info] Confirm file exists: {1}", GetType().Name, fileInfo.FullName);
                }
                else
                {
                    messageBuilder.AppendFormat("未找到文件: {0}\n", fileInfo.FullName);
                    Console.WriteLine("[{0}] [Warn] File not found: {1}", GetType().Name, fileInfo.FullName);
                }
            }
            List<FileInfo> fontFileInfosList = new List<FileInfo>();
            foreach (string fontName in fontNamesSet)
            {
                if (FontMap.ContainsKey(fontName))
                {
                    FileInfo fileInfo = new FileInfo(FontMap[fontName]);
                    if (fileInfo.Exists)
                    {
                        fontFileInfosList.Add(fileInfo);
                        totalLength += fileInfo.Length;
                        Console.WriteLine("[{0}] [Info] Confirm font exists: {1} - {2}", GetType().Name, fontName, fileInfo.FullName);
                    }
                    else
                    {
                        messageBuilder.AppendFormat("未找到字体文件: {0} - {1}\n", fontName, fileInfo.FullName);
                        Console.WriteLine("[{0}] [Warn] Font file not found: {1} - {2}", GetType().Name, fontName, fileInfo.FullName);
                    }
                }
                else
                {
                    messageBuilder.AppendFormat("未找到字体: {0}\n", fontName);
                    Console.WriteLine("[{0}] [Info] Font not found: {1}", GetType().Name, fontName);
                }

            }


            // Collect files and create archive
            Dictionary<string, string> archivedFileMap = new Dictionary<string, string>();
            string archivePath;
            if (exoFiles.Length == 1)
                archivePath = Path.Combine(Path.GetDirectoryName(exoFiles[0]), string.Format("{0}.auz", Path.GetFileNameWithoutExtension(exoFiles[0])));
            else
                archivePath = Path.Combine(Path.GetDirectoryName(exoFiles[0]), string.Format("{0}.auz", Path.GetDirectoryName(exoFiles[0]).Split('\\').Last()));
            using (FileStream archiveStream = new FileStream(archivePath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Create))
                {
                    // Archive files
                    long currentLength = 0;
                    byte[] buffer = new byte[1024 * 1024];
                    foreach (FileInfo file in fileInfosList)
                    {
                        string archivedPath = GetArchivedPath(file.FullName);
                        if (archivedFileMap.ContainsValue(archivedPath))
                        {
                            string fullFileNameWithoutExtension = Path.Combine(Path.GetDirectoryName(archivedPath), Path.GetFileNameWithoutExtension(archivedPath));
                            string extension = Path.GetExtension(archivedPath);
                            long i = 0;
                            do
                            {
                                i++;
                                archivedPath = string.Format("{0} (1){2}", fullFileNameWithoutExtension, i, extension);
                            }
                            while (archivedFileMap.ContainsValue(archivedPath));
                        }
                        archivedFileMap[file.FullName] = archivedPath;
                        ZipArchiveEntry archiveEntry = archive.CreateEntry(archivedPath, CompressionLevel.Optimal);
                        using (Stream archiveEntryStream = archiveEntry.Open())
                        {
                            using (FileStream sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                            {
                                while (sourceStream.Position != sourceStream.Length)
                                {
                                    int readLength = sourceStream.Read(buffer, 0, buffer.Length);
                                    archiveEntryStream.Write(buffer, 0, readLength);
                                    currentLength += readLength;

                                    double progress = (double)currentLength / totalLength;
                                    Console.WriteLine("[{0}] [Info] Writting files {1}/{2} ({3})", GetType().Name, currentLength, totalLength, progress);
                                    CurrentProgress = progress;
                                }
                                Console.WriteLine("[{0}] [Info] File archived: {1} - {2}", GetType().Name, file.FullName, archivedPath);
                            }
                        }
                    }
                    foreach (FileInfo file in fontFileInfosList)
                    {
                        string archivedPath = Path.Combine("Fonts", file.Name);
                        ZipArchiveEntry archiveEntry = archive.CreateEntry(archivedPath);
                        using (Stream archiveEntryStream = archiveEntry.Open())
                        {
                            using (FileStream sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                            {
                                while (sourceStream.Position != sourceStream.Length)
                                {
                                    int readLength = sourceStream.Read(buffer, 0, buffer.Length);
                                    archiveEntryStream.Write(buffer, 0, readLength);
                                    currentLength += readLength;

                                    double progress = (double)currentLength / totalLength;
                                    Console.WriteLine("[{0}] [Info] Writting fonts {1}/{2} ({3})", GetType().Name, currentLength, totalLength, progress);
                                    CurrentProgress = progress;
                                }
                                Console.WriteLine("[{0}] [Info] Font archived: {1} - {2}", GetType().Name, file.FullName, archivedPath);
                            }
                        }
                    }

                    // Generate Exo
                    HashSet<string> archivedExoSet = new HashSet<string>();
                    for (int i = 0; i < exoFiles.Length; i++)
                    {
                        string exoFile = exoFiles[i];
                        string archivedExoPath = Path.GetFileName(exoFile);
                        if (archivedExoSet.Contains(archivedExoPath))
                        {
                            string fullFileNameWithoutExtension = Path.Combine(Path.GetDirectoryName(archivedExoPath), Path.GetFileNameWithoutExtension(archivedExoPath));
                            string extension = Path.GetExtension(archivedExoPath);
                            long j = 0;
                            do
                            {
                                j++;
                                archivedExoPath = string.Format("{0} (1){2}", fullFileNameWithoutExtension, j, extension);
                            }
                            while (archivedExoSet.Contains(archivedExoPath));
                        }
                        archivedExoSet.Add(archivedExoPath);

                        Exo exo = exos[i];
                        foreach (Exo.Exedit.Item item in exo.MainExedit.Items)
                        {
                            foreach (Exo.Exedit.Item.SubItem subItem in item.SubItems)
                            {
                                // Find files
                                if (subItem.Name == "Image file" || subItem.Name == "Video file" || subItem.Name == "Audio file")
                                {
                                    if (subItem.Params.ContainsKey("file"))
                                    {
                                        string filePath = subItem.Params["file"];
                                        if (archivedFileMap.ContainsKey(filePath))
                                        {
                                            string archivedPath = archivedFileMap[filePath];
                                            subItem.Params["file"] = string.Format(".\\{0}", archivedPath);
                                        }
                                    }
                                }
                            }
                        }
                        ZipArchiveEntry exoArchiveEntry = archive.CreateEntry(archivedExoPath);
                        using (Stream archiveEntryStream = exoArchiveEntry.Open())
                        {
                            using (StreamWriter streamWriter = new StreamWriter(archiveEntryStream, Encoding.Default))
                            {
                                streamWriter.Write(exo.ToString());
                                Console.WriteLine("[{0}] [Info] Exo archived: {1} - {2}", GetType().Name, exoFile, archivedExoPath);
                            }

                        }

                    }
                }
            }

            messageBuilder.AppendFormat("共处理{0}个Exo存档，已归档{1}/{2}个素材，{3}/{4}个字体。\n{5}", exoFiles.Length, fileInfosList.Count, filePathsSet.Count, fontNamesSet.Count, fontFileInfosList.Count, archivePath);

            Console.WriteLine("[{0}] [Info] Archiving finished - {1}", GetType().Name, exoFiles.Length);

            return messageBuilder.ToString();
        }

        private string ReleaseArchive(string archiveFile)
        {
            Console.WriteLine("[{0}] [Info] Start releasing archive - {1}", GetType().Name, archiveFile);
            bool isTileRelease = MessageBox.Show("是否按文件夹释放素材", "释放素材", MessageBoxButton.YesNo) == MessageBoxResult.No;

            StringBuilder messageBuilder = new StringBuilder();
            uint fileCount = 0;
            uint exoCount = 0;
            string exoDirectory = Path.Combine(Path.GetDirectoryName(archiveFile), Path.GetFileNameWithoutExtension(archiveFile));

            using (FileStream archiveStream = new FileStream(archiveFile, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
                {
                    // Calc total length
                    long totalLength = 0;
                    foreach (ZipArchiveEntry archiveEntry in archive.Entries)
                    {
                        if (archiveEntry.FullName.EndsWith(".exo"))
                            continue;

                        totalLength += archiveEntry.Length;
                    }

                    // Release files
                    long currentLength = 0;
                    byte[] buffer = new byte[1024 * 1024];
                    foreach (ZipArchiveEntry archiveEntry in archive.Entries)
                    {
                        if (archiveEntry.FullName.EndsWith(".exo"))
                            continue;

                        string releaseDirectory;
                        string releaseFullFileName;
                        if (!archiveEntry.FullName.StartsWith("Fonts\\") && isTileRelease)
                        {
                            releaseDirectory = Path.Combine(Path.GetDirectoryName(archiveFile), Path.GetFileNameWithoutExtension(archiveFile));
                            releaseFullFileName = Path.Combine(releaseDirectory, archiveEntry.FullName.Replace('\\', '_'));
                        }
                        else
                        {
                            releaseDirectory = Path.Combine(Path.GetDirectoryName(archiveFile), Path.GetFileNameWithoutExtension(archiveFile), Path.GetDirectoryName(archiveEntry.FullName));
                            releaseFullFileName = Path.Combine(releaseDirectory, archiveEntry.Name);
                        }
                        Directory.CreateDirectory(releaseDirectory);

                        using (Stream archiveEntryStream = archiveEntry.Open())
                        {
                            using (FileStream releaseStream = new FileStream(releaseFullFileName, FileMode.Create))
                            {
                                int position = 0;
                                while (position != archiveEntry.Length)
                                {
                                    int readLength = archiveEntryStream.Read(buffer, 0, buffer.Length);
                                    releaseStream.Write(buffer, 0, readLength);
                                    position += readLength;
                                    currentLength += readLength;
                                    double progress = (double)currentLength / totalLength;
                                    Console.WriteLine("[{0}] [Info] Releasing files {1}/{2} ({3})",GetType().Name, currentLength, totalLength, progress);
                                    CurrentProgress = progress;
                                }

                            }
                        }
                        fileCount++;
                    }

                    // Generate Exo
                    foreach (ZipArchiveEntry archiveEntry in archive.Entries)
                    {
                        if (!archiveEntry.FullName.EndsWith(".exo"))
                            continue;

                        ZipArchiveEntry exoArchiveEntry = archiveEntry;
                        using (Stream exoStream = exoArchiveEntry.Open())
                        {
                            Exo exo = new Exo(exoStream, Encoding.Default);
                            foreach (Exo.Exedit.Item item in exo.MainExedit.Items)
                            {
                                foreach (Exo.Exedit.Item.SubItem subItem in item.SubItems)
                                {
                                    if (subItem.Name == "Image file" || subItem.Name == "Video file" || subItem.Name == "Audio file")
                                    {
                                        if (subItem.Params.ContainsKey("file"))
                                        {
                                            string archivedPath = subItem.Params["file"];
                                            if (archivedPath.StartsWith(".\\"))
                                            {
                                                if (isTileRelease)
                                                    subItem.Params["file"] = Path.Combine(Path.GetDirectoryName(archiveFile), Path.GetFileNameWithoutExtension(archiveFile), archivedPath.Substring(".\\".Length).Replace('\\', '_'));
                                                else
                                                    subItem.Params["file"] = Path.Combine(Path.GetDirectoryName(archiveFile), Path.GetFileNameWithoutExtension(archiveFile), archivedPath.Substring(".\\".Length));
                                            }
                                        }
                                    }
                                }
                            }

                            if (!Directory.Exists(exoDirectory))
                                Directory.CreateDirectory(exoDirectory);
                            string exoFullFileName = Path.Combine(exoDirectory, exoArchiveEntry.Name);
                            using (StreamWriter streamWriter = new StreamWriter(exoFullFileName, false, Encoding.Default))
                            {
                                streamWriter.Write(exo.ToString());
                            }
                        }
                        Console.WriteLine("[{0}] [Info] Exo generated - {1}", GetType().Name, exoArchiveEntry.Name);
                        exoCount++;
                    }
                }
            }

            messageBuilder.AppendFormat("共释放{0}个素材，生成{1}个Exo存档\n{2}", fileCount, exoCount, exoDirectory);
            Console.WriteLine("[{0}] [Info] Archive releasing finished - {1}", GetType().Name, archiveFile);
            return messageBuilder.ToString();
        }

        private string GetArchivedPath(string onDiskPath)
        {
            string directory = Path.GetDirectoryName(onDiskPath);
            string folderName = directory.Split('\\').Last();
            string fileName = Path.GetFileName(onDiskPath);
            return string.Format("{0}\\{1}", folderName, fileName);
        }

    }
}
