using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models
{
    public class RateTypePeriodVersionViewModel
    {
        public List<SelectItemList> RateTypes { get; set; }
        public List<SelectItemList> CostReportPeriods { get; set; }
        public List<SelectItemList> CostReportVersions { get; set; }
    }
}