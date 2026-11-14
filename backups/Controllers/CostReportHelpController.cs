using MSLC.StatLog.Controllers;
using System.Web.Mvc;


namespace WebPortal_OH_FI.Controllers
{
    public class CostReportHelpController : LoggedInBaseController
    {
        // GET: CostReportHelp
        // NOTE: there is no security attribute as everyone should be able to access help.
        public ActionResult Index()
        {
            return PartialView();
        }
    }
}