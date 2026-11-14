using MSLC.ApplicationProxy;
using MSLC.ArumProxy;
using MSLC.ArumProxy.Models;
using MSLC.FileProxy;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.IP;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Encoding;
using MSLC.Portal.Utility.Proxies;
using MSLC.StatLog.Controllers;
using System;
using System.Linq;
using System.Web.Mvc;
using WebPortal_OH_FI.Models;
using WebPortal_OH_FI.Web;

namespace WebPortal_OH_FI.Controllers
{
    public class ProjectsController : NoClassificationBaseController
    {
        private ArumServiceProxy _arumUserProxy;
        private FileServiceProxy _arumFileProxy;
        private ARUMApplicationServiceProxy _arumApplicationProxy;

        public ProjectsController()
        {
	        var allClassifications = PortalConfiguration.GetAllClassifications();
            _arumUserProxy = ARUMUser.GetArumServiceProxy(allClassifications.First());
            _arumFileProxy = ARUMFile.GetARUMFileServiceProxy(allClassifications.First());
            _arumApplicationProxy = ARUMApplication.GetARUMApplicationProxy(allClassifications.First());
        }

        /// <summary>
        /// Logs the user out.
        /// </summary>
        /// <returns>
        /// Clears the Session objects and Redirects to Login page.
        /// </returns>
        [HttpGet]
        public ActionResult LogOut()
        {
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

        [HttpGet]
        [ClearProviderID]
        public ActionResult Projects()
        {
            var arumUser = RetrieveSession.ARUMUser;
            if (arumUser == null)
            {
                if (!OHFISession.IsSSO)
                {
                    return RedirectToAction("Login", "Home");
                }
                else
                {
                    return RedirectToAction("Error", "SSO");
                }
            }

            // reset any session variables that would cause issues from any previous project selection.
            OHFISession.ResetForNewProject();

            #region Cost Report Settings
            Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] = null;
            Session[SESSION_KEYS_OCRT.PROVIDER_DATA] = null;
            Session[SESSION_KEYS_OCRT.SCHEDULE_GRID_MASK] = null;
            Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_LAST] = null;
            Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_FIRST] = null;
            Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNTER] = null;
            Session[SESSION_KEYS_OCRT.SCHED_LAST] = null;
            Session[SESSION_KEYS_OCRT.SELECTED_TAB] = null;
            Session[SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED] = null;
            Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = null;
            Session[SESSION_KEYS_OCRT.SELECTED_SCHEDULE] = null;
            Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] = true;
            ClientMapping.ClearAllClientIdMappings();
            #endregion

            RetrieveSession.Classification = null;

            var myViewModel = new ProjectsViewModel.Projects();

            return View(myViewModel);
        }
        [HttpGet]
        public ActionResult SetProject(string cID)
        {
            var message = string.Empty;
            var myUser = RetrieveSession.ARUMUser;
            var pageName = "SetProject";
            var isSSOUser = OHFISession.IsSSOUser;

            if (myUser == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var myExternalIPAddress = IP.GetUserIP_RemoteAddR();
            var decodedProgramType = EncodingTools.GetStringDataFromBase64EncodedString(cID);
            if (string.IsNullOrEmpty(decodedProgramType))
            {
                return RedirectToAction("Projects", "Projects");
            }
            var classifications = PortalConfiguration.GetAllClassifications().ToList();
            if (classifications == null)
                throw new Exception(
                    "[Web.Config] - Classifications Settings element missing from PortalConfiguration");


            var singleClassification =
                classifications.FirstOrDefault(c =>
                    c.DisplayName.Trim().ToUpper() == decodedProgramType.ToString().ToUpper());

            if (singleClassification == null)
            {
                throw new Exception(
                    $"[Web.Config] - Classifications Settings not found for selected program type [{decodedProgramType}].");
            }

            _arumApplicationProxy = ARUMApplication.GetARUMApplicationProxy(singleClassification);
            var applicationRoles =
                _arumApplicationProxy.GetApplicationRoles(singleClassification.ARUMToken, false, pageName);
            if (applicationRoles == null)
            {
                message = "No Application Roles found, please contact support.";
                TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                return RedirectToAction("Projects", "Projects");
            }

            _arumUserProxy = ARUMUser.GetArumServiceProxy(singleClassification);
            var userConfirmation = _arumUserProxy.DoesUserExistForApplication(myUser.Email, pageName);
            if (userConfirmation == null || !userConfirmation.Results.Successful)
            {
                message = "User not found.";
                TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                return RedirectToAction("Projects", "Projects");
            }
            #region Login logic
            // Update IP for user

            var arumUser = _arumUserProxy.GetUserRolesByUserIDAndApplication(userConfirmation.UserID, pageName, myExternalIPAddress, true);

            if (!arumUser.Results.Successful)
            {
                message = arumUser.Results.Message;
                TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                return RedirectToAction("Projects", "Projects");
            }

            if (ARUMConfiguration.HasProviders(singleClassification))
            {
                if (arumUser.Providers == null)
                {
                    message = "No Providers Assigned to Current User.  Contact Support.  Click the icon below.";
                    TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                    return RedirectToAction("Projects", "Projects");
                }
            }

            if (arumUser.UserID == 0)
            {
                message = "Invalid UserID detected, please contact support.";
                TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                return RedirectToAction("Projects", "Projects");
            }
            
            if (arumUser.Roles == null && !arumUser.Roles.Any())
            {
                message = "User has no role assigned, please contact support.";
                TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                return RedirectToAction("Projects", "Projects");

            }
            if (applicationRoles == null)
            {
                message = "No Application Roles found for application, please contact support.";
                TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                return RedirectToAction("Projects", "Projects");
            }

            var TopLevelRole = arumUser.Roles[0].RoleName.ToUpper();
            var highestLevelRoleApplicationRole = applicationRoles?.lstApplicationRoles.FirstOrDefault(c => c.Role.Name.Trim().ToUpper() == TopLevelRole.Trim().ToUpper());
            if (highestLevelRoleApplicationRole == null)
            {
                message = "No roles found for user, please contact support.";
                TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                return RedirectToAction("Projects", "Projects");
            }



            if (highestLevelRoleApplicationRole?.ApplicationRole.FileAccess.Trim().ToUpper() == "PL")
            {
                if (arumUser.Providers == null)
                {
                    message = "No Providers assigned to user, please contact support";
                    TempData["Message"] = $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                    return RedirectToAction("Projects", "Projects");
                }

            }

            if (!isSSOUser)
            {
                message = UpdateIpAddressForARUMUser(arumUser.UserID, arumUser.IPAddress, myExternalIPAddress, pageName);
                if (!string.IsNullOrEmpty(message))
                {
                    TempData["Message"] =
                        $"Validation Message for selected program {singleClassification.DisplayName}<br/><br/>{message}";
                    return RedirectToAction("Projects", "Projects");
                }
            }
            #endregion

            // Set project type
            RetrieveSession.Classification = singleClassification;
            Session["InitialCatalogFromSession"] = null;
            Session["InitialCatalogFromSession"] = RetrieveSession.Classification?.ARUMToken;
            Session["AppNameFromSession"] = RetrieveSession.Classification?.Type;
            OHFISession.SetApplicationInformation(singleClassification);

            // Update ARUM User
            RetrieveSession.ARUMUser = arumUser as ARUMProxy_User;

            if (PortalConfiguration.EnableBB && (arumUser.BulletinBoards?.Any() ?? false))
            {
                var NewBulletinBoards =
                    arumUser.BulletinBoards.Count(c => c.IsNewBulletinBoard) +
                    arumUser.BulletinBoards.Count(c => c.AlwaysShow.HasValue && c.AlwaysShow.Value);

                if (NewBulletinBoards >= 1)
                {
                    return RedirectToAction("ShowAll", "BulletinBoard");
                }
            }


            return RedirectToAction("Index", "Secure");
        }
        private string UpdateIpAddressForARUMUser(int arumUserID, string currentIP, string externalIP, string PageName)
        {
            if (currentIP != externalIP)
            {
                var myARUMResults = _arumUserProxy.UpdateUserIPAddress(arumUserID, externalIP, PageName);
                if (!myARUMResults.Successful)
                {
                    return myARUMResults.Message;
                }


            }
            return string.Empty;
        }
    }
}