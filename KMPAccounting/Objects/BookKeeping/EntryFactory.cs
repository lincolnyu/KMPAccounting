using KMPCommon;
using System;

namespace KMPAccounting.Objects.BookKeeping
{
    public class EntryFactory
    {
        public static Entry DeserializeFromLine(string line)
        {
            var p = 0;
            if (!line.GetNextWord('|', p, out var newp, out var timestampStr) || timestampStr == null)
            {
                throw new ArgumentException("Invalid line format.");
            }

            p = newp + 1;

            var timestamp = CsvUtility.ParseTimestamp(timestampStr);

            line.GetNextWord('|', p, out newp, out var type);

            p = newp + 1;
            var content = line[p..];
            return type switch
            {
                "CompositeTransaction" => CompositeTransaction.ParseLine(timestamp, content),
                "Transaction" => SimpleTransaction.ParseLine(timestamp, content),
                "OpenAccount" => OpenAccount.ParseLine(timestamp, content),
                _ => throw new ArgumentException($"Unknown entry type {type}"),
            };
        }
    }
}