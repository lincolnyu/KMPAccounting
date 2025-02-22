using System;
using System.Text;

namespace KMPAccounting.Objects.Serialization
{
    public static class SerializationHelper
    {
        public static readonly int IndentedSize = 2;

        public static string SerializeRemarks(string remarks)
        {
            return remarks.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("|", @"\|");
        }

        public static void SerializeIndentedRemarks(StringBuilder sb, string remarks, int indentSize)
        {
            foreach (var line in remarks.Split('\n'))
            {
                sb.Append(' ', indentSize);
                sb.Append(line);
                sb.Append('\n');
            }
        }

        public static string DeserializeIndentedRemarks(LineLoader lineLoader, int indentedSize)
        {
            var indentation = new string(' ', indentedSize);
            var sb = new StringBuilder();
            while (true)
            {
                var line = lineLoader.PeekLine();
                // End of stream.
                if (line == null)
                {
                    break;
                }

                if (line.Length > 0 && line[0] != ' ')
                {
                    break;
                }
                // Tolerant with any line that has nonzero indention.

                lineLoader.ReadLine(); // Consume the line.

                if (line.Length > indentedSize && line.StartsWith(indentation))
                {
                    sb.Append(line[indentedSize..]);
                }
                else
                {
                    sb.Append(line.TrimStart());
                }

                sb.Append('\n');
            }

            if (sb.Length > 0)
            {
                sb.Length -= 1; // Remove the last '\n'.
            }

            return sb.ToString();
        }

        public static string DeserializeRemarks(string serializedRemarks)
        {
            var sb = new StringBuilder();
            var i = 0;
            while (i < serializedRemarks.Length)
            {
                if (serializedRemarks[i] == '\\')
                {
                    if (i + 1 < serializedRemarks.Length)
                    {
                        if (serializedRemarks[i + 1] == '\\')
                        {
                            sb.Append('\\');
                            i += 2;
                            continue;
                        }

                        if (serializedRemarks[i + 1] == 'n')
                        {
                            sb.Append('\n');
                            i += 2;
                            continue;
                        }

                        if (serializedRemarks[i + 1] == '|')
                        {
                            sb.Append('|');
                            i += 2;
                            continue;
                        }
                    }

                    throw new ArgumentException("Invalid Remarks field format.");
                }

                sb.Append(serializedRemarks[i]);
                ++i;
            }

            return sb.ToString();
        }
    }
}