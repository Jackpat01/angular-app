using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models
{
    public class NoteViewModel
    {
        public string NoteId { get; set; }
        public string ProviderCostReportId { get; set; }
        public string Notes { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}