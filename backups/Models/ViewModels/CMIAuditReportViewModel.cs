using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class CMIAuditReportViewModel
    {
        public List<SelectListItem> ReportTypeList { get; set; } = new List<SelectListItem>();
        public string SelectedReportType { get; set; }
        public List<SelectListItem> DateTypeList { get; set; } = new List<SelectListItem>();
        public string SelectedDate { get; set; }
        public string VariableLabel { get; set; }
    }
}