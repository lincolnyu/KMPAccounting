using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KMPAccounting.Importing
{
    public class Transaction
    {
        public const char AccountsDelimiter = ';';

        public const char AccountValuePairDelimiter = '=';

        public string? Description { get; set; }

        public decimal Amount { get; set; }

        public decimal? Balance { get; set; }

        public DateTime Date { get; set; }

        public List<(string, decimal)> CounterAccounts = [];

        public List<string> Fields = [];

        public void UpdateToFields(CsvImporter.CsvDescriptor descriptor)
        {
            SetFieldAtOrAdd(descriptor.IndexOfAmount, Amount.ToString(CultureInfo.InvariantCulture));

            SetFieldAtOrAdd(descriptor.IndexOfDate, Date.ToShortDateString());

            if (Description is not null)
            {
                if (descriptor.IndexOfDescription < 0)
                {
                    throw new ArgumentException("Descriptor does not have Description index defined but description is set in the transaction.");
                }
                SetFieldAtOrAdd(descriptor.IndexOfDescription, Description);
            }

            if (Balance.HasValue)
            {
                if (descriptor.IndexOfBalance < 0)
                {
                    throw new ArgumentException("Descriptor does not have Balance index defined but balance is set in the transaction.");
                }
                SetFieldAtOrAdd(descriptor.IndexOfBalance, Balance.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (CounterAccounts.Count > 0)
            {
                var index = descriptor.IndexOfCounterAccounts;
                if (index < 0)
                {
                    throw new ArgumentException($"Descriptor does not have CounterAccounts index defined but with counter accounts: '{CounterAccounts[0].Item1}', ...");
                }

                if (CounterAccounts.Count > 1)
                {
                    SetFieldAtOrAdd(index, string.Join(AccountsDelimiter, CounterAccounts.Select(x => $"{x.Item1}{AccountValuePairDelimiter}{x.Item2}")));
                }
                else /*CounterAccounts.Count == 1*/
                {
                    SetFieldAtOrAdd(index, CounterAccounts[0].Item1);
                }
            }
        }

        private void SetFieldAtOrAdd(int index, string val)
        {
            while (index >= Fields.Count)
            {
                Fields.Add("");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index cannot be negative ({index})");
            }

            Fields[index] = val;
        }
    }
}