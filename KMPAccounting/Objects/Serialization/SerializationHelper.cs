using System;
using System.Text;

namespace KMPAccounting.Objects.Serialization
{
    public static class SerializationHelper
    {
        public static readonly int StandardIndentationSize = 1;

        /// <summary>
        ///  Serialize the remarks excluding the ending '\n' which is the caller's responsibility to add.
        /// </summary>
        /// <param name="sb">The string builder for the output.</param>
        /// <param name="remarks">The remarks</param>
        /// <param name="indented">If it is the indented mode.</param>
        public static void SerializeRemarks(this StringBuilder sb, string? remarks, bool indented)
        {
            if (remarks is not null)
            {
                if (indented)
                {
                    if (remarks == string.Empty)
                    {
                        sb.Append('|');
                    }
                    else
                    {
                        sb.Append('\n');
                        SerializationHelper.SerializeIndentedRemarks(sb, remarks, StandardIndentationSize);
                    }
                }
                else
                {
                    sb.Append($"{SerializationHelper.SerializeUnindentedRemarks(remarks)}");
                    sb.Append('|');
                }
            }
        }

        public static string? DeserializeRemarks(string remainingLine, LineLoader ll, bool indented)
        {
            if (indented && string.IsNullOrWhiteSpace(remainingLine))
            {
                return DeserializeIndentedRemarks(ll, StandardIndentationSize);
            }
            else
            {
                return string.IsNullOrWhiteSpace(remainingLine) ? 
                    null : DeserializeUnindentedRemarks(remainingLine);
            }
        }

        public static string SerializeUnindentedRemarks(string remarks)
        {
            return remarks.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("|", @"\|");
        }

        public static void SerializeIndentedRemarks(StringBuilder sb, string remarks, int indentSize)
        {
            var split = remarks.Split('\n');
            for (var index = 0; index < split.Length; index++)
            {
                var line = split[index];
                sb.Append(' ', indentSize);
                sb.Append(line);
                if (index < split.Length - 1)
                {
                    sb.Append('\n');
                }
            }
        }

        public static string? DeserializeIndentedRemarks(LineLoader lineLoader, int indentationSize)
        {
            var indentation = new string(' ', indentationSize);
            StringBuilder? sb = null;
            while (true)
            {
                var line = lineLoader.PeekLine();
                // End of stream or next record (tolerant with any line that has nonzero indentation)
                if (line == null || line.Length > 0 && line[0] != ' ')
                {
                    if (sb is null)
                    {
                        return null;
                    }
                    break;
                }

                if (sb is null)
                {
                    // first line
                    sb = new StringBuilder();
                }
                else
                {
                    sb.Append('\n');
                }

                lineLoader.ReadLine(); // Consume the line.

                if (line.Length > indentationSize && line.StartsWith(indentation))
                {
                    sb.Append(line[indentationSize..]);
                }
                else
                {
                    sb.Append(line.TrimStart());
                }
            }

            return sb.ToString();
        }

        public static string DeserializeUnindentedRemarks(string serializedRemarks)
        {
            var sb = new StringBuilder();
            var i = 0;
            while (i < serializedRemarks.Length)
            {
                if (serializedRemarks[i] == '|')
                {
                    break;
                }
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