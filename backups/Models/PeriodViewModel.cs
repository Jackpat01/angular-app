using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models
{
    public class PeriodViewModel
    {
        public string PeriodCode { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? SubmissionBegin { get; set; }
        public DateTime? SubmissionEnd { get; set; }
        public virtual ICollection<CHOWPeriodViewModel> CHOWPeriod { get; set; }
            = new HashSet<CHOWPeriodViewModel>();
        public List<SelectListItem> PeriodList { get; set; }
    }
    public class CHOWPeriodViewModel
    {
        public int CHOWPeriodId { get; set; }
        public int ProviderId { get; set; }
        public string PeriodCode { get; set; }
        public int? SubmittedProviderCostReportId { get; set; }
    }
}