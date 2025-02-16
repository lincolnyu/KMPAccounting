using KMPAccounting.Objects.AccountCreation;

namespace KMPAccounting.ReportSchemes
{
    public static class StandardAccounts
    {
        public const string Assets = "Assets";
        public const string Equity = "Equity";
        public const string Liability = "Liability";

        public const string Cash = "Assets.Cash";

        public const string Income = "Equity.Income";               // Credit
        public const string TaxReturn = "Equity.TaxReturn";         // Credit

        public const string Deduction = "Equity.Deduction";         // Debit
        public const string Expense = "Equity.Expense";             // Debit

        public const string TaxWithheld = "Liability.TaxWithheld";  // Credit

        public static readonly string EquityMain = $"Equity.{Objects.Constants.MainNodeName}";

        public static string GetAccountFullName(string bookName, string type, string subdivision="", string subcategory="")
        {
            var path = (AccountPath)bookName;
            path += type.Trim();
            path += subdivision.Trim();
            path += subcategory.Trim();
            return path;
        }
    }
}
