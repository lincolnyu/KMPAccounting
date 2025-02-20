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

            int i = 0;
            int j = 0;
            var addedAccount = new HashSet<string>();

            while (true)
            {
                var target = ledger.Entries[i];
                var source = sourceLedger.Entries[j];

                if (target.DateTime < source.DateTime)
                {
                    // Pass the target.

                    CheckOpenAccountAndReturnIfAlreadyAdded(target);
                    ++i;
                    if (i == ledger.Entries.Count)
                    {
                        ledger.Entries.AddRange(sourceLedger.Entries.GetRange(j, sourceLedger.Entries.Count - j)
                            .Where(CheckOpenAccountAndReturnIfAlreadyAdded));
                        return;
                    }
                }
                else if (target.DateTime > source.DateTime)
                {
                    // Insert the source.

                    if (CheckOpenAccountAndReturnIfAlreadyAdded(source))
                    {
                        ledger.Entries.Insert(i, source);
                        ++i;
                    }

                    ++j;
                    if (j == sourceLedger.Entries.Count)
                    {
                        return;
                    }
                }
                else
                {
                    // Insert the source.

                    var canAdd = CheckOpenAccountAndReturnIfAlreadyAdded(source);
                    if (target.Equals(source))
                    {
                        ++i;
                    }
                    else if (canAdd)
                    {
                        ledger.Entries.Insert(i + 1, source);
                        i += 2;
                    }

                    ++j;

                    if (i == ledger.Entries.Count)
                    {
                        ledger.Entries.AddRange(sourceLedger.Entries.GetRange(j, sourceLedger.Entries.Count - j));
                        return;
                    }

                    if (j == sourceLedger.Entries.Count)
                    {
                        return;
                    }
                }
            }

            bool CheckOpenAccountAndReturnIfAlreadyAdded(Entry entry)
            {
                if (entry is OpenAccount openAccount)
                {
                    if (!addedAccount.Add(openAccount.FullName))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}