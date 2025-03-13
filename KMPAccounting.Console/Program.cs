using System.Diagnostics;
using KMPAccounting.Accounting;
using KMPAccounting.Importing;
using KMPAccounting.Objects;
using KMPAccounting.Objects.AccountCreation;
using KMPAccounting.Objects.Accounts;
using KMPAccounting.Objects.BookKeeping;
using KMPAccounting.ReportSchemes;
using static KMPAccounting.Objects.AccountHelper;
using Transaction = KMPAccounting.Importing.Transaction;

namespace KMPAccounting.Console;

internal class Program
{
    private const string UnspecifiedExpenseAccount = "Expense.ToBeSpecified";
    private const string UnspecifiedIncomeAccount = "Income.ToBeSpecified";

    private static void Main(string[] args)
    {
        string? cmd = null;
        string? stateName = null;
        string? accountName = null;
        List<string> inputFiles = [];
        string? outputFile = null;
        var counterAccountsPrefix = "";
        var isCredit = false;

        if (args.Length > 0)
        {
            cmd = args[0];

            for (var i = 1; i < args.Length; i++)
                if (args[i] == "--root" && i + 1 < args.Length)
                {
                    stateName = args[i + 1];
                    i++;
                }
                else if (args[i] == "--account" && i + 1 < args.Length)
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
                else if (args[i] == "--counteraccountsprefix" && i + 1 < args.Length)
                {
                    counterAccountsPrefix = args[i + 1];
                    i++;
                }
                else if (args[i] == "credit")
                {
                    isCredit = true;
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
                    if (stateName == null || accountName == null || inputFiles.Count != 1 || outputFile == null)
                    {
                        ShowUsage();
                        return;
                    }

                    CreateExpenseLedger(stateName, accountName, isCredit, inputFiles[0], outputFile,
                        counterAccountsPrefix);
                    break;
                case "balance": // load balance
                    if (stateName == null || inputFiles.Count != 1 || outputFile == null)
                    {
                        ShowUsage();
                        return;
                    }

                    LoadBalance(stateName, inputFiles[0], 2, outputFile);
                    break;
                case "merge":
                    if (inputFiles.Count == 0 || outputFile == null)
                    {
                        ShowUsage();
                        return;
                    }

                    Merge(inputFiles, outputFile);
                    break;
                case "writebalance":
                    if (stateName == null || inputFiles.Count != 1 || outputFile == null)
                    {
                        ShowUsage();
                        return;
                    }

                    WriteBalance(stateName, inputFiles[0], outputFile);
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static void WriteBalance(string root, string inputFile, string outputFile)
    {
        Ledger ledger = new();
        {
            using var srLedger = new StreamReader(inputFile);
            var line = srLedger.ReadLine(); // Skip the first line 
            var indentedRemarks = false;
            if (line == null)
            {
                return;
            }
            if (line.StartsWith("IndentedRemarks="))
                indentedRemarks = line.Substring("IndentedRemarks=".Length) == "true";
            ledger.DeserializeFromStream(srLedger, indentedRemarks);
            ledger.Execute(0, ledger.Entries.Count);
        }

        {
            if (GetAccount(root) is not AccountsRoot book)
            {
                System.Console.WriteLine($"Error: Root account {root} not found.");
                return;
            }

            using var sw = new StreamWriter(outputFile);
            sw.Write(book.ToString(2));
        }
    }

    private static void ShowUsage()
    {
        System.Console.WriteLine(
            "Usage: expense --root <root> --account <account> [credit] --input <inputfile> --output <outputfile>");
        System.Console.WriteLine(
            "       balance --root <root> --input <inputfile> --output <outputfile>");
        System.Console.WriteLine(
            "       merge --input <inputfile1> [--input <inputfile2> ...] --output <outputfile>");
        System.Console.WriteLine(
            "       writebalance --root <root> --input <ledgerfile> --output <balancefile>");
    }

    private static void LoadBalance(string stateName, string inputFile, int tabSize, string outputFile)
    {
        AccountsRoot.Clear();
        var ledger = new Ledger();

        using var srBalance = new StreamReader(inputFile);
        var accountCreationDate = DateTime.MinValue;

        List<string> currentAccountNamePath = [];
        var isCredit = false;

        var debited = new List<(string, decimal)>();
        var credited = new List<(string, decimal)>();
        var debitedTotal = 0m;
        var creditedTotal = 0m;
        var addedAccounts = new HashSet<string>();
        string? remarks = null;

        decimal? lastAmount = null;

        while (!srBalance.EndOfStream)
        {
            var line = srBalance.ReadLine();
            if (line == null) break;
            if (line.TrimEnd().Equals("Remarks:", StringComparison.InvariantCultureIgnoreCase))
            {
                remarks = srBalance.ReadToEnd();
                break;
            }

            var spaceCount = line.TakeWhile(char.IsWhiteSpace).Count();

            var parsed = line.Split('=', StringSplitOptions.TrimEntries);
            var name = parsed[0];
            if (name == "Credit" || name == "Debit")
            {
                ConcludeLeaf();
                currentAccountNamePath.Clear();
            }

            if (name == "Credit")
            {
                isCredit = true;
                continue;
            }

            if (name == "Debit")
            {
                isCredit = false;
                continue;
            }

            var level = spaceCount / tabSize - 1; // Credit/Debit is at level 0

            var flagIndex = name.IndexOf('(');
            if (flagIndex >= 0) name = name[..flagIndex];

            decimal? currentAmount = parsed.Length > 1 ? decimal.Parse(parsed[1]) : null;

            if (level < currentAccountNamePath.Count)
            {
                ConcludeLeaf();

                currentAccountNamePath[level] = name;
                currentAccountNamePath.RemoveRange(level + 1, currentAccountNamePath.Count - level - 1);
            }
            else if (level == currentAccountNamePath.Count)
            {
                currentAccountNamePath.Add(name);
            }
            else
            {
                throw new ArgumentException("Invalid indentation level.");
            }

            lastAmount = currentAmount;
        }

        ConcludeLeaf();

        if (creditedTotal != debitedTotal)
        {
            System.Console.WriteLine(
                $"Error: Credited total {creditedTotal} does not match debited total {debitedTotal}.");
            return;
        }

        ledger.AddAndExecuteTransaction(accountCreationDate, debited, credited, remarks);

        using var sw = new StreamWriter(outputFile);
        sw.WriteLine("IndentedRemarks=true");
        ledger.SerializeToStream(sw, true);

        return;

        void ConcludeLeaf()
        {
            if (currentAccountNamePath.Count == 0) return;

            var accountName = $"{stateName}.{string.Join('.', currentAccountNamePath)}";

            if (!addedAccounts.Contains(accountName))
            {
                ledger.EnsureCreateAccount(accountCreationDate, accountName,
                    GetChooseSideFunc(isCredit
                        ? SideOptionEnum.AllCredit
                        : SideOptionEnum.AllDebit));
                addedAccounts.Add(accountName);
            }
            else
            {
                System.Console.WriteLine(
                    $"Error: adding duplicate account {accountName}.");
                return;
            }

            if (lastAmount == null)
            {
                System.Console.WriteLine(
                    $"Error: Amount not specified for account {accountName}.");
                return;
            }

            var amount = lastAmount.Value;
            if (isCredit)
            {
                credited.Add((accountName, amount));
                creditedTotal += amount;
            }
            else
            {
                debited.Add((accountName, amount));
                debitedTotal += amount;
            }
        }
    }

    private static void CreateExpenseLedger(string stateName, string accountName, bool isCredit, string inputFile,
        string outputFile, string counterAccountsPrefix)
    {
        using var srCsv = new StreamReader(inputFile);

        var (_, descriptor) = CsvImporter.GetHeaderAndDescriptor(srCsv);
        var arr = CsvImporter.Import(srCsv, descriptor, counterAccountsPrefix, false).ToArray();
        Transaction[] transactions;
        if (arr.Length > 1 && arr[0].Date > arr[^1].Date)
            transactions = arr.Reverse().ToArray();
        else
            transactions = arr;

        AccountsRoot.Clear();
        var ledger = new Ledger();

        var accountCreationDate = DateTime.MinValue;

        AccountPath root = new(stateName);
        var mainAccount = root + accountName;
        var unspecifiedExpense = root + UnspecifiedExpenseAccount;
        var unspecifiedIncome = root + UnspecifiedIncomeAccount;

        ledger.EnsureCreateAccount(accountCreationDate, mainAccount,
            GetChooseSideFunc(isCredit
                ? SideOptionEnum.AllCredit
                : SideOptionEnum.AllDebit));

        Debug.Assert(GetAccount(mainAccount)!.Balance == 0);

        var addedAccount = new HashSet<string>();
        foreach (var transaction in transactions)
        {
            if (transaction.Amount == 0) continue;

            for (var index = 0; index < transaction.CounterAccounts.Count; index++)
            {
                var (x, amount) = transaction.CounterAccounts[index];
                // Add the root name, assuming it's not included
                transaction.CounterAccounts[index] = (root + x, amount);
            }

            if (transaction.CounterAccounts.Count == 0)
            {
                var isExpense = transaction.Amount < 0;
                transaction.CounterAccounts.Add(
                    (isExpense ? unspecifiedExpense : unspecifiedIncome, transaction.Amount));
            }

            foreach (var (counterAccount, _) in transaction.CounterAccounts)
                if (!addedAccount.Contains(counterAccount))
                {
                    var isCounterAccountCredit = StandardAccounts.GetAccountIsCredit(counterAccount);
                    ledger.EnsureCreateAccount(accountCreationDate, counterAccount,
                        GetChooseSideFunc(isCounterAccountCredit ? SideOptionEnum.AllDebit : SideOptionEnum.AllCredit));
                    addedAccount.Add(counterAccount);
                    Debug.Assert(GetAccount(counterAccount)!.Balance == 0);
                }
        }

        decimal? balanceTracker = null;
        foreach (var transaction in transactions)
        {
            var counterAccounts = transaction.CounterAccounts;

            var amount = transaction.Amount;
            if (amount == 0) continue;

            var balance = transaction.Balance;

            // Note: Assuming credit account amounts and balances are negative
            if (isCredit)
            {
                amount = -amount;
                balance = -balance;
            }

            var date = transaction.Date;

            var isIncome = isCredit ^ (amount > 0);
            var absAmount = amount > 0 ? amount : -amount;
            if (isIncome)
            {
                if (counterAccounts.Count > 1)
                {
                    var composite = CreateTransaction(date, [(mainAccount, absAmount)], counterAccounts,
                        transaction.Description);
                    ledger.AddAndExecute(composite);
                }
                else
                {
                    ledger.AddAndExecuteTransaction(date, mainAccount, counterAccounts[0].Item1, absAmount,
                        transaction.Description);
                }
            }
            else
            {
                if (counterAccounts.Count > 1)
                {
                    var composite = CreateTransaction(date, counterAccounts, [(mainAccount, absAmount)],
                        transaction.Description);
                    ledger.AddAndExecute(composite);
                }
                else
                {
                    ledger.AddAndExecuteTransaction(date, counterAccounts[0].Item1, mainAccount, absAmount,
                        transaction.Description);
                }
            }

            if (balanceTracker != null) balanceTracker += amount;

            if (balance.HasValue)
            {
                if (balanceTracker != null)
                {
                    if (balanceTracker.Value != balance.Value)
                        System.Console.WriteLine(
                            $"Error: Balance verification failed. Expected {balance.Value}, actual {GetAccount(mainAccount)!.Balance}.");
                }
                else
                {
                    balanceTracker = balance;
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
            if (line == null) continue;

            if (!line.StartsWith("IndentedRemarks="))
                throw new ArgumentException("Ledger file must start with 'IndentedRemarks=<true/false>'.");

            if (!bool.TryParse(line["IndentedRemarks=".Length..].Trim(), out var indentedRemarks))
                throw new ArgumentException("Ledger file must start with 'IndentedRemarks=<true/false>'.");

            var ledger = new Ledger();
            ledger.DeserializeFromStream(sr, indentedRemarks);
            if (outputLedger == null)
                outputLedger = ledger;
            else
                outputLedger.MergeFrom(ledger);
        }

        if (outputLedger != null)
        {
            using var sw = new StreamWriter(outputFle);
            sw.WriteLine("IndentedRemarks=true");
            outputLedger.SerializeToStream(sw, true);
        }
    }
}