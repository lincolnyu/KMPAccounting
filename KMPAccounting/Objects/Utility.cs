using System.Collections.Generic;
using System.Linq;
using KMPAccounting.Objects.BookKeeping;

namespace KMPAccounting.Objects
{
    public static class Utility
    {
        public static void MergeFrom(this Ledger ledger, Ledger sourceLedger)
        {
            // TODO inconsistent accounts (accounts with the same name but different properties)

            var i = 0;
            var j = 0;
            var addedAccount = new HashSet<string>();

            while (i < ledger.Entries.Count && j < sourceLedger.Entries.Count)
            {
                var target = ledger.Entries[i];
                var source = sourceLedger.Entries[j];

                if (target.DateTime > source.DateTime)
                {
                    // Insert the source.
                    if (IsNotOpeningAlreadyAddedAccount(source))
                    {
                        ledger.Entries.Insert(i, source);
                        ++i;
                    }
                    ++j;
                }
                else
                {
                    // Prioritize the target.
                    if (IsNotOpeningAlreadyAddedAccount(target))
                    {
                        ++i;
                    }
                    else
                    {
                        ledger.Entries.RemoveAt(i);
                    }
                    if (target.Equals(source))
                    {
                        ++j;
                    }
                }
            }

            for (; i < ledger.Entries.Count; i++)
            {
                if (!IsNotOpeningAlreadyAddedAccount(ledger.Entries[i]))
                {
                    ledger.Entries.RemoveAt(i);
                    --i;
                }
            }

            if (j < sourceLedger.Entries.Count)
            {
                ledger.Entries.AddRange(sourceLedger.Entries.GetRange(j, sourceLedger.Entries.Count - j).Where(IsNotOpeningAlreadyAddedAccount));
            }

            bool IsNotOpeningAlreadyAddedAccount(Entry entry)
            {
                return entry is not OpenAccount openAccount || addedAccount.Add(openAccount.FullName);
            }
        }
    }
}