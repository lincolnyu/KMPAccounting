using KMPAccounting.Accounting;
using KMPAccounting.Objects.Reports;
using System;
using System.Collections.Generic;

namespace KMPAccounting.ReportSchemes
{
    public class ReportSchemePersonalGeneric : ReportSchemeBase
    {
        public class PersonalDetails
        {
            /// <summary>
            ///  Constructing the object
            /// </summary>
            /// <param name="stateName">Name of the accounts state of the person.</param>
            /// <param name="taxReturnCashAccount">Suffix following Assets.Cash in the name of the account for tax return, with leading dot.</param>
            /// <param name="equityDivisionName">A nonempty substring in the equity account for the person of a couple share the same accounts state, with leading dot.</param>
            public PersonalDetails(AccountsSetup accountsSetup)
            {
                AccountsSetup = accountsSetup;
            }

            public AccountsSetup AccountsSetup { get; }

            public decimal NetIncomeAdjustment { get; set; } = 0m;

            /// <summary>
            ///  Post income tax calculation adjustent, such as medicare levy etc.
            /// </summary>
            public decimal TaxAdjustment { get; internal set; } = 0m;
        }

        public class TaxBracket
        {
            public TaxBracket(decimal rate, decimal lowerbound, decimal upperbound, decimal baseTax)
            {
                Rate = rate;
                Lowerbound = lowerbound;
                Upperbound = upperbound;
                BaseTax = baseTax;
            }

            /// <summary>
            ///  Tax for every dollar
            /// </summary>
            public decimal Rate { get; }

            public decimal Lowerbound { get; }
            public decimal Upperbound { get; }
            public decimal BaseTax { get; }

            public decimal Calculate(decimal taxableIncome)
            {
                return (taxableIncome - Lowerbound) * Rate + BaseTax;
            }
        }

        public class PersonalTaxOutcome
        {
            public PersonalTaxOutcome(decimal taxableIncome, BaseTaxBrackets brackets, int bracketIndex)
            {
                TaxableIncome = taxableIncome;
                TaxBracket = brackets.Brackets[bracketIndex];
                Tax = TaxBracket.Calculate(taxableIncome);
                TaxBracketIndex = bracketIndex;
            }

            public TaxBracket TaxBracket { get; }
            public decimal TaxableIncome { get; }
            public decimal Tax { get; }

            public int TaxBracketIndex { get; }
        }

        public abstract class BaseTaxBrackets
        {
            public abstract TaxBracket[] Brackets { get; }

            public PersonalTaxOutcome Calculate(decimal taxableIncome)
            {
                for (var i = 0; i < Brackets.Length; i++)
                {
                    var bracket = Brackets[i];
                    if (taxableIncome <= bracket.Upperbound)
                    {
                        return new PersonalTaxOutcome(taxableIncome, this, i);
                    }
                }

                throw new ArgumentException($"Unable to find a tax bracket for income {taxableIncome}.");
            }
        }

        public ReportSchemePersonalGeneric(PersonalDetails selfDetails, PersonalDetails? partnerDetails = null)
        {
            SelfDetails = selfDetails;
            PartnerDetails = partnerDetails;
        }

        public override void Start()
        {
            SelfDetails.AccountsSetup.InitializeTaxPeriod();

            if (PartnerDetails != null)
            {
                PartnerDetails.AccountsSetup.InitializeTaxPeriod();
            }
        }

        public override IEnumerable<PnlReport> Stop()
        {
            var pnlReport = new PnlReport();
            PnlReport? partnerPnlReport = null;

            SelfDetails.AccountsSetup.FinalizeTaxPeriodPreTaxCalculation(pnlReport, SelfDetails.NetIncomeAdjustment);

            if (PartnerDetails != null)
            {
                partnerPnlReport = new PnlReport();

                PartnerDetails.AccountsSetup.FinalizeTaxPeriodPreTaxCalculation(partnerPnlReport,
                    PartnerDetails.NetIncomeAdjustment);

                var (tax, partnerTax) = GetFamilyTax(pnlReport.TaxableIncome, partnerPnlReport.TaxableIncome);

                pnlReport.Tax = tax.Tax;
                partnerPnlReport.Tax = partnerTax.Tax;

                pnlReport.CustomizedInfo = sb =>
                {
                    sb.AppendLine(
                        $"Bracket = {{ Index = {tax.TaxBracketIndex}, Rate = {tax.TaxBracket.Rate}, Lower = {tax.TaxBracket.Lowerbound}, Upper = {tax.TaxBracket.Upperbound} }}");
                };

                pnlReport.Tax += SelfDetails.TaxAdjustment;
                partnerPnlReport.Tax += PartnerDetails.TaxAdjustment;

                partnerPnlReport.CustomizedInfo = sb =>
                {
                    sb.AppendLine(
                        $"Bracket = {{ Index = {partnerTax.TaxBracketIndex}, Rate = {partnerTax.TaxBracket.Rate}, Lower = {partnerTax.TaxBracket.Lowerbound}, Upper = {partnerTax.TaxBracket.Upperbound} }}");
                };
            }
            else
            {
                var tax = GetPersonalTax(pnlReport.TaxableIncome);
                pnlReport.Tax = tax.Tax;
                pnlReport.Tax += SelfDetails.TaxAdjustment;

                pnlReport.CustomizedInfo = sb =>
                {
                    sb.AppendLine(
                        $"Bracket = {{ Index = {tax.TaxBracketIndex}, Rate = {tax.TaxBracket.Rate}, Lower = {tax.TaxBracket.Lowerbound}, Upper = {tax.TaxBracket.Upperbound} }}");
                };
            }

            SelfDetails.AccountsSetup.FinalizeTaxPeriodPostTaxCalculation(pnlReport);

            yield return pnlReport;

            if (partnerPnlReport != null)
            {
                PartnerDetails!.AccountsSetup.FinalizeTaxPeriodPostTaxCalculation(partnerPnlReport);

                yield return partnerPnlReport;
            }
        }

        protected virtual PersonalTaxOutcome GetPersonalTax(decimal taxableIncome) =>
            GetPersonalTaxDefault(taxableIncome);

        protected virtual (PersonalTaxOutcome, PersonalTaxOutcome) GetFamilyTax(decimal taxableIncome1,
            decimal taxableIncome2)
        {
            // TODO Find out the real family tax policy
            var tax1 = GetPersonalTax(taxableIncome1);
            var tax2 = GetPersonalTax(taxableIncome2);
            return (tax1, tax2);
        }

        public class DefaultTaxBrackets : BaseTaxBrackets
        {
            const decimal Bracket0UpperLimit = 18200;
            const decimal Bracket1UpperLimit = 45000;
            const decimal Bracket2UpperLimit = 120000;
            const decimal Bracket3UpperLimit = 180000;

            const decimal Bracket1Rate = 0.19m;
            const decimal Bracket2Rate = 0.325m;
            const decimal Bracket3Rate = 0.37m;
            const decimal Bracket4Rate = 0.45m;

            public override TaxBracket[] Brackets { get; } =
            {
                new TaxBracket(0, 0, Bracket0UpperLimit, 0m),
                new TaxBracket(Bracket1Rate, Bracket0UpperLimit, Bracket1UpperLimit, 0m),
                new TaxBracket(Bracket2Rate, Bracket1UpperLimit, Bracket2UpperLimit, 5092m),
                new TaxBracket(Bracket3Rate, Bracket2UpperLimit, Bracket3UpperLimit, 29467m),
                new TaxBracket(Bracket4Rate, Bracket3UpperLimit, decimal.MaxValue, 51667m)
            };

            public static DefaultTaxBrackets Instance { get; } = new DefaultTaxBrackets();
        }

        public class PersonalTaxBrackets_FY2024 : DefaultTaxBrackets
        {
            const decimal Bracket0UpperLimit = 18200;
            const decimal Bracket1UpperLimit = 45000;
            const decimal Bracket2UpperLimit = 135000; // Changed vs default
            const decimal Bracket3UpperLimit = 180000;

            const decimal Bracket1Rate = 0.16m; // Changed vs default
            const decimal Bracket2Rate = 0.325m;
            const decimal Bracket3Rate = 0.37m;
            const decimal Bracket4Rate = 0.45m;

            public override TaxBracket[] Brackets { get; } =
            {
                new TaxBracket(0, 0, Bracket0UpperLimit, 0m),
                new TaxBracket(Bracket1Rate, Bracket0UpperLimit, Bracket1UpperLimit, 0m),
                new TaxBracket(Bracket2Rate, Bracket1UpperLimit, Bracket2UpperLimit, 5092m),
                new TaxBracket(Bracket3Rate, Bracket2UpperLimit, Bracket3UpperLimit, 29467m),
                new TaxBracket(Bracket4Rate, Bracket3UpperLimit, decimal.MaxValue, 51667m)
            };

            public static new PersonalTaxBrackets_FY2024 Instance { get; } = new PersonalTaxBrackets_FY2024();
        }

        public static PersonalTaxOutcome GetPersonalTaxDefault(decimal taxableIncome)
        {
            return DefaultTaxBrackets.Instance.Calculate(taxableIncome);
        }

        /// <summary>
        ///  Stage 3 Tax cut
        /// </summary>
        /// <param name="taxableIncome">Taxable income</param>
        /// <returns>Tax paiable</returns>
        /// <remarks>
        ///  https://www.etax.com.au/stage-3-tax-cuts-explained/?utm_source=taxtipsmar2024&utm_medium=email&utm_campaign=taxtips0324-seg2-5397002&sc_src=email_5397002&sc_lid=372240647&sc_uid=YP8Nz3SJbL&sc_llid=655882&sc_eh=99de70f40385c2701
        /// </remarks>
        public static PersonalTaxOutcome GetPersonalTax_FY2024Stage3Cut(decimal taxableIncome)
        {
            return PersonalTaxBrackets_FY2024.Instance.Calculate(taxableIncome);
        }

        public PersonalDetails SelfDetails { get; }

        public PersonalDetails? PartnerDetails { get; }
    }
}