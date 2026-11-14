using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class ProviderAddressListReportViewModel
    {
        public List<SelectListItem> AddressTypes { get; set; }
        public string AddressTypeSelected { get; set; }
        public string StartDate { get; set; }
        public bool IncludeInactive { get; set; }
    }
}
