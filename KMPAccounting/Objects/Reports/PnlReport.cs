using System;
using System.Text;

namespace KMPAccounting.Objects.Reports
{
    public class PnlReport
    {
        public decimal Income { get; set; }
        public decimal Deduction { get; set; }
        public decimal TaxWithheld { get; set; }

        public decimal TaxableIncome => Income + TaxWithheld - Deduction;

        public decimal Tax { get; set; }

        public decimal TaxReturn => TaxWithheld - Tax; // Negative for tax payable

        public decimal PostTaxIncome => TaxableIncome - Tax;

        public Action<StringBuilder>? CustomizedInfo { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            var grossIncome = Income + TaxWithheld;

            sb.Append($"GrossIncome = Income + TaxWithheld = {Income} + {TaxWithheld} = {grossIncome}\n");
            sb.Append(
                $"TaxableIncome = GrossIncome - Deduction = {grossIncome} - {Deduction} = {TaxableIncome}\n");
            sb.Append($"TaxWithheld = {TaxWithheld}\n");
            sb.Append($"Tax = {Tax}\n");
            sb.Append($"TaxReturn = {TaxReturn}\n");
            sb.Append($"PostTaxIncome = TaxableIncome - Tax = {PostTaxIncome}\n");

            CustomizedInfo?.Invoke(sb);

            return sb.ToString();
        }
    }
}