namespace WebPortal_OH_FI.Models
{
    public class ScheduleViewModel
    {
        public string ProviderCostReportId { get; set; }
        public string Schedule { get; set; }
        public string Title { get; set; }
        public string Instructions { get; set; }
        public string GridDefinitionURL { get; set; }
        public string GridDataURL { get; set; }
        public string GridUpdateURL { get; set; }
        public string UpdatePageCounterURL { get; set; }
        public string DeletePageURL { get; set; }
        public string GotoPageURL { get; set; }
        public string SummaryGridDiv { get; set; }
        public string SummaryGridCreateFunction { get; set; }
        public string SummaryGridCreateFunctionCall { get; set; }
        public string SummaryGridRefreshCall { get; set; }
        public bool CostReportIsSubmitted { get; set; }
        public bool CanEditCostReport { get; set; }
        public string LockedOn { get; set; }
        public string Style { get; set; }
        public bool Collapsed { get; set; }
        public int SchedulePageCounter { get; set; }
        public string GridLineControlURL { get; set; }
        public string InRuleEngineURL { get; set; }
        public bool CanEditAuditAdjustment { get; set; }
        public bool IsAuditCostReport { get; set; }
    }
}