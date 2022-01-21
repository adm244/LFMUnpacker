using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace LFMUnpacker.Extensions
{
  public static class BinaryReaderExtension
  {
    public static int ReadInt16BE(this BinaryReader reader)
    {
      byte[] buffer = reader.ReadBytes(sizeof(Int16));
      return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    public static int ReadInt32BE(this BinaryReader reader)
    {
      byte[] buffer = reader.ReadBytes(sizeof(Int32));
      return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }

    public static string ReadCString(this BinaryReader reader)
    {
      StringBuilder stringBuilder = new StringBuilder();

      for (;;)
      {
        char character = reader.ReadChar();
        if (character == 0)
          break;

        stringBuilder.Append(character);
      }

      return stringBuilder.ToString();
    }
  }
}
