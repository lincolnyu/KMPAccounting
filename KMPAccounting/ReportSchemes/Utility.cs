using KMPAccounting.Objects;
using KMPAccounting.Objects.BookKeeping;
using KMPAccounting.Objects.Reports;
using KOU = KMPAccounting.Objects.AccountHelper;
using System.Diagnostics;
using System;
using KMPAccounting.Objects.AccountCreation;

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

        public static AccountsSetup CreateStandard(string bookName, string subdivision = "")
        {
            var state = (AccountPath)bookName;
            return new AccountsSetup
            {
                TaxReturnCashAccount = StandardAccounts.GetAccountFullName(state, StandardAccounts.Cash),
                Income = StandardAccounts.GetAccountFullName(state, StandardAccounts.Income, subdivision),
                Expense = StandardAccounts.GetAccountFullName(state, StandardAccounts.Expense, subdivision),
                Deduction = StandardAccounts.GetAccountFullName(state, StandardAccounts.Deduction, subdivision),
                TaxWithheld = StandardAccounts.GetAccountFullName(state, StandardAccounts.TaxWithheld, subdivision),
                TaxReturn = StandardAccounts.GetAccountFullName(state, StandardAccounts.TaxReturn, subdivision),
                EquityMain =
                    StandardAccounts.GetAccountFullName(state,
                        StandardAccounts.EquityMain), // Assuming equity is equally shared.
            };
        }

        public void InitializeTaxPeriod()
        {
            // Make sure income and deduction etc. are cleared into equity balance.

            // Debit
            var incomeAccount = KOU.GetAccount(Income!);
            var income = incomeAccount?.Balance ?? 0m;
            incomeAccount?.ZeroOutBalanceOfTree();

            // Tax return usually may be under income, so it may already be zeroed.
            // Debit
            var taxReturnAccount = KOU.GetAccount(TaxReturn!);
            var taxReturn = taxReturnAccount?.Balance ?? 0m;
            taxReturnAccount?.ZeroOutBalanceOfTree();

            // Credit
            var deductionAccount = KOU.GetAccount(Deduction!);
            var deduction = deductionAccount?.Balance ?? 0m;
            deductionAccount?.ZeroOutBalanceOfTree();

            // Credit
            decimal expense = 0;
            if (Expense != null)
            {
                var expenseAccount = KOU.GetAccount(Expense);
                expense = expenseAccount?.Balance ?? 0m;
                expenseAccount?.ZeroOutBalanceOfTree();
            }

            // Credit
            var taxWithheldAccount = KOU.GetAccount(TaxWithheld!);
            var taxWithheld = taxWithheldAccount?.Balance ?? 0m;
            taxWithheldAccount?.ZeroOutBalanceOfTree();

            var deltaEquity = income + taxReturn - expense - deduction - taxWithheld;

            Ledger? ledger = null;
            ledger.EnsureCreateAccount(DateTime.Now, EquityMain!, false);

            KOU.GetAccount(EquityMain!)!.Balance += deltaEquity;
        }

        public void FinalizeTaxPeriodPreTaxCalculation(PnlReport pnlReport,
            decimal adjustment = 0)
        {
            pnlReport.Income = KOU.GetAccount(Income!)?.Balance ?? 0;
            pnlReport.Deduction = (KOU.GetAccount(Deduction!)?.Balance ?? 0) +
                                  (BusinessLossDeduction.GetValueOrDefault(0));

            if (adjustment > 0) pnlReport.Income += adjustment;
            else if (adjustment < 0) pnlReport.Deduction -= adjustment;

            pnlReport.TaxWithheld = KOU.GetAccount(TaxWithheld!)?.Balance ?? 0;
        }

        public void FinalizeTaxPeriodPostTaxCalculation(PnlReport pnlReport)
        {
            Ledger? ledger = null;
            ledger.EnsureCreateAccount(DateTime.Now, TaxReturn!, false);
            ledger.EnsureCreateAccount(DateTime.Now, TaxReturnCashAccount!, false);

            ledger.AddAndExecuteTransaction(DateTime.Now, TaxReturnCashAccount!,
                TaxReturn!, pnlReport.TaxReturn);

            Debug.Assert(KOU.GetStateNode(TaxReturnCashAccount!) is { Balance: 0 });
        }
    }
}