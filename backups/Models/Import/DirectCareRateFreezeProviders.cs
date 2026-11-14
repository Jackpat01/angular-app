using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models.Import
{
    public class DirectCareRateFreezeProviderImport
    {
        public string EffectiveDate { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixScore { get; set; }
        public string DirectCareRate { get; set; }
    }
}