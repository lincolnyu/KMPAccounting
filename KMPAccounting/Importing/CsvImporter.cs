﻿using System;
using KMPCommon;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KMPAccounting.Objects.AccountCreation;

namespace KMPAccounting.Importing
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

        public static CsvDescriptor GuessDescriptor(IEnumerable<string> header)
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

        public static (string[], CsvDescriptor) GetHeaderAndDescriptor(StreamReader sr)
        {
            var header = sr.GetAndBreakRow(true).ToArray();
            var descriptor = GuessDescriptor(header);
            return (header, descriptor);
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

        public static IEnumerable<Transaction> Import(StreamReader sr, CsvDescriptor descriptor,
            string counterAccountsPrefix, bool keepAllFields)
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

                var balance = descriptor.IndexOfBalance >= 0 && descriptor.IndexOfBalance < line.Length
                        ? ParseAmount(line[descriptor.IndexOfBalance])
                        : null;

                var amount = ParseAmount(line[descriptor.IndexOfAmount]);

                if (!DateTime.TryParse(line[descriptor.IndexOfDate], out var date))
                {
                    continue;
                }

                if (amount is null)
                {
                    continue;
                }

                var transaction = new Transaction
                {
                    Date = date,
                    Description = descriptor.IndexOfDescription >= 0 && descriptor.IndexOfDescription < line.Length
                        ? line.ElementAtOrDefault(descriptor.IndexOfDescription)
                        : null,
                    Amount = amount!.Value,
                    Balance = balance,
                };

                if (descriptor.IndexOfCounterAccounts >= 0 &&
                    descriptor.IndexOfCounterAccounts < line.Length)
                {
                    transaction.CounterAccounts.AddRange(ParseCounterAccounts(line[descriptor.IndexOfCounterAccounts],
                        transaction.Amount, counterAccountsPrefix));
                }

                if (keepAllFields)
                {
                    transaction.Fields.AddRange(line);
                }

                yield return transaction;
            }
        }

        private static IEnumerable<(string, decimal)> ParseCounterAccounts(string counterAccountsValue,
            decimal totalAmount, string counterAccountsPrefix)
        {
            var counterAccounts = counterAccountsValue.Split(Transaction.AccountsDelimiter);

            decimal?[] amounts = new decimal?[counterAccounts.Length];
            var sum = 0m;
            var filled = 0;
            for (var index = 0; index < counterAccounts.Length; index++)
            {
                var counterAccount = counterAccounts[index];
                var parts = counterAccount.Split(Transaction.AccountValuePairDelimiter, StringSplitOptions.TrimEntries);
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

            var counterAccountsPrefixAsPath = new AccountPath(counterAccountsPrefix);

            for (var index = 0; index < counterAccounts.Length; index++)
            {
                var counterAccount = counterAccounts[index];
                var parts = counterAccount.Split(Transaction.AccountValuePairDelimiter, StringSplitOptions.TrimEntries);
                yield return (counterAccountsPrefixAsPath + parts[0], amounts[index]!.Value);
            }
        }
    }
}