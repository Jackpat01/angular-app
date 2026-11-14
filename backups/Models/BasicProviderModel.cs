
using System.Collections.Generic;
using MSLC.ArumProxy.Interfaces;

namespace WebPortal_OH_FI.Models
{
    public class BasicProviderModel
    {
        public string Name { get; set; }
        public int ProviderID { get; set; }
        public string LTCFacID { get; set; }
        public string MCAID { get; set; }
        public List<IARUMProviderIdentifier> Identifiers { get; set; }
    }
}