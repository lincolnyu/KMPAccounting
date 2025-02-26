using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KMPAccounting.AccountImporting
{
    public class Transaction
    {
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
            SetFieldAtOrAdd(descriptor.IndexOfDescription, Description ?? "");
            SetFieldAtOrAdd(descriptor.IndexOfBalance, Balance?.ToString(CultureInfo.InvariantCulture) ?? "");
            if (CounterAccounts.Count > 1)
            {
                SetFieldAtOrAdd(descriptor.IndexOfCounterAccounts,
                    string.Join(";", CounterAccounts.Select(x => $"{x.Item1}:{x.Item2}")));
            }
            else if (CounterAccounts.Count == 1)
            {
                SetFieldAtOrAdd(descriptor.IndexOfCounterAccounts, CounterAccounts[0].Item1);
            }
        }

        private void SetFieldAtOrAdd(int index, string val)
        {
            while (index >= Fields.Count)
            {
                Fields.Add("");
            }

            Fields[index] = val;
        }
    }
}