using KMPAccounting.Objects;
using KMPAccounting.Objects.BookKeeping;
using KMPAccounting.Objects.Reports;
using System.Diagnostics;
using System;
using KMPAccounting.Objects.AccountCreation;
using static KMPAccounting.Objects.AccountHelper;
using static KMPAccounting.Objects.AccountHelper.SideOptionEnum;

namespace KMPAccounting.ReportSchemes
{
    public class AccountsSetup
    {
        /// <summary>
        ///  Suffix following Assets.Cash in the name of the account for tax return, with leading dot.
        /// </summary>
        public string? TaxReturnCashAccount { get; set; }

        public string? Income { get; set; }

        public string? Expense { get; set; }

        public string? Deduction { get; set; }

        public string? TaxWithheld { get; set; }

        public string? TaxReturn { get; set; }

        /// <summary>
        ///  The equity account that attracts income and deduction
        /// </summary>
        public string? EquityMain { get; set; }

        public decimal? BusinessLossDeduction { get; set; }

        public static AccountsSetup CreateStandard(string rootName, string subdivision = "")
        {
            var root = (AccountPath)rootName;
            return new AccountsSetup
            {
                TaxReturnCashAccount = StandardAccounts.GetAccountFullName(root, StandardAccounts.Cash),
                Income = StandardAccounts.GetAccountFullName(root, StandardAccounts.Income, subdivision),
                Expense = StandardAccounts.GetAccountFullName(root, StandardAccounts.Expense, subdivision),
                Deduction = StandardAccounts.GetAccountFullName(root, StandardAccounts.Deduction, subdivision),
                TaxWithheld = StandardAccounts.GetAccountFullName(root, StandardAccounts.TaxWithheld, subdivision),
                TaxReturn = StandardAccounts.GetAccountFullName(root, StandardAccounts.TaxReturn, subdivision),
                EquityMain =
                    StandardAccounts.GetAccountFullName(root,
                        StandardAccounts.EquityMain), // Assuming equity is equally shared.
            };
        }

        public void InitializeTaxPeriod()
        {
            // Make sure income and deduction etc. are cleared into equity balance.

            // Debit
            var incomeAccount = GetAccount(Income!);
            var income = incomeAccount?.Balance ?? 0m;
            incomeAccount?.ZeroOutBalanceOfTree();

            // Tax return usually may be under income, so it may already be zeroed.
            // Debit
            var taxReturnAccount = GetAccount(TaxReturn!);
            var taxReturn = taxReturnAccount?.Balance ?? 0m;
            taxReturnAccount?.ZeroOutBalanceOfTree();

            // Credit
            var deductionAccount = GetAccount(Deduction!);
            var deduction = deductionAccount?.Balance ?? 0m;
            deductionAccount?.ZeroOutBalanceOfTree();

            // Credit
            decimal expense = 0;
            if (Expense != null)
            {
                var expenseAccount = GetAccount(Expense);
                expense = expenseAccount?.Balance ?? 0m;
                expenseAccount?.ZeroOutBalanceOfTree();
            }

            // Credit
            var taxWithheldAccount = GetAccount(TaxWithheld!);
            var taxWithheld = taxWithheldAccount?.Balance ?? 0m;
            taxWithheldAccount?.ZeroOutBalanceOfTree();

            var deltaEquity = income + taxReturn - expense - deduction - taxWithheld;

            Ledger? ledger = null;
            ledger.EnsureCreateAccount(DateTime.Now, EquityMain!, GetChooseSideFunc(SameAsParent));

            GetAccount(EquityMain!)!.Balance += deltaEquity;
        }

        public void FinalizeTaxPeriodPreTaxCalculation(PnlReport pnlReport,
            decimal adjustment = 0)
        {
            pnlReport.Income = GetAccount(Income!)?.Balance ?? 0;
            pnlReport.Deduction = (GetAccount(Deduction!)?.Balance ?? 0) +
                                  (BusinessLossDeduction.GetValueOrDefault(0));

            if (adjustment > 0) pnlReport.Income += adjustment;
            else if (adjustment < 0) pnlReport.Deduction -= adjustment;

            pnlReport.TaxWithheld = GetAccount(TaxWithheld!)?.Balance ?? 0;
        }

        public void FinalizeTaxPeriodPostTaxCalculation(PnlReport pnlReport)
        {
            Ledger? ledger = null;
            ledger.EnsureCreateAccount(DateTime.Now, TaxReturn!, GetChooseSideFunc(SameAsParent));
            ledger.EnsureCreateAccount(DateTime.Now, TaxReturnCashAccount!, GetChooseSideFunc(SameAsParent));

            ledger.AddAndExecuteTransaction(DateTime.Now, TaxReturnCashAccount!,
                TaxReturn!, pnlReport.TaxReturn);

            Debug.Assert(GetStateNode(TaxReturnCashAccount!) is { Balance: 0 });
        }
    }
}