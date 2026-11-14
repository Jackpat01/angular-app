using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class ClosedFacilitiesReportViewModel
    {
        public List<SelectListItem> ProviderStatuses { get; set; }
        public string SelectedProviderStatus { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
