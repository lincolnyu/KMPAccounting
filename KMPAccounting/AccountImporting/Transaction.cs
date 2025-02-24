using System;
using System.Collections.Generic;

namespace KMPAccounting.AccountImporting
{
    public class Transaction
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal? Balance { get; set; }
        public DateTime Date { get; set; }
        public List<(string, decimal)> CounterAccounts = [];
    }
}
