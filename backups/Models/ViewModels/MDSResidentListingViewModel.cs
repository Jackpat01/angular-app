using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class MDSResidentListingViewModel
    {
        public string ID { get; set; }
        public string JobName { get; set; }
        public string ReportName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }
        public bool Downloadable { get; set; }
        public bool Error { get; set; }
    }
}