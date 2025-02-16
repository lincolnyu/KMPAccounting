using KMPAccounting.AccountImporting;
using KMPAccounting.Objects;
using KMPAccounting.Objects.Accounts;
using KMPAccounting.Objects.BookKeeping;

namespace KMPAccounting.Console;

class Program
{
    static void Main(string[] args)
    {
        string? cmd = null;
        string? accountName = null;
        string? inputFile = null;
        string? outputFile = null;
        bool isCredit = false;

        if (args.Length > 0)
        {
            cmd = args[0];

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--account" && i + 1 < args.Length)
                {
                    accountName = args[i + 1];
                    i++;
                }
                else if (args[i] == "--input" && i + 1 < args.Length)
                {
                    inputFile = args[i + 1];
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

        if (cmd == null || accountName == null || inputFile == null || outputFile == null)
        {
            System.Console.WriteLine(
                "Usage: expenseledger --account <account> [credit] --input <inputfile> --output <outputfile>");
            return;
        }

        try
        {
            switch (cmd)
            {
                case "expenseledger":
                    CreateExpenseLedger(accountName, isCredit, inputFile, outputFile);
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private const string UnspecifiedExpenseAccount = "Expense.Unspecified";
    private const string UnspecifiedIncomeAccount = "Income.Unspecified";

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
        ledger.SerializeToStream(sw);
    }
}