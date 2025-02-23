using KMPAccounting.Accounting;
using KMPAccounting.Objects.Reports;
using System.Collections.Generic;

namespace KMPAccounting.ReportSchemes
{
    public class ReportSchemeBusinessGeneric(ReportSchemeBusinessGeneric.BusinessDetails details) : ReportSchemeBase
    {
        public class BusinessDetails
        {
            /// <summary>
            ///  Constructing the object
            /// </summary>
            /// <param name="accountsSetup">Name of the accounts root of the business.</param>
            public BusinessDetails(AccountsSetup accountsSetup)
            {
                AccountsSetup = accountsSetup;
            }

            public AccountsSetup AccountsSetup { get; }
        }

        public override void Start()
        {
            details.AccountsSetup.InitializeTaxPeriod();
        }

        public override IEnumerable<PnlReport> Stop()
        {
            var pnlReport = new PnlReport();

            details.AccountsSetup.FinalizeTaxPeriodPreTaxCalculation(pnlReport);

            pnlReport.Tax = GetBusinessTax(pnlReport.TaxableIncome);

            details.AccountsSetup.FinalizeTaxPeriodPostTaxCalculation(pnlReport);

            yield return pnlReport;
        }

        protected virtual decimal GetBusinessTax(decimal taxableIncome) => GetBusinessTaxDefault(taxableIncome);

        private decimal GetBusinessTaxDefault(decimal taxableIncome)
        {
            if (taxableIncome < 0) return 0;
            //const decimal FullRate = 0.3m;
            const decimal lowerRate = 0.275m;
            // TODO Update this to the correct.
            return taxableIncome * lowerRate;
        }
    }
}