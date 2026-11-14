using System.Web.Mvc;
using SSO.Functions;
using WebPortal_OH_FI.Web;

// ReSharper disable once CheckNamespace
namespace MSLC.StatLog.Controllers
{
    public partial class LoggedInBaseController
    {
        partial void OnActionExecutingPartial(ActionExecutingContext filterContext)
        {
            if (!OHFISession.IsSSO) return;

            //call a keep alive fuction via token.
            var authToken = OHFISession.AuthToken;
            if (!string.IsNullOrEmpty(authToken))
            {
                OMEFunctions.KeepAlive(authToken);
            }
        }
    }
}