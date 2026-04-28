using System;
using System.IO;
using System.Text;

namespace UBot.Core.Extensions;

public static class ByteArrayExtensions
{
    public static string HexDump(this byte[] buffer)
    {
        return HexDump(buffer, 0, buffer.Length);
    }

    public static string HexDump(this byte[] buffer, int offset, int count)
    {
        const int bytesPerLine = 16;
        var output = new StringBuilder();
        var ascii_output = new StringBuilder();
        var length = count;
        if (length % bytesPerLine != 0)
            length += bytesPerLine - length % bytesPerLine;
        for (var x = 0; x <= length; ++x)
        {
            if (x % bytesPerLine == 0)
            {
                if (x > 0)
                {
                    output.AppendFormat("  {0}{1}", ascii_output, Environment.NewLine);
                    ascii_output.Clear();
                }

                if (x != length)
                    output.AppendFormat("{0:d10}   ", x);
            }

            if (x < count)
            {
                output.AppendFormat("{0:X2} ", buffer[offset + x]);
                var ch = (char)buffer[offset + x];
                if (!char.IsControl(ch))
                    ascii_output.AppendFormat("{0}", ch);
                else
                    ascii_output.Append(".");
            }
            else
            {
                output.Append("   ");
                ascii_output.Append(".");
            }
        }

        return output.ToString().TrimEnd('\r', '\n', ' ');
    }
}

public static class BinaryWriterExtensions
{
    public static void WriteAscii(this BinaryWriter writer, string value)
    {
        var encoding = Encoding.UTF8;

        var buffer = encoding.GetBytes(value);

        writer.Write(buffer.Length);
        writer.Write(buffer);
    }

    public static byte[] GetSnapshot(this BinaryWriter binary) => ((MemoryStream)binary.BaseStream).ToArray();
}