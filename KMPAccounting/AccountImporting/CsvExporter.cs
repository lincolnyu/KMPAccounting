using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KMPCommon;
using static KMPAccounting.AccountImporting.CsvImporter;

namespace KMPAccounting.AccountImporting
{
    public class CsvExporter
    {
        public static void Export(StreamWriter sw, IEnumerable<Transaction> transactions, CsvDescriptor descriptor, string[] header, string counterAccountsPrefix)
        {
            sw.WriteLine(string.Join(",", header.Select(CsvUtility.StringToCsvField)));
            foreach (var transaction in transactions)
            {
                if (counterAccountsPrefix != "")
                {
                    var newList = new List<(string, decimal)>();
                    foreach (var (counterAccount, amount) in transaction.CounterAccounts)
                    {
                        if (counterAccount.StartsWith(counterAccountsPrefix))
                        {
                            newList.Add((counterAccount[counterAccountsPrefix.Length..], amount));
                        }
                        else
                        {
                            throw new ArgumentException(
                                $"Counter account {counterAccount} is not starting with specified prefix {counterAccountsPrefix}");
                        }
                    }

                    transaction.CounterAccounts = newList;
                }

                transaction.UpdateToFields(descriptor);
                sw.WriteLine(string.Join(",", transaction.Fields.Select(CsvUtility.StringToCsvField)));
            }
        }
    }
}