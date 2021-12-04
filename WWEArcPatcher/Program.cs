using Ganss.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace WWEArcPatcher
{
    class CRCTable
    {
        [Column("File")]
        public string File { get; set; }
        [Column("Offset")]
        public Int32 Offset { get; set; }
        [Column("OffsetSize")]
        public int OffsetSize { get; set; }
        [Column("CRC")]
        public string CRC { get; set; }
    }

    static class Program
    {
        static List<CRCTable> table = new List<CRCTable>();

        static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("WWE ARC Tool @Deftones");
            if (args.Length < 1)
            {
                Console.WriteLine("Args yok");
            }
            else
            {
                string input = args[0];
                if (input.EndsWith("Chunk0.arc"))
                {
                    byte[] nullEndBytes = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    byte[] chunkData = File.ReadAllBytes(input);
                    Console.WriteLine("ARC CRC offset tablosu dumplanıyor...");
                    foreach (string read in Directory.EnumerateFiles(Path.GetDirectoryName(input) + Path.DirectorySeparatorChar + "mod", "*.pac", SearchOption.AllDirectories))
                    {
                        string file = read.Replace(Path.DirectorySeparatorChar.ToString() + "mod" + Path.DirectorySeparatorChar.ToString(), Path.DirectorySeparatorChar.ToString());
                        string fName = read.Split(Path.DirectorySeparatorChar + "mod" + Path.DirectorySeparatorChar)[1];
                        if (fName == "pac\\menu\\win\\Assets\\wwe19_singleread\\texture\\ssface\\ltag_typea_win.pac" || fName == "pac\\menu\\win\\Assets\\wwe19_singleread\\texture\\ssface\\ltag_typeb_win.pac") continue;
                        FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                        BinaryReader BinaryReader = new BinaryReader(stream);
                        stream.Seek(4096L, SeekOrigin.Begin);
                        byte[] CRCByte = BinaryReader.ReadBytes(96);
                        CRCByte = CRCByte.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
                        string CRC = BitConverter.ToString(CRCByte);
                        Console.WriteLine(file);
                        Int32 offset = chunkData.Locate(CRCByte)[0];
                        table.Add(new CRCTable { File = fName, Offset = offset, OffsetSize= CRCByte.Length , CRC = CRC });
                        BinaryReader.Close();
                        stream.Close();
                    }
                    new ExcelMapper().Save("ARC_Offsets.xlsx", table, "Offsets");
                    Console.WriteLine("ARC CRC offset tablosu dumplandı...");
                }
                else if (Directory.Exists(input))
                {
                    Console.WriteLine("Modlanmış dosyalara göre ARC dosyası tekrar oluşturuluyor...");
                    var CRCList = new ExcelMapper("ARC_Offsets.xlsx").Fetch<CRCTable>();
                    FileStream stream = new FileStream(Path.GetDirectoryName(input) + Path.DirectorySeparatorChar + "Chunk0.arc", FileMode.Open, FileAccess.ReadWrite);
                    foreach (CRCTable table in CRCList)
                    {
                        if (!File.Exists(input + Path.DirectorySeparatorChar + table.File)) continue;
                        Console.WriteLine(table.File + " = " + table.Offset);
                        //BinaryWriter BinaryWriter = new BinaryWriter(stream);
                        stream.Seek(table.Offset, SeekOrigin.Begin);
                        FileStream stream2 = new FileStream(input + Path.DirectorySeparatorChar + table.File, FileMode.Open, FileAccess.Read);
                        BinaryReader BinaryReader2 = new BinaryReader(stream2);
                        stream2.Seek(4096L, SeekOrigin.Begin);
                        byte[] NewCRCByte = BinaryReader2.ReadBytes(table.OffsetSize);
                        BinaryReader2.Close();
                        stream2.Close();
                        stream.Write(NewCRCByte, 0, NewCRCByte.Length);
                    }
                    stream.Close();
                    Console.WriteLine("ARC dosyası patchlendi.");
                }
                else
                {
                    Console.WriteLine("Dosya geçersiz.");
                }
            }
            Console.WriteLine("Konsolu kapatmak için bir tuşa basın.");
            Console.ReadKey();
        }
    }
}
