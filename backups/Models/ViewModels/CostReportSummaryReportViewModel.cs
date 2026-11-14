using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class CostReportSummaryReportViewModel : ReportViewModelBase
    {
        public CostReportSummaryReportViewModel()
        {
            ReportName = "Cost Report Summary";
        }

        public List<SelectListItem> CostReportTypeList { get; set; }
        public string CostReportType { get; set; }
        public List<SelectListItem> PeriodList { get; set; }
        public string SfyDate { get; set; }
    }
}