using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KMPCommon;

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
            Fields[descriptor.IndexOfAmount] = Amount.ToString(CultureInfo.InvariantCulture);
            Fields[descriptor.IndexOfDate] = CsvUtility.TimestampToString(Date);
            Fields[descriptor.IndexOfDescription] = Description ?? "";
            Fields[descriptor.IndexOfBalance] = Balance?.ToString(CultureInfo.InvariantCulture) ?? "";
            Fields[descriptor.IndexOfCounterAccounts] = string.Join(";", CounterAccounts.Select(x => $"{x.Item1}:{x.Item2}"));
        }
    }
}