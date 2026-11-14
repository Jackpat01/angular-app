using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.StatLog.Controllers;
using Serilog;
using System;
using System.Web.Mvc;
using WebPortal_OH_FI.Web;

namespace WebPortal_OH_FI.Controllers
{
    public class SecureController : LoggedInBaseController
    {
        public SecureController()
        {
        }

        // GET: Secure
        [ClearProviderID]
        public ActionResult Index()
        {
            try
            {
                Log.Information("Testing SL 2.15.0");
                var myUser = RetrieveSession.ARUMUser;
                if (myUser != null)
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
            }
            return RedirectToAction("Login", "Home");
        }
        /// <summary>
        /// Logs the out.
        /// </summary>
        /// <returns>
        /// Clears the Session objects and Redirects to Login page.
        /// </returns>
        [HttpGet]
        public ActionResult LogOut()
        {
            //Session.Clear();
            //Session.Abandon();
            if (OHFISession.IsSSO)
            {
                Session.Clear();
                Session.Abandon();
                return RedirectToAction("LogOut", "SSO");
            }
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Home");
        }
        
    }
}