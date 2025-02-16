using System;

namespace KMPAccounting.AccountImporting
{
    public class Transaction
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal? Balance { get; set; }
        public DateTime Date { get; set; }

        public string? CounterAccount { get; set; }
    }
}
