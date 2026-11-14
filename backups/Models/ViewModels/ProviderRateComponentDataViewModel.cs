using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class ProviderRateComponentDataViewModel
    {
        public List<SelectListItem> PeriodList { get; set; }
        public string SfyDate { get; set; }
        public List<SelectListItem> MonthList { get; set; }
        public string RedDateSelect { get; set; }
        public List<SelectListItem> RateTypeList { get; set; }
        public string RateType { get; set; }
    }
}