namespace WebPortal_OH_FI.Models
{
    public class CostReportSubmissionModel
    {
        public bool CanLockCostReport { get; set; }

        public bool CanSubmitCostReport { get; set; }

        public bool CanToggleLocalCalculations { get; set; }

        public bool CostReportIsLocked { get; set; }

        public bool CostReportIsOutsideScope { get; set; }

        public bool CostReportIsSubmitted { get; set; }

        public string CostReportMessage { get; set; }

        public bool LocalCalculations { get; set; }

        public string ProviderCostReportId { get; set; }

        public string SubmissionAuthority { get; set; }

        public bool UnresolvedExceptions { get; set; }

        public bool CostReportUsedInRate { get; set; }
    }
}
