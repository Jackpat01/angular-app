using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using WebPortal_OH_FI.Common;

namespace WebPortal_OH_FI.Models.CostReport
{
    public class AuditAdjustmentHistoryReportModel
    {
        [ExcelStyling(HeaderName = "Provider Name", ColumnWidth = 15)]
        public string ProviderName { get; set; }
        [ExcelStyling(HeaderName = "Provider NPI", ColumnWidth = 13.5f)]
        public string NPI { get; set; }
        [ExcelStyling(HeaderName = "CR Type", ColumnWidth = 9)]
        public string CRType { get; set; }
        [ExcelStyling(HeaderName = "Document Number", ColumnWidth = 17.7f)]
        public string DocumentNumber { get; set; }
        [ExcelStyling(HeaderName = "Schedule", ColumnWidth = 9.3f)]
        public string Schedule { get; set; }
        [ExcelStyling(HeaderName = "Page Number", ColumnWidth = 12.7f)]
        public string PageNumber { get; set; }
        [ExcelStyling(HeaderName = "Line", ColumnWidth = 8.1f)]
        public string Line { get; set; }
        [ExcelStyling(HeaderName = "Column", ColumnWidth = 8.1f)]
        public string Column { get; set; }
        [ExcelStyling(HeaderName = "Grid Line", ColumnWidth = 10.6f)]
        public string GridLine { get; set; }
        [ExcelStyling(HeaderName = "Grid Column", ColumnWidth = 12.9f)]
        public string GridColumn { get; set; }
        [ExcelStyling(HeaderName = "Original Value", ColumnWidth = 13.9f)]
        public string OriginalValue { get; set; }
        [ExcelStyling(HeaderName = "New Value", ColumnWidth = 11.3f)]
        public string NewValue { get; set; }
        [ExcelStyling(HeaderName = "User who made change", ColumnWidth = 22)]
        public string ChangedBy { get; set; }
        [ExcelStyling(HeaderName = "Date/Time", ColumnWidth = 13)]
        public DateTime TimeStamp { get; set; }
        [ExcelStyling(IgnoreProperty = true)]
        public string RangeName { get; set; }
    }
}