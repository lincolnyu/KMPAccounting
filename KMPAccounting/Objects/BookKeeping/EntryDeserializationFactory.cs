using KMPCommon;
using System;
using KMPAccounting.Objects.Serialization;

namespace KMPAccounting.Objects.BookKeeping
{
    public class EntryDeserializationFactory
    {
        public static Entry Deserialize(LineLoader lineLoader, bool indentedRemarks)
        {
            var p = 0;
            var line = lineLoader.ReadLine();
            if (line == null)
            {
                throw new ArgumentException("Unexpected end of stream.");
            }

            if (!line.GetNextWord('|', p, out var newp, out var timestampStr) || timestampStr == null)
            {
                throw new ArgumentException("Invalid line format.");
            }

            p = newp + 1;

            var timestamp = CsvUtility.ParseTimestamp(timestampStr);

            line.GetNextWord('|', p, out newp, out var type);

            p = newp + 1;
            var payload = line[p..];

            Entry entry = type switch
            {
                "CompositeTransaction" => CompositeTransaction.ParseLine(timestamp, payload),
                "Transaction" => SimpleTransaction.ParseLine(timestamp, payload),
                "OpenAccount" => OpenAccount.ParseLine(timestamp, payload),
                _ => throw new ArgumentException($"Unknown entry type {type}"),
            };

            if (indentedRemarks)
            {
                entry.Remarks =
                    SerializationHelper.DeserializeIndentedRemarks(lineLoader, SerializationHelper.IndentedSize);
            }

            return entry;
        }
    }
}