using MSLC.ApplicationProxy;
using MSLC.ApplicationProxy.Interfaces;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.IP;
using MSLC.Portal.Utility.Proxies;
using Omu.ValueInjecter;
using Serilog;
using SSO.Functions;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using WebPortal_OH_FI.Common.SSO.Models;

namespace WebPortal_OH_FI.Web
{
    public class SSOController : Controller
    {
        #region Properties
        private ARUMApplicationServiceProxy _arumApplicationClient;
        #endregion

        public SSOController()
        {
            var allClassifications = PortalConfiguration.GetAllClassifications();
            _arumApplicationClient = ARUMApplication.GetARUMApplicationProxy(allClassifications.FirstOrDefault());
        }

        //[Route("SSO/Index/{UserName?}")]
        //public ActionResult Index(string UserName)
        //{
        //    //possible UserNames
        //    //E2Estateuser Old  New  - OHFIState
        //    //E2Eprovideruser Old  New  - OHFIProvider
        //    //E2Eprovagentuser Old  New  - OHFIProvagent
        //    try
        //    {
        //        Session["IsSSO"] = true;
        //        string returnURL = WebPortal_OH_FI.Common.Functions.OME_ReferralURL;

        //        var externalIPAddress = IP.GetUserIP_RemoteAddR();
        //        ARUMProxy_UserExtended arumSSOUser = null;

        //        //var referralURL = Request.Headers["Referer"].ToString().ToUpper().Trim();

        //        //if (referralURL != WebPortal_OH_FI.Common.Functions.OME_ReferralURL)
        //        //{
        //        //    return RedirectToAction("Error","SSO");
        //        //}

        //        if (string.IsNullOrEmpty(UserName))
        //        {
        //            return LogWebError("Username not provided", returnURL);
        //        }

        //        var arumProxyUserExtended = OMEFunctions.GetSSOUser(UserName);


        //        if (arumProxyUserExtended?.OMEAssignedUserProvider.Result.ReturnStatus == "ERROR" || arumProxyUserExtended?.OMEAssignedUserProvider.User[0] == null)
        //        {
        //            var errorResult = arumProxyUserExtended?.OMEAssignedUserProvider.Result;
        //            TempData["Message"] = "User not found, please contact support.";
        //            return LogWebError(errorResult.ReturnStatus + "\r\n" + errorResult.ReturnStatusCode + "\r\n" + errorResult.ReturnStatusDescription, returnURL, UserName, arumProxyUserExtended?.OMEAssignedUserProvider);
        //        }

        //        var user = arumProxyUserExtended.OMEAssignedUserProvider.User[0];


        //        if (user.UserRoles.Any(c => c.RoleName.Trim().ToUpper() == "PROVIDERADMINISTRATOR") ||
        //            user.UserRoles.Any(c => c.RoleName.Trim().ToUpper() == "PROVIDERAGENT"))
        //        {
        //            if (!user.Provider
        //                    .Any(c => c.ProvActiveIndicator == "Y"))
        //            {
        //                TempData["Message"] = user.UserRoles.Any(c => c.RoleName.Trim().ToUpper() == "PROVIDERADMINISTRATOR") ?
        //                    //Provider Admin error message
        //                    "Account is not associated to an active provider. Please follow the procedures you were provided by Maximus." :
        //                    //Provider Agent error message
        //                    "Account is not associated to an active provider. Please contact your provider to resolve the issue.";

        //                return LogWebError("Account is not associated to an active provider", returnURL, UserName, arumProxyUserExtended?.OMEAssignedUserProvider);
        //            }
        //        }


        //        if (WebPortal_OH_FI.Common.Functions.LogSSO)
        //        {
        //            var LogFolder = WebPortal_OH_FI.Common.Functions.SSO_Logs;
        //            var pathWithBINandLogFolder = System.IO.Path.Combine(LogFolder, "Logs", DateTime.Now.Year.ToString(), CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month));
        //            var fileName = $"JSONObjectForSSOUser_{UserName}_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.log";
        //            var fulljsonLogsPathWithFileName = System.IO.Path.Combine(LogFolder, pathWithBINandLogFolder, fileName);

        //            if (!System.IO.Directory.Exists(pathWithBINandLogFolder))
        //            {
        //                System.IO.Directory.CreateDirectory(pathWithBINandLogFolder);
        //            }

        //            var json = Newtonsoft.Json.JsonConvert.SerializeObject(arumProxyUserExtended.OMEAssignedUserProvider, Newtonsoft.Json.Formatting.Indented);
        //            if (!string.IsNullOrEmpty(json))
        //            {
        //                System.IO.File.WriteAllText(fulljsonLogsPathWithFileName, json);
        //            }
        //        }

        //        var arumTokens = WebPortal_OH_FI.Common.Functions.GetAllClassifications().Select(c => c.ARUMToken).ToList();

        //        arumSSOUser = SSO.Functions.ARUMFunctions.SSOUser(arumProxyUserExtended, externalIPAddress);


        //        if (arumSSOUser == null)
        //        {
        //            return LogWebError("ARUMSSOUser was unable to be created", returnURL, UserName, arumProxyUserExtended?.OMEAssignedUserProvider);
        //        }

        //        IARUMUserValidation arumValidations;
        //        //If Session variable is null or this is a different account, create new Session values
        //        if (Session["ARUMUserValidations"] == null || (Session["AuthToken"] != null && (string)Session["AuthToken"] != arumProxyUserExtended.AuthToken))
        //        {
        //            arumValidations = _arumApplicationClient.GetUserRolesByEmailNoPasswordAndApplications(
        //                Common.Functions.GetARUMTokensFromClassifications,
        //                arumSSOUser.Email,
        //                "SSO",
        //                externalIPAddress
        //            );
        //            var filteredListOfOnlyActiveTokens = arumValidations.lstARUMUsers.Where(c => c.IsActive).ToList();
        //            arumValidations.lstARUMUsers = filteredListOfOnlyActiveTokens;
        //            Session["ARUMUserValidations"] = arumValidations;
        //            Session["AuthToken"] = arumProxyUserExtended.AuthToken;
        //            Session["ARUMUser"] = arumSSOUser;

        //            return RedirectToAction("Projects", "Projects");
        //        }
        //        //If this is same user as before, proceed to Projects page
        //        else if (Session["ARUMUserValidations"] != null && (string)Session["AuthToken"] == arumProxyUserExtended.AuthToken)
        //            return RedirectToAction("Projects", "Projects");

        //        return LogWebError("User was unable to be set", returnURL, arumProxyUserExtended?.OMEAssignedUserProvider.User[0].UserName, arumProxyUserExtended?.OMEAssignedUserProvider);

        //    }
        //    catch (Exception ex)
        //    {
        //        SendError(ex, UserName);
        //        return LogWebError(ex.Message, UserName);
        //    }

        //}

        private void SendError(Exception ex, string SSOUsername)
        {
            var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
            Log.Error(ex, $"Exception for SSO User {SSOUsername} at {Request.Url.OriginalString} Exception: {exMessage}");
        }

        [Route("SSO/Error")]
        public ActionResult Error()
        {
            return View();
        }

        [HttpGet]
        [Route("SSO/Login")]
        public ActionResult Login(string AuthToken, string ProviderID, string ReturnURL)
        {
            return LogUserIntoWebPortal(AuthToken, ProviderID, ReturnURL);
        }

        //[HttpPost]
        //[Route("SSO/Login")]
        //public void Login(string AuthToken, string ProviderID, string ReturnURL)
        //{
        //    LogUserIntoWebPortal(AuthToken, ProviderID, ReturnURL);
        //}

        private ActionResult LogUserIntoWebPortal(string AuthToken, string ProviderID, string ReturnURL)
        {
            Session["IsSSO"] = true;
            if (string.IsNullOrEmpty(ReturnURL))
                ReturnURL = WebPortal_OH_FI.Common.Functions.OME_ReferralURL;

            var OMETimer = new Stopwatch();
            var allSSOTimer = new Stopwatch();
            var logTimer = new Stopwatch();
            allSSOTimer.Start();
            string OMETime = "N/A";
            string allSSOTime = "N/A";
            string logTime = "N/A";
            string authTokenBytes = "N/A";
            ARUMProxy_UserExtended arumProxyUserExtended = null;

            try
            {
                var externalIPAddress = IP.GetUserIP_RemoteAddR();
                ARUMProxy_UserExtended arumSSOUser = null;

                if (string.IsNullOrEmpty(AuthToken))
                {
                    return LogWebError("Authentication Token not provided", ReturnURL, authTokenBytes, OMETime, allSSOTime);
                }

                authTokenBytes = (AuthToken.Length * sizeof(Char)).ToString();

                OMETimer.Start();
                arumProxyUserExtended = OMEFunctions.GetSSOUserByToken(AuthToken);
                OMETimer.Stop();
                TimeSpan OMETimeTaken = OMETimer.Elapsed;
                OMETime = OMETimeTaken.ToString(@"m\:ss\.fff");

                if (arumProxyUserExtended == null || arumProxyUserExtended.OMEAssignedUserProvider == null || !arumProxyUserExtended.OMEAssignedUserProvider.User.Any())
                {
                    return LogWebError("User not found", ReturnURL, authTokenBytes, OMETime, allSSOTime, "UserNotFound", arumProxyUserExtended?.OMEAssignedUserProvider);
                }

                var user = arumProxyUserExtended.OMEAssignedUserProvider.User[0];

                var arumTokens = PortalConfiguration.GetAllClassifications().Select(c => c.ARUMToken).ToList();

                arumSSOUser = SSO.Functions.ARUMFunctions.SSOUser(arumProxyUserExtended, externalIPAddress);

                IUserValidation applicationValidations;

                applicationValidations = _arumApplicationClient.GetUserRolesByEmailNoPasswordAndApplications(
                    Common.Functions.GetARUMTokensFromClassifications,
                    arumSSOUser.Email,
                    "SSO",
                    externalIPAddress
                );

                if (applicationValidations?.lstARUMUsers == null || !applicationValidations.lstARUMUsers.Any())
                {
                    throw new Exception("Unable to retrieve user from ARUM");
                }

                var arumValidations = HelperFunctions.MapApplicationProxyToUserProxy(applicationValidations);
                var filteredListOfOnlyActiveTokens = arumValidations.lstARUMUsers.Where(c => c.IsActive).ToList();
                arumValidations.lstARUMUsers = filteredListOfOnlyActiveTokens;

                if (arumValidations?.lstARUMUsers == null || !arumValidations.lstARUMUsers.Any())
                {
                    throw new Exception("Unable to map application objects to user objects");
                }

                arumSSOUser = arumSSOUser.InjectFrom(arumValidations.lstARUMUsers.FirstOrDefault()) as ARUMProxy_UserExtended;

                Session["ARUMUserValidations"] = arumValidations;

                Session["AuthToken"] = AuthToken;
                Session["ARUMUser"] = arumSSOUser;

                allSSOTimer.Stop();
                TimeSpan SSOTimeTaken = allSSOTimer.Elapsed;
                allSSOTime = SSOTimeTaken.ToString(@"m\:ss\.fff");
                //Log successful user
                if (WebPortal_OH_FI.Common.Functions.LogSSO)
                {
                    logTimer.Start();
                    var LogFolder = WebPortal_OH_FI.Common.Functions.SSO_Logs;
                    var pathWithBINandLogFolder = System.IO.Path.Combine(LogFolder, "Logs", DateTime.Now.Year.ToString(), CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month));
                    var fileName = $"JSONObjectForSSOUser_{user.UserName}_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.log";
                    var fulljsonLogsPathWithFileName = System.IO.Path.Combine(LogFolder, pathWithBINandLogFolder, fileName);

                    if (!System.IO.Directory.Exists(pathWithBINandLogFolder))
                    {
                        System.IO.Directory.CreateDirectory(pathWithBINandLogFolder);
                    }

                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(user, Newtonsoft.Json.Formatting.Indented);
                    if (!string.IsNullOrEmpty(json))
                    {
                        System.IO.File.WriteAllText(fulljsonLogsPathWithFileName, json);
                    }
                    logTimer.Stop();
                    TimeSpan logTimeTaken = logTimer.Elapsed;
                    logTime = logTimeTaken.ToString(@"m\:ss\.fff");
                    HelperFunctions.LogOMETiming(authTokenBytes, OMETime, allSSOTime, logTime, user.UserName, false);
                }

                return RedirectToAction("Projects", "Projects");
            }
            catch (Exception ex)
            {
                if (OMETimer.IsRunning)
                {
                    OMETimer.Stop();
                    TimeSpan timeTaken = OMETimer.Elapsed;
                    OMETime = timeTaken.ToString(@"m\:ss\.fff");
                }
                if (allSSOTimer.IsRunning)
                {
                    allSSOTimer.Stop();
                    TimeSpan timeTaken = allSSOTimer.Elapsed;
                    allSSOTime = timeTaken.ToString(@"m\:ss\.fff");
                }
                if (logTimer.IsRunning)
                {
                    logTimer.Stop();
                    TimeSpan timeTaken = logTimer.Elapsed;
                    logTime = timeTaken.ToString(@"m\:ss\.fff");
                }
                return LogWebError(ex.ToString(), ReturnURL, authTokenBytes, OMETime, allSSOTime, arumProxyUserExtended?.OMEAssignedUserProvider?.User[0]?.UserName, arumProxyUserExtended?.OMEAssignedUserProvider);
            }
        }
        public ActionResult LogOut()
        {
            return Redirect(WebPortal_OH_FI.Common.Functions.OME_ReferralURL);
        }

        public ActionResult LogWebError(string errorMessage, string returnURL, string AuthTokenSize, string OMEtime, string AllSSOTime, string UserName = "UsernameOrTokenNotProvided", UserAgentProvAssnModel OMEAssignedUserProvider = null)
        {
            Log.Error($"SSO Error: {errorMessage}");
            if (WebPortal_OH_FI.Common.Functions.LogSSO)
            {
                var logTimer = new Stopwatch();
                logTimer.Start();
                HelperFunctions.LogError(errorMessage, "SSOController", UserName, OMEAssignedUserProvider);
                logTimer.Stop();
                TimeSpan logTimeTaken = logTimer.Elapsed;
                var logTime = logTimeTaken.ToString(@"m\:ss\.fff");
                HelperFunctions.LogOMETiming(AuthTokenSize, OMEtime, AllSSOTime, logTime, UserName, true);
            }
            return Redirect(returnURL);
        }
    }
}