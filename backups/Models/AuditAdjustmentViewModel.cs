using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models
{
    //public class AuditAdjustmentViewModel
    //{
    //    public int AuditAdjustmentId { get; set; }
    //    public int ProviderCostReportId { get; set; }
    //    public string CostReportTypeId { get; set; }
    //    public int LineNo { get; set; } // 1 or 2
    //    public string WorkSheetName { get; set; }
    //    public string AccNum { get; set; }
    //    public string WorkPaperReference { get; set; }
    //    public string Narr1 { get; set; }
    //    public string Narr2 { get; set; }
    //    public string Narr3 { get; set; }
    //    public int? Col { get; set; }
    //    public int? AccountType { get; set; }
    //    public string RatioAlloc { get; set; }
    //    public string ReportedOrAdjusted { get; set; }
    //    public string Adjustment { get; set; }
    //    public string Adjusted { get; set; }
    //    public int? AdjustmentPairId { get; set; }
    //    public bool IsActive { get; set; } = true;
    //}

    /// <summary>
    /// View model for jqGrid row data (flattened for display).
    /// </summary>
    //public class AuditAdjustmentGridViewModel
    //{
    //    public int AuditAdjustmentId { get; set; }
    //    public int ProviderCostReportId { get; set; }
    //    public string CostReportTypeId { get; set; }
    //    public int LineNo { get; set; }
    //    public string WorkSheetName { get; set; }
    //    public string AccNum { get; set; }
    //    public string WorkPaperReference { get; set; }
    //    public string Narr1 { get; set; }
    //    public string Narr2 { get; set; }
    //    public string Narr3 { get; set; }
    //    public string Col { get; set; }
    //    public int? AccountType { get; set; }
    //    public string AccountTypeDisplay { get; set; }
    //    public string RatioAlloc { get; set; }
    //    public string ReportedOrAdjusted { get; set; }
    //    public string Adjustment { get; set; }
    //    public string Adjusted { get; set; }
    //    public int? AdjustmentPairId { get; set; }
    //    //public string ErrorMessages { get; set; }
    //}

    /// <summary>
    /// Request model for saving an adjustment pair (lines 1 and 2).
    /// </summary>
    public class SaveAuditAdjustmentGridViewModel
    {
        public int ProviderCostReportId { get; set; }
        public string CostReportTypeId { get; set; }
        public string WorkSheetName { get; set; }
        public string AccNum { get; set; }
        public string WorkPaperReference { get; set; }
        public string Narr1 { get; set; }
        public string Narr2 { get; set; }
        public string Narr3 { get; set; }
        public string Col { get; set; }
        public int? AccountType { get; set; }
        public string RatioAlloc { get; set; }
        public string ReportedOrAdjusted { get; set; }
        public string Adjustment { get; set; }
        public string Adjusted { get; set; }
        public int? AdjustmentPairId { get; set; }
        public string Line2Narr2 { get; set; }        
        // Line 2 overrides (only RatioAlloc can be changed by user on line 2 for B/C/D schedules)
        public string Line2RatioAlloc { get; set; }
    }

    //public class AuditAdjustmentSummaryViewModel
    //{
    //    public int ProviderCostReportId { get; set; }
    //    public int? AdjustmentPairId { get; set; }
    //    public string Worksheet { get; set; }
    //    public int ColLine1 { get; set; }
    //    public int ColLine2 { get; set; }
    //    public string AccNum { get; set; }
    //    public int AdjustedLine1 { get; set; }
    //    public int Adjustment { get; set; }
    //    public string RatioAllocLine2 { get; set; }
    //    public int AdjustedLine2 { get; set; }
    //    public bool IsActive { get; set; }
    //}

    public class AuditAdjustmentPanelRecordViewModel
    {
        public string Schedule { get; set; }
        public string AccountNumber { get; set; }
        public string ColumnLine1 { get; set; }
        public string ColumnLine2 { get; set; }
        public string SalaryOtherPeriod { get; set; }
        public string Line1RatioAlloc { get; set; }
        public string Line2RatioAlloc { get; set; }
        public string Line2Desc { get; set; }
        public string ReportedOrAdjusted { get; set; }
        public string Adjustment { get; set; }
        public string Adjusted {  get; set; }
    }
}