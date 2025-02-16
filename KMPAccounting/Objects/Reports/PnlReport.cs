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

            sb.AppendLine($"GrossIncome = Income + TaxWithheld = {Income} + {TaxWithheld} = {Income + TaxWithheld}");
            sb.AppendLine(
                $"TaxableIncome = GrossIncome - Deduction = {Income + TaxWithheld} - {Deduction} = {TaxableIncome}");
            sb.AppendLine($"TaxWithheld = {TaxWithheld}");
            sb.AppendLine($"Tax = {Tax}");
            sb.AppendLine($"TaxReturn = {TaxReturn}");
            sb.AppendLine($"PostTaxIncome = TaxableIncome - Tax = {PostTaxIncome}");

            CustomizedInfo?.Invoke(sb);

            return sb.ToString();
        }
    }
}