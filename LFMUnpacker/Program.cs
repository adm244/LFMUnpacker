using System.Diagnostics;
using System.IO;
using System.Text;

using LFMUnpacker.Extensions;

namespace LFMUnpacker
{
  public struct TableEntry
  {
    public string Name;
    public int Offset;
    public int Size;
    public byte Flags;
  }

  class Program
  {
    static void Main(string[] args)
    {
      using (FileStream stream = new FileStream(args[0], FileMode.Open, FileAccess.Read))
      {
        using (BinaryReader reader = new BinaryReader(stream, Encoding.Latin1))
        {
          int dataOffset = reader.ReadInt32BE();
          byte globalFlags = reader.ReadByte();

          byte[] tableBuffer = reader.ReadBytes(dataOffset - (int)reader.BaseStream.Position);
          if ((globalFlags & 4) != 0)
          {
            int key = dataOffset + 0x00E6C2CF;
            XorData(key, tableBuffer);
          }

          TableEntry[] entries = ReadTable(tableBuffer, (globalFlags & 2) != 0);

          Trace.Assert(reader.BaseStream.Position == dataOffset);

          for (int i = 0; i < entries.Length; ++i)
          {
            string filepath = Path.Combine(args[1], entries[i].Name);
            string folder = Path.GetDirectoryName(filepath);

            if (!Directory.Exists(folder))
              Directory.CreateDirectory(folder);
            
            byte[] buffer = reader.ReadBytes(entries[i].Size);
            if (entries[i].Flags == 1)
            {
              int key = (dataOffset + entries[i].Offset) + (entries[i].Size * 7);
              XorData(key, buffer);
            }

            using (MemoryStream ms = new MemoryStream(buffer))
            {
              using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
              {
                ms.CopyTo(fs);
              }
            }
          }
        }
      }
    }

    public static TableEntry[] ReadTable(byte[] buffer, bool hasFlags)
    {
      using (MemoryStream stream = new MemoryStream(buffer))
      {
        using (BinaryReader reader = new BinaryReader(stream, Encoding.Latin1))
        {
          int count = reader.ReadInt16BE();
          TableEntry[] entries = new TableEntry[count];

          for (int i = 0; i < entries.Length; ++i)
          {
            entries[i] = new TableEntry()
            {
              Name = reader.ReadCString(),
              Size = reader.ReadInt32BE(),
              Flags = hasFlags ? reader.ReadByte() : (byte)0,
              Offset = i > 0 ? entries[i - 1].Offset + entries[i - 1].Size : 0
            };
          }

          return entries;
        }
      }
    }

    public static void XorData(int key, byte[] buffer)
    {
      int salt = 0x1001 * key - 0x6F0B34D9;
      int position = 0;

      if (buffer.Length > 3)
      {
        int block_size = buffer.Length / 4;
        position = block_size * 4;

        for (int i = 0; i < position; i += 4)
        {
          buffer[i + 0] ^= (byte)(salt >> 4);
          buffer[i + 1] ^= (byte)(salt >> 10);
          buffer[i + 2] ^= (byte)(salt >> 16);
          buffer[i + 3] ^= (byte)(salt >> 22);

          salt = ((int)(key ^ (key << 8) ^ 0xE08ADA15)) + ((0x10001 * key + 0x4D3B1949) * salt);
        }
      }

      if (position < buffer.Length)
      {
        int remaining = buffer.Length - position;
        for (int i = 0; i < remaining; ++i)
        {
          buffer[position + i] ^= (byte)salt;
        }
      }
    }
  }
}
