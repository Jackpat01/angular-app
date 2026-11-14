using System.Collections.Generic;
using static MSLC.StatLog.MVC.ViewModels.StatLogViewModel;

namespace WebPortal_OH_FI.Models.CostReport
{
    public class Index
    {
        public string PrimaryIdentifier { get; set; }
        public List<ProviderDropDown> AutoProviderInformation { get; set; }
        public bool IsCollapsed { get; set; } = false;
        public string InRuleEngineURL { get; set; } = "";
        public bool UseLocalCalculations { get; set; }
    }
}