using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models
{
    public class CMIOverrideReportViewModel
    {
        public string CYE { get; set; }
        //public string ProviderNumber { get; set; }
        //public string CaseMixValue { get; set; }
        //public string MDSScoreType { get; set; }
        //public string Comments { get; set; }
        //public virtual string Score
        //{
        //    get { return MDSScoreType; }
        //}
        public List<SelectListItem> ReportTypeList { get; set; }
        public string SelectedReportType { get; set; }
        public string RedDateLabelText { get; set; }
        public string RedDateSelectText { get; set; }
        public List<SelectListItem> RedDateList { get; set; }
        public string RedDateSelect { get; set; }
        //public List<DatabaseLookupGridViewModel> DatabaseLookupGroupViewModel { get; set; }
    }
}