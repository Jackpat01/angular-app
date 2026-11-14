// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System.Web.Mvc;

namespace MSLC.StatLog.UI.Models
{
    public static class MSLCComponents
    {
        public static XMLSchemaComponent XMLSchemaDisplay(this HtmlHelper html, string schema, int id, string xml = "") => new XMLSchemaComponent(html, schema, xml, id);
    }
}