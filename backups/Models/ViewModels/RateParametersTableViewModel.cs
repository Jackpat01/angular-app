using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class RateParametersTableViewModel //Rate Parameters table
    {
        public int RatePrameterId { get; set; }
        public bool IsUserAdmin { get; set; }
        public List<SelectListItem> RedDateList { get; set; }
        public List<SelectListItem> RedInfoItems { get; set; }
        public string REDDate { get; set; }
    }

    public class RedInfo //Rate Parameters table
    {
        public int RatePrameterId { get; set; }
        public string REDDate { get; set; }
        public string DisplayName { get; set; }
        public string VariableName { get; set; }
        public int Value { get; set; }
    }
}