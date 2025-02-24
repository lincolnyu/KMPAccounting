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
            public int IndexOfCounterAccounts { get; set; }
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
                IndexOfCounterAccounts = GetIndex(headerLower, "counteraccounts", "counteraccount", "counter accounts",
                    "counter account", "counter_accounts", "counter_account", "against")
            };

            return descriptor;
        }

        public IEnumerable<Transaction> GuessColumnsAndImport(StreamReader sr, string sourceReference,
            string counterAccountsPrefix)
        {
            var header = sr.GetAndBreakRow(true).ToArray();
            var descriptor = GuessDescriptor(header);
            return Import(sr, descriptor, sourceReference, counterAccountsPrefix);
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

        public IEnumerable<Transaction> Import(StreamReader sr, CsvDescriptor descriptor, string sourceReference,
            string counterAccountsPrefix)
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
                };

                if (descriptor.IndexOfCounterAccounts >= 0 &&
                    descriptor.IndexOfCounterAccounts < line.Length)
                {
                    transaction.CounterAccounts.AddRange(ParseCounterAccounts(line[descriptor.IndexOfCounterAccounts], transaction.Amount, counterAccountsPrefix));
                }

                yield return transaction;
            }
        }

        private static IEnumerable<(string, decimal)> ParseCounterAccounts(string counterAccountsValue, decimal totalAmount, string counterAccountsPrefix)
        {
            var counterAccounts = counterAccountsValue.Split(';');

            decimal?[] amounts = new decimal?[counterAccounts.Length];
            var sum = 0m;
            var filled = 0;
            for (var index = 0; index < counterAccounts.Length; index++)
            {
                var counterAccount = counterAccounts[index];
                var parts = counterAccount.Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    decimal? amount;
                    if (parts[1].EndsWith('%'))
                    {
                        if (!decimal.TryParse(parts[1][..^1], out var percent))
                        {
                            throw new ArgumentException($"Invalid percent format: {parts[1]}");
                        }

                        amount = totalAmount * percent / 100;
                    }
                    else
                    {
                        amount = ParseAmount(parts[1]);
                    }

                    if (amount.HasValue)
                    {
                        sum += amount.Value;
                        filled++;
                        amounts[index] = amount;
                    }
                }
            }

            if (filled == amounts.Length)
            {
                if (sum != totalAmount)
                {
                    sum = 0;
                    for (var index = 0; index < amounts.Length - 1; index++)
                    {
                        sum += amounts[index]!.Value;
                    }
                    amounts[^1] = totalAmount - sum;
                }
            }
            else
            {
                var remaining = totalAmount - sum;
                var dist = remaining / (amounts.Length - filled);

                for (var index = 0; index < amounts.Length; index++)
                {
                    if (amounts[index] == null)
                    {
                        amounts[index] = filled == amounts.Length - 1 ? totalAmount - sum : dist;
                        sum += amounts[index]!.Value;
                        filled++;
                    }
                }
            }

            for (var index = 0; index < counterAccounts.Length; index++)
            {
                var counterAccount = counterAccounts[index];
                var parts = counterAccount.Split(':', StringSplitOptions.TrimEntries);
                yield return (counterAccountsPrefix + parts[0], amounts[index]!.Value);
            }
        }
    }
}