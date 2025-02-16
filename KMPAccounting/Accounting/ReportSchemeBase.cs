using KMPAccounting.Objects.Reports;
using System.Collections.Generic;

namespace KMPAccounting.Accounting
{
    public abstract class ReportSchemeBase
    {
        /// <summary>
        ///  Typically called after accounts state initialization and before ledger run.
        /// </summary>
        public abstract void Start();

        /// <summary>
        ///  Typically called after the relevant ledger run to generate the reports.
        /// </summary>
        /// <returns>All the applicable reports this report scheme is able to generate.</returns>
        public abstract IEnumerable<PnlReport> Stop();
    }
}