using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models
{
    public class ChopDataViewModel
    {
        public int ChopDataId { get; set; }
        public int ChowReviewID { get; set; }
        public int ProviderId { get; set; }
        public string Comments { get; set; }
    }
}