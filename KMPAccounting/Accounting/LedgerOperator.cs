using KMPAccounting.Objects.BookKeeping;

namespace KMPAccounting.Accounting
{
    public static class LedgerOperator
    {
        public static void Execute(this Ledger ledger, int startingIndexInclusive, int endingIndexExclusive)
        {
            for (var i = startingIndexInclusive; i < endingIndexExclusive; ++i)
            {
                var entry = ledger.Entries[i];
                entry.Redo();
            }
        }

        public static void Execute(this Ledger ledger, int startingIndexInclusive)
        {
            for (var i = startingIndexInclusive; i < ledger.Entries.Count; ++i)
            {
                var entry = ledger.Entries[i];
                entry.Redo();
            }
        }

        public static void Rollback(this Ledger ledger, int startingIndexInclusive, int endingIndexExclusive)
        {
            for (var i = endingIndexExclusive-1; i >= startingIndexInclusive; --i)
            {
                var entry = ledger.Entries[i];
                entry.Undo();
            }
        }

        public static void Rollback(this Ledger ledger, int startingIndexInclusive)
        {
            for (var i = ledger.Entries.Count - 1; i >= startingIndexInclusive; --i)
            {
                var entry = ledger.Entries[i];
                entry.Undo();
            }
        }
    }
}