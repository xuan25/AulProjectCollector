using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExoUtil
{
    public class Exo
    {
        public Exedit MainExedit;
        public enum Level { Exo, Exedit, Item, SubItem };

        public Exo(string filename, Encoding encoding)
        {
            using (StreamReader streamReader = new StreamReader(filename, encoding))
            {
                Init(streamReader);
            }
        }

        public Exo(Stream stream, Encoding encoding)
        {
            using (StreamReader streamReader = new StreamReader(stream, encoding))
            {
                Init(streamReader);
            }
        }

        private void Init(StreamReader streamReader)
        {
            Level level = Level.Exo;
            while (streamReader.Peek() != -1)
            {
                string line = streamReader.ReadLine();
                switch (level)
                {
                    case Level.Exo:
                        if (line == "[exedit]")
                        {
                            MainExedit = new Exedit();
                            level = Level.Exedit;
                        }
                        break;
                    case Level.Exedit:
                        if (line.Contains('='))
                        {
                            int equalMarkIndex = line.IndexOf('=');
                            string key = line.Substring(0, equalMarkIndex);
                            string value = line.Substring(equalMarkIndex + 1);

                            switch (key)
                            {
                                case "width":
                                    MainExedit.Width = uint.Parse(value);
                                    break;
                                case "height":
                                    MainExedit.Height = uint.Parse(value);
                                    break;
                                case "rate":
                                    MainExedit.Rate = uint.Parse(value);
                                    break;
                                case "scale":
                                    MainExedit.Scale = uint.Parse(value);
                                    break;
                                case "length":
                                    MainExedit.Length = uint.Parse(value);
                                    break;
                                case "audio_rate":
                                    MainExedit.AudioRate = uint.Parse(value);
                                    break;
                                case "audio_ch":
                                    MainExedit.AudioChannel = uint.Parse(value);
                                    break;
                            }
                        }
                        else if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            uint index = uint.Parse(line.Substring(1, line.Length - 2));
                            MainExedit.Items.Add(new Exedit.Item(index));
                            level = Level.Item;
                        }
                        break;
                    case Level.Item:
                        if (line.Contains('='))
                        {
                            int equalMarkIndex = line.IndexOf('=');
                            string key = line.Substring(0, equalMarkIndex);
                            string value = line.Substring(equalMarkIndex + 1);
                            MainExedit.Items.Last().Params.Add(key, uint.Parse(value));
                        }
                        else if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            if (line.Contains("."))
                            {
                                int dotIndex = line.IndexOf('.');
                                uint majorIndex = uint.Parse(line.Substring(1, dotIndex - 1));
                                uint subIndex = uint.Parse(line.Substring(dotIndex + 1, line.Length - dotIndex - 2));
                                MainExedit.Items.Last().SubItems.Add(new Exedit.Item.SubItem(majorIndex, subIndex));
                                level = Level.SubItem;
                            }
                            else
                            {
                                uint index = uint.Parse(line.Substring(1, line.Length - 2));
                                MainExedit.Items.Add(new Exedit.Item(index));
                                level = Level.Item;
                            }
                        }
                        break;
                    case Level.SubItem:
                        if (line.Contains('='))
                        {
                            int equalMarkIndex = line.IndexOf('=');
                            string key = line.Substring(0, equalMarkIndex);
                            string value = line.Substring(equalMarkIndex + 1);

                            switch (key)
                            {
                                case "_name":
                                    MainExedit.Items.Last().SubItems.Last().Name = value;
                                    break;
                                default:
                                    MainExedit.Items.Last().SubItems.Last().Params.Add(key, value);
                                    break;
                            }
                        }
                        else if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            if (line.Contains("."))
                            {
                                int dotIndex = line.IndexOf('.');
                                uint majorIndex = uint.Parse(line.Substring(1, dotIndex - 1));
                                uint subIndex = uint.Parse(line.Substring(dotIndex + 1, line.Length - dotIndex - 2));
                                MainExedit.Items.Last().SubItems.Add(new Exedit.Item.SubItem(majorIndex, subIndex));
                                level = Level.SubItem;
                            }
                            else
                            {
                                uint index = uint.Parse(line.Substring(1, line.Length - 2));
                                MainExedit.Items.Add(new Exedit.Item(index));
                                level = Level.Item;
                            }
                        }
                        break;
                }

            }

        }

        public override string ToString()
        {
            return MainExedit.ToString();
        }

        public class Exedit
        {
            public uint Width { get; set; }
            public uint Height { get; set; }
            public uint Rate { get; set; }
            public uint Scale { get; set; }
            public uint Length { get; set; }
            public uint AudioRate { get; set; }
            public uint AudioChannel { get; set; }
            public List<Item> Items { get; set; }

            public Exedit()
            {
                Items = new List<Item>();
            }

            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("[exedit]\r\n");
                stringBuilder.AppendFormat("width={0}\r\n", Width);
                stringBuilder.AppendFormat("height={0}\r\n", Height);
                stringBuilder.AppendFormat("rate={0}\r\n", Rate);
                stringBuilder.AppendFormat("scale={0}\r\n", Scale);
                stringBuilder.AppendFormat("length={0}\r\n", Length);
                stringBuilder.AppendFormat("audio_rate={0}\r\n", AudioRate);
                stringBuilder.AppendFormat("audio_ch={0}\r\n", AudioChannel);
                foreach (Item item in Items)
                {
                    stringBuilder.Append(item.ToString());
                }
                return stringBuilder.ToString();
            }

            public class Item
            {
                public uint Index { get; set; }
                public Dictionary<string, uint> Params;
                public List<SubItem> SubItems { get; set; }

                public Item(uint index)
                {
                    Index = index;
                    Params = new Dictionary<string, uint>();
                    SubItems = new List<SubItem>();
                }

                public override string ToString()
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("[{0}]\r\n", Index);

                    foreach(KeyValuePair<string, uint> keyValuePair in Params)
                    {
                        stringBuilder.AppendFormat("{0}={1}\r\n", keyValuePair.Key, keyValuePair.Value);
                    }
                    foreach (SubItem subItem in SubItems)
                    {
                        stringBuilder.Append(subItem.ToString());
                    }
                    return stringBuilder.ToString();
                }

                public class SubItem
                {
                    public uint MajorIndex;
                    public uint SubIndex;
                    public string Name { get; set; }
                    public Dictionary<string, string> Params;

                    public SubItem(uint majorIndex, uint subIndex)
                    {
                        MajorIndex = majorIndex;
                        SubIndex = subIndex;
                        Params = new Dictionary<string, string>();
                    }

                    public override string ToString()
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendFormat("[{0}.{1}]\r\n", MajorIndex, SubIndex);
                        stringBuilder.AppendFormat("_name={0}\r\n", Name);
                        foreach (KeyValuePair<string, string> keyValuePair in Params)
                        {
                            stringBuilder.AppendFormat("{0}={1}\r\n", keyValuePair.Key, keyValuePair.Value);
                        }
                        return stringBuilder.ToString();
                    }
                }
            }
        }
    }
}
