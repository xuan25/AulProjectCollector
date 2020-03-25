using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AulProjectCollector
{
    public class FontLoader
    {
        public SortedDictionary<string, string> LoadSystemFonts()
        {
            SortedDictionary<string, string> fontMap = new SortedDictionary<string, string>();

            using (RegistryKey localMachineKeySub = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Fonts", false))
            {
                string[] fontFullNames = localMachineKeySub.GetValueNames();
                foreach (string fontFullName in fontFullNames)
                {
                    string fontPath = localMachineKeySub.GetValue(fontFullName).ToString();
                    if (!Path.IsPathRooted(fontPath))
                        fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontPath);

                    PrivateFontCollection fontCollection = new PrivateFontCollection();
                    fontCollection.AddFontFile(fontPath);
                    foreach (FontFamily fontFamily in fontCollection.Families)
                    {
                        fontMap[fontFamily.Name] = fontPath;
                        Console.WriteLine("[{0}] [Info] Font loaded : {1} - {2}", GetType().Name, fontFamily.Name, fontPath);
                    }
                }
            }

            using (RegistryKey currentUserKeySub = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Fonts", false))
            {
                string[] userFontFullNames = currentUserKeySub.GetValueNames();
                foreach (string userFontFullName in userFontFullNames)
                {
                    string fontPath = currentUserKeySub.GetValue(userFontFullName).ToString();

                    PrivateFontCollection fontCollection = new PrivateFontCollection();
                    fontCollection.AddFontFile(fontPath);
                    foreach (FontFamily fontFamily in fontCollection.Families)
                    {
                        fontMap[fontFamily.Name] = fontPath;
                        Console.WriteLine("[{0}] [Info] Font loaded : {1} - {2}", GetType().Name, fontFamily.Name, fontPath);
                    }
                }
            }

            return fontMap;
        }
    }
}
