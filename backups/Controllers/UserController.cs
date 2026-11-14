using MSLC.StatLog.Controllers;
using Serilog;
using System;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Controllers
{
    /// <summary>
    /// This Controller is used to pull certain values from  the ARUM User Session object and display them on the page.
    /// </summary>
    public class UserController : LoggedInBaseController
    {
        /// <summary>
        /// Names for user.
        /// </summary>
        /// <returns></returns>
        public PartialViewResult NameForUser()
        {

            try
            {
                return PartialView();
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
                return null;
            }

        }
        /// <summary>
        /// Applications the name of the user view model partial user.
        /// </summary>
        /// <returns>
        /// Returns the UserName to the view.
        /// </returns>
        public PartialViewResult RoleForUser()
        {
            try
            {
                return PartialView();
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
                return null;
            }
        }
    }
}