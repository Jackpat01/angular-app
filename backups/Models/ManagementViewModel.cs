namespace WebPortal_OH_FI.Models
{
    public class ManagementViewModel
    {
        public string ProviderCostReportId { get; set; }
        public string ProviderId { get; set; }
        public string PeriodId { get; set; }
        public string CostReportTypeId { get; set; }
        public string VersionName { get; set; }
        public string ProviderName { get; set; }
        public bool CanEditCostReport { get; set; }
        public bool CanCreateNewFacility { get; set; }
        public bool CanCreateFromCopy { get; set; }

        public bool CanCreateCostReport { get; set; }

        public string LastSubmittedCostReportTypeCode { get; set; }

        public string LastSubmittedDocumentNumber { get; set; }

    }
}