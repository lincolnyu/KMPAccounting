using KMPAccounting.Objects.AccountCreation;

namespace KMPAccounting.ReportSchemes
{
    public static class StandardAccounts
    {
        public const string Assets = "Assets";
        public const string Equity = "Equity";
        public const string Liability = "Liability";

        public const string Cash = "Assets.Cash";

        public const string Income = "Income";                              // Credit
        public const string TaxReturn = "TaxReturn";                        // Credit
        public const string TaxWithheld = "TaxWithheld";                    // Tax withheld credit side, always equal to TaxWithheldTaxOffice.

        public const string Deduction = "Deduction";                        // Debit
        public const string Expense = "Expense";                            // Debit
        public const string TaxWithheldTaxOffice = "TaxWithheldTaxOffice";  // Tax withheld debit side, always equal to TaxWithheld.


        public static readonly string EquityMain = $"Equity.{Objects.Constants.MainNodeName}";

        public static string GetAccountFullName(string rootName, string type, string subdivision="", string subcategory="")
        {
            var path = (AccountPath)rootName;
            path += type.Trim();
            path += subdivision.Trim();
            path += subcategory.Trim();
            return path;
        }

        public static bool GetAccountIsCredit(AccountPath accountFullName)
        {
            var typeName = accountFullName[1];
            return !typeName.StartsWith(Assets);
        }
    }
}
