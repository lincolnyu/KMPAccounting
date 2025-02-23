using System;
using KMPCommon;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KMPAccounting.AccountImporting
{
    public class CsvImporter
    {
        public class CsvDescriptor
        {
            public int IndexOfDate { get; set; }
            public int IndexOfDescription { get; set; }
            public int IndexOfAmount { get; set; }
            public int IndexOfBalance { get; set; }
            public int IndexOfCounterAccount { get; set; }
        }

        private static int GetIndex(string[] header, params string[] searchItems)
        {
            foreach (var str in searchItems)
            {
                var index = Array.IndexOf(header, str);
                if (index != -1)
                {
                    return index;
                }
            }

            return -1;
        }

        public CsvDescriptor GuessDescriptor(IEnumerable<string> header)
        {
            var headerLower = header.Select(x => x.ToLower()).ToArray();
            var descriptor = new CsvDescriptor
            {
                IndexOfDate = GetIndex(headerLower, "date"),
                IndexOfDescription = GetIndex(headerLower, "description", "remarks", "comments"),
                IndexOfAmount = GetIndex(headerLower, "amount", "debit/credit"),
                IndexOfBalance = GetIndex(headerLower, "balance"),
                IndexOfCounterAccount = GetIndex(headerLower, "counteraccount", "counter account", "counter_account")
            };

            return descriptor;
        }

        public IEnumerable<Transaction> GuessColumnsAndImport(StreamReader sr, string sourceReference)
        {
            var header = sr.GetAndBreakRow(true).ToArray();
            var descriptor = GuessDescriptor(header);
            return Import(sr, descriptor, sourceReference);
        }

        private static decimal? ParseAmount(string amount)
        {
            if (decimal.TryParse(amount, out var result))
            {
                return result;
            }

            if (decimal.TryParse(amount.Replace("$", ""), out result))
            {
                return result;
            }

            return null;
        }

        public IEnumerable<Transaction> Import(StreamReader sr, CsvDescriptor descriptor, string sourceReference)
        {
            while (!sr.EndOfStream)
            {
                var line = sr.GetAndBreakRow(true).ToArray();

                if (descriptor.IndexOfDate < 0)
                {
                    throw new ArgumentException("Date column not found.");
                }

                if (descriptor.IndexOfAmount < 0)
                {
                    throw new ArgumentException("Amount column not found.");
                }

                var transaction = new Transaction
                {
                    Date = DateTime.Parse(line[descriptor.IndexOfDate]),
                    Description = descriptor.IndexOfDescription >= 0 && descriptor.IndexOfDescription < line.Length
                        ? line.ElementAtOrDefault(descriptor.IndexOfDescription)
                        : null,
                    Amount = ParseAmount(line[descriptor.IndexOfAmount])!.Value,
                    Balance = descriptor.IndexOfBalance >= 0 && descriptor.IndexOfBalance < line.Length
                        ? ParseAmount(line[descriptor.IndexOfBalance])
                        : null,
                    CounterAccount = descriptor.IndexOfCounterAccount >= 0 &&
                                     descriptor.IndexOfCounterAccount < line.Length
                        ? line.ElementAtOrDefault(descriptor.IndexOfCounterAccount)
                        : null,
                };
                yield return transaction;
            }
        }
    }
}