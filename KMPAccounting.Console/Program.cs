using KMPAccounting.AccountImporting;
using KMPAccounting.Objects;
using KMPAccounting.Objects.Accounts;
using KMPAccounting.Objects.BookKeeping;

namespace KMPAccounting.Console;

internal class Program
{
    private static void Main(string[] args)
    {
        string? cmd = null;
        string? accountName = null;
        List<string> inputFiles = [];
        string? outputFile = null;
        var isCredit = false;

        if (args.Length > 0)
        {
            cmd = args[0];

            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] == "--account" && i + 1 < args.Length)
                {
                    accountName = args[i + 1];
                    i++;
                }
                else if (args[i] == "--input" && i + 1 < args.Length)
                {
                    var inputFile = args[i + 1];
                    inputFiles.Add(inputFile);
                    i++;
                }
                else if (args[i] == "--output" && i + 1 < args.Length)
                {
                    outputFile = args[i + 1];
                    i++;
                }
                else if (args[i] == "credit")
                {
                    isCredit = true;
                }
            }
        }

        if (cmd == null)
        {
            ShowUsage();
            return;
        }

        try
        {
            switch (cmd)
            {
                case "expense":
                    if (accountName == null || inputFiles.Count != 1 || outputFile == null)
                    {
                        ShowUsage();
                        return;
                    }

                    CreateExpenseLedger(accountName, isCredit, inputFiles[0], outputFile);
                    break;
                case "merge":
                    if (inputFiles.Count == 0 || outputFile == null)
                    {
                        ShowUsage();
                        return;
                    }

                    Merge(inputFiles, outputFile);
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static void ShowUsage()
    {
        System.Console.WriteLine(
            "Usage: expense --account <account> [credit] --input <inputfile> --output <outputfile>");
        System.Console.WriteLine(
            "       merge --input <inputfile1> [--input <inputfile2> ...] --output <outputfile>");
    }

    private const string UnspecifiedExpenseAccount = "Expense.ToBeSpecified";
    private const string UnspecifiedIncomeAccount = "Income.ToBeSpecified";

    private static void CreateExpenseLedger(string accountName, bool isCredit, string inputFile, string outputFile)
    {
        var csvImporter = new CsvImporter();
        using var srCsv = new StreamReader(inputFile);
        var transactions = csvImporter.GuessColumnsAndImport(srCsv, inputFile).OrderBy(x => x.Date).ToArray();

        AccountsState.Clear();
        var ledger = new Ledger();

        var accountCreationDate = DateTime.MinValue;

        ledger.EnsureCreateAccount(accountCreationDate, accountName, isCredit);

        var addedExpenseAccount = new HashSet<string>();
        foreach (var transaction in transactions)
        {
            if (transaction.CounterAccount == null)
            {
                var isExpense = transaction.Amount > 0 ^ !isCredit;
                transaction.CounterAccount = isExpense ? UnspecifiedExpenseAccount : UnspecifiedIncomeAccount;
            }

            if (!addedExpenseAccount.Contains(transaction.CounterAccount))
            {
                ledger.EnsureCreateAccount(accountCreationDate, transaction.CounterAccount,
                    false /* It has to be expense*/);
                addedExpenseAccount.Add(transaction.CounterAccount);
            }
        }

        foreach (var transaction in transactions)
        {
            var amount = transaction.Amount;
            var date = transaction.Date;
            if (isCredit)
            {
                if (amount > 0)
                {
                    // expense
                    ledger.AddAndExecuteTransaction(date, transaction.CounterAccount!, accountName, amount,
                        transaction.Description);
                }
                else
                {
                    // income
                    ledger.AddAndExecuteTransaction(date, accountName, transaction.CounterAccount!, -amount,
                        transaction.Description);
                }
            }
            else
            {
                if (amount > 0)
                {
                    // income
                    ledger.AddAndExecuteTransaction(date, accountName, transaction.CounterAccount!, amount,
                        transaction.Description);
                }
                else
                {
                    // expense
                    ledger.AddAndExecuteTransaction(date, transaction.CounterAccount!, accountName, -amount,
                        transaction.Description);
                }
            }
        }

        using var sw = new StreamWriter(outputFile);
        sw.WriteLine("IndentedRemarks=true");
        ledger.SerializeToStream(sw, true);
    }


    private static void Merge(List<string> inputFiles, string outputFle)
    {
        Ledger? outputLedger = null;
        foreach (var inputFile in inputFiles)
        {
            using var sr = new StreamReader(inputFile);

            var line = sr.ReadLine();
            if (line == null)
            {
                continue;
            }

            if (!line.StartsWith("IndentedRemarks="))
            {
                throw new ArgumentException("Ledger file must start with 'IndentedRemarks=<true/false>'.");
            }

            if (!bool.TryParse(line["IndentedRemarks=".Length..].Trim(), out var indentedRemarks))
            {
                throw new ArgumentException("Ledger file must start with 'IndentedRemarks=<true/false>'.");
            }

            var ledger = new Ledger();
            ledger.DeserializeFromStream(sr, indentedRemarks);
            if (outputLedger == null)
            {
                outputLedger = ledger;
            }
            else
            {
                outputLedger.MergeFrom(ledger);
            }
        }

        if (outputLedger != null)
        {
            using var sw = new StreamWriter(outputFle);
            sw.WriteLine("IndentedRemarks=true");
            outputLedger.SerializeToStream(sw, true);
        }
    }
}