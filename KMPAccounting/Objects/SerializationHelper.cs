using System;
using System.Text;

namespace KMPAccounting.Objects
{
    public static class SerializationHelper
    {
        public static string SerializeRemarks(string remarks)
        {
            return remarks.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("|", @"\|");
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
