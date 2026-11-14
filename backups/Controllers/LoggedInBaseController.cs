// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System.Web.Mvc;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.Utility;
using Serilog;

namespace MSLC.StatLog.Controllers
{

    public partial class LoggedInBaseController : Controller
    {
        /// <summary>
        /// This requires all users to be logged in.  
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext != null)
                if (!filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    var myUser = RetrieveSession.ARUMUser;
                    if (myUser == null || myUser.UserID == -1 || string.IsNullOrWhiteSpace(myUser.Email))
                        filterContext.Result = RedirectToAction(
                            PortalConfiguration.MissingARUMUserRedirectAction,
                            PortalConfiguration.MissingARUMUserRedirectController);

                    if (RetrieveSession.Classification == null)
                        filterContext.Result = RedirectToAction(
                            PortalConfiguration.MissingClassificationRedirectAction, 
                            PortalConfiguration.MissingClassificationRedirectController);
                }
                else
                {
                    //Ajax calls do not support 302 redirects.
                    //If you wanted to, you could return a json object with an error message and a url as properties 
                    //you could then set the http code to an error code and return the error json object
                    //then the consumer could handle it accordingly.
                
                    //not really worth it unless we did a whole web portal redesign.
                }
            OnActionExecutingPartial(filterContext);

            base.OnActionExecuting(filterContext);
        }

        partial void OnActionExecutingPartial(ActionExecutingContext filterContext);
    }
}