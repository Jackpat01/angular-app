// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ganss.Xss;
using MLSC.EDSProxy.Models;
using MSLC.ArumProxy.Models;
using MSLC.EDSProxy;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.URL;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.EDS;
using MSLC.Portal.Utility.Proxies;
using MSLC.Portal.Utility.Templates;
using MSLC.StatLog.BL.Email;
using MSLC.StatLog.BL.Role;
using MSLC.StatLog.BL.StatLogBL;
using MSLC.StatLog.Configuration;
using MSLC.StatLog.MVC.Email;
using MSLC.StatLog.MVC.ViewModels;
using MSLC.StatLog.UI.Models;
using Serilog;
using static System.String;
using static MSLC.StatLog.UI.Models.StatLogMapping;
using EmailParameters = MSLC.StatLog.MVC.Email.EmailParameters;

namespace MSLC.StatLog.Controllers
{
    public class CommunicationController : LoggedInBaseController
    {
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult ViewEmail(string sHistoryID)
        {
            int historyID = ClientMappingStatLog.ToInternalID(sHistoryID, StatLogScopes.HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "CommunicationController", "ViewEmail", "Statlog:View");

            if (map.Error)
                return ErrorJson(map.ErrorMessage);

            if (!map.History.Viewable(new []{ RetrieveSession.CurrentRole }))
                return ErrorJson("Error: This history is not viewable.");

            if (map.History.Event.ID != StatLogConfiguration.EventIDForEmail)
                return ErrorJson("Error: View Email functionality is not enabled for this History.");

            // We might need to take in mutliple comma delimited emailIDs in the future.
            var validEDSEmailID = int.TryParse(map.History.EDSEmailIDS, out int edsEmailID);
            if (!validEDSEmailID)
                return ErrorJson("Error: Could not retrieve a the email associated with this History, please contact support.");

            var emailLogFull = EDSTools.GetEmailByID(RetrieveSession.Classification, edsEmailID);

            var viewModel = new SendEmailViewModel.ViewOnly
            {
                sHistoryID = sHistoryID,
                Email_To = emailLogFull.Recipients,
                Email_ReplyTo = emailLogFull.Reply_To,
                Email_Subject = emailLogFull.Subject,
                Email_From = emailLogFull.From_Address,
                Email_Body = emailLogFull.Body
            };

            return View(viewModel);
        }

        #region "View - Send New Email"
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult SendEmail(string sCostReportID)
        {
            if (!StatLogConfiguration.CanSendStatLogEmail(RetrieveSession.ARUMUser))
                return ErrorJson("Error: Your account does not have the ability to send StatLog Emails.");

            int costReportID = ClientMappingStatLog.ToInternalID(sCostReportID, StatLogScopes.COST_REPORT_ID_STATLOG);

            Mapping map = MapByCostReportID(costReportID, "Statlog:Interact");
            if (map.Error)
                return ErrorJson(map.ErrorMessage);
            
            var roles = RoleBL.GetApplicationRoles(RetrieveSession.Classification);

            if (!roles.Any())
                return ErrorJson("Error: No roles found for application.");
            
            var roleItemList = new List<SelectListItem> { new SelectListItem { Text = "Please select...", Value = "" } };
            roleItemList.AddRange(roles.Select(role => new SelectListItem
            {
                Text = role.Name,
                Value = ClientMappingStatLog.ToClientID(role.ID.ToString(), StatLogScopes.ROLE_ID_STATLOG)
            }));

            ARUMProxy_User user = RetrieveSession.ARUMUser;
            var roleSelectList = new SelectList(roleItemList, "Value", "Text", null);

            var viewModel = new SendEmailViewModel.View
            {
                sCostReportID = sCostReportID,
                ProviderName = map.Provider.Name,
                Email_Type = "Select...",
                Email_SenderName = user.FirstName + " " + user.LastName,
                Email_Subject = $"{map.Provider.Name} - {map.StatLog.EndDate.ToShortDateString()}",
                Email_SenderEmail = user.Email,
                Email_MinsToWait = "1",
                Email_ReplyTo = user.Email,
                TypeList = roleSelectList 
            };
            
            return View(viewModel);
        }

        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public JsonResult GetUsersByType(string sCostReportID, string sRoleID)
        {
            if (!StatLogConfiguration.CanSendStatLogEmail(RetrieveSession.ARUMUser))
                throw new Exception("Error: Your account does not have the ability to send StatLog Emails.");

            int costReportID = ClientMappingStatLog.ToInternalID(sCostReportID, StatLogScopes.COST_REPORT_ID_STATLOG);
            int roleID = ClientMappingStatLog.ToInternalID(sRoleID, StatLogScopes.ROLE_ID_STATLOG);

            Mapping map = MapByCostReportID(costReportID, "Statlog:Interact");
            if (map.Error)
                throw new Exception(map.ErrorMessage);

            if (roleID < 1)
                throw new Exception("Error: Could not parse role, please log out and try again.");

            var roles = RoleBL.GetApplicationRoles(RetrieveSession.Classification);

            if (!roles.Any())
                throw new Exception("Error: No roles found for application.");

            var _arumUserProxy = ARUMUser.GetArumServiceProxy(RetrieveSession.Classification);
            var ItemList = new List<SelectListItem>();

            var selectedRole = roles.FirstOrDefault(r => r.ID == roleID);
            if(selectedRole == null)
                throw new Exception("Error: Invalid role selection.");

            if (selectedRole.FileAccess.Trim().ToUpper() == "A")
            {
                var roleUsers = _arumUserProxy.GetUsersByApplicationRoleID(roleID, "Communication");
                var roleList = roleUsers.UsersByRole
                    .Where(singleUser => singleUser.FirstName != "SYSTEM" && singleUser.LastName != "SYSTEM" &&
                                         singleUser.IsActive)
                    .Select(singleUser => new SelectListItem
                    {
                        Text = singleUser.FirstName + " " + singleUser.LastName,
                        Value = singleUser.Email
                    });
                ItemList.AddRange(roleList);
            }
            else
            {
                var providerUsers = _arumUserProxy.GetProviderUsersByApplicationIDAndProviderID(map.Provider.ARUMProviderID, "Communication");
                var roleList = providerUsers.providerUsers
                    .Where(providerUser => providerUser.FirstName != "SYSTEM" && providerUser.LastName != "SYSTEM")
                    .Where(providerUser => providerUser.HighestRole != null)
                    .Where(providerUser => selectedRole.Name.Trim().ToUpper() == "ALL" ||
                                           providerUser.HighestRole.Trim().ToUpper() ==
                                           selectedRole.Name.Trim().ToUpper())
                    .Select(providerUser => new SelectListItem
                    {
                        Text = providerUser.FirstName + " " + providerUser.LastName,
                        Value = providerUser.Email
                    });
                ItemList.AddRange(roleList);
            }

            return Json(ItemList, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult Send(SendEmailViewModel.Send viewModel)
        {
            int costReportID = ClientMappingStatLog.ToInternalID(viewModel.sCostReportID, StatLogScopes.COST_REPORT_ID_STATLOG);
            Mapping map = MapByCostReportID(costReportID, "Statlog:Interact");

            if (!StatLogConfiguration.CanSendStatLogEmail(RetrieveSession.ARUMUser))
                return StatlogMessageRedirect(map, "Error: Your account does not have the ability to send StatLog Emails.");

            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            ARUMProxy_User user = RetrieveSession.ARUMUser;

            if (!ModelState.IsValid ||
                viewModel.Email_To == null ||
                viewModel.Email_To.Length <= 0 ||
                IsNullOrEmpty(viewModel.Email_Subject) ||
                IsNullOrEmpty(viewModel.Email_Message) ||
                IsNullOrEmpty(viewModel.Email_SenderName) ||
                IsNullOrWhiteSpace(viewModel.Email_SenderName) ||
                IsNullOrWhiteSpace(viewModel.Email_SenderEmail) ||
                IsNullOrEmpty(viewModel.Email_MinsToWait))
            {
                return StatlogMessageRedirect(map, "One or more required fields is empty.");
            }
            
            // TODO: Validate that the Email_To has valid emails. 
            viewModel.Email_To = viewModel.Email_To.Where(n => !IsNullOrWhiteSpace(n)).ToArray();

            //Sanitize email to prevent XSS
            var sanitizer = new HtmlSanitizer();
            viewModel.Email_Message = sanitizer.Sanitize(viewModel.Email_Message);

            var emailParameters = new EmailParameters(viewModel);
            
            #region Send Email

            if (!ValidateEmails.Validate(emailParameters))
                return StatlogMessageRedirect(map, "One or more recipients is an invalid email address.");

            var webPortalURL = string.Empty;
            if (RetrieveSession.Classification != null)
            {
                webPortalURL = !string.IsNullOrEmpty(RetrieveSession.Classification.URL) ? RetrieveSession.Classification.URL : URL.GetBaseURL(false);
            }

            string emailBody = Templates.GetTemplate("StatLogTemplates\\StandardEmail.txt");
            emailBody = emailBody.Replace(EmailReplacementTypes.BODY.ReplacementValue(), emailParameters.Body);
            emailBody = emailBody.Replace(EmailReplacementTypes.IMG_LOGO.ReplacementValue(), PortalConfiguration.ImageLogo);
            emailBody = emailBody.Replace(EmailReplacementTypes.URL.ReplacementValue(), webPortalURL);
            emailBody = emailBody.Replace(EmailReplacementTypes.IGNORED_RECIPIENTS.ReplacementValue(), Empty);
            emailBody = emailBody.Replace(EmailReplacementTypes.EMAIL_SIGNATURE.ReplacementValue(), StatlogARUMConfiguration.GetEmailSignature(RetrieveSession.Classification));

            var emaillogID = EDSTools.InsertEmail(RetrieveSession.Classification, new EDSEmailParams()
            {
                emailParams = new EmailParams()
                {
                    emailParameters = new EDSProxy.EmailParameters()
                    {
                        Type = "External",
                        NotificationType = $"{PortalConfiguration.Application} StatLog Notification",
                        Recipients = emailParameters.Recipients,
                        Body = emailBody,
                        From_Address = $"{viewModel.Email_SenderName}<{viewModel.Email_SenderEmail}>",
                        Importance = "Normal",
                        Subject = emailParameters.Subject,
                        URL = webPortalURL,
                        Minutes2Wait = 1,
                        Reply_To = viewModel.Email_ReplyTo,
                        Sensitivity = "Normal"
                    }
                }
            });
            
            if (emaillogID.emailLog.ID <= 0)
                return StatlogMessageRedirect(map, "Email failed to be inserted into the database.");

            StatLogBL.InsertHistory(RetrieveSession.Classification, map.StatLog.CRID,
                StatLogConfiguration.EventIDForEmail, null, "", false, user.Email, null, DateTime.Now,
                null, null, emaillogID.emailLog.ID.ToString(), user.UserID, "CommunicationController",
                "Send", map.Provider.ARUMProviderID);
            
            #endregion

            return StatlogMessageRedirect(map, "Email queued successfully.");

        }
        #endregion

        #region Send All Providers Email
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:AllProvidersEmail")]
        public ActionResult SendProviderEmail()
        {
            if (!StatLogConfiguration.EnableSendEmailToALLProviderUsers)
                throw new Exception("Feature not enabled");
            
            var viewModel = new ProviderEmailViewModel.View
            {
                Email_MinsToWait = "1"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:AllProvidersEmail")]
        [ValidateAntiForgeryToken]
        // TODO: Clean this endpoint up.
        public ActionResult SendProviders(ProviderEmailViewModel.Send viewModel)
        {
            if (!StatLogConfiguration.EnableSendEmailToALLProviderUsers)
                throw new Exception("Feature not enabled");

            var myArumUser = RetrieveSession.ARUMUser;

            var Email_SenderName = myArumUser.FirstName + " " + myArumUser.LastName;
            var Email_SenderEmail = myArumUser.Email;
            //var Email_MinsToWait = "1";
            var Email_ReplyTo = myArumUser.Email;

            try
            {
                var _arumUserProxy = ARUMUser.GetArumServiceProxy(RetrieveSession.Classification);
                var myARUMProvidersByApplicationID = _arumUserProxy.GetApplicationProvidersByApplicationID(int.Parse(myArumUser.UserID.ToString()), "SendALLProviders");
                List<string> MyARUMAllProviderUsers = new List<string>();
                List<string> MyARUMAllProviderUsersNoDubs = new List<string>();

                //Get All Providers that user has access  
                foreach (var provider in myARUMProvidersByApplicationID.myProvidersByApplication)
                {
                    //Get All users associated to provider (ID)
                    var myARUMProviderUsersByApplicationAndProviderID = _arumUserProxy.GetProviderUsersByApplicationIDAndProviderID(provider.ProviderID, "SendALLProviders");
                    foreach (var providerUsers in myARUMProviderUsersByApplicationAndProviderID.providerUsers)
                    {
                        MyARUMAllProviderUsers.Add(providerUsers.Email);
                    }
                    //Remove all dublicates 
                    MyARUMAllProviderUsersNoDubs = MyARUMAllProviderUsers.Distinct().ToList();
                }

                string[] Email_To = MyARUMAllProviderUsersNoDubs.ToArray();

                if (Email_To == null || Email_To.Length <= 0) 
                    TempData["Message"] = "Selected providers have no users.";
                else if(IsNullOrEmpty(viewModel.Email_Subject)) 
                    TempData["Message"] = "Email is missing a subject.";
                else if (IsNullOrEmpty(viewModel.Email_Message))
                    TempData["Message"] = "Email is missing a message.";
                else if (IsNullOrWhiteSpace(Email_SenderName))
                    TempData["Message"] = "Email sender is missing a name.";
                else if (IsNullOrWhiteSpace(Email_SenderEmail))
                    TempData["Message"] = "Sender is missing a email.";
                else if (IsNullOrEmpty(viewModel.Email_MinsToWait))
                    TempData["Message"] = "Minutes to wait has not been filled out.";
                else
                {
                    #region Initialization

                    Email_To = Email_To.Where(n => !IsNullOrWhiteSpace(n)).ToArray();
                    
                    var toFormatted = Email_To.Any(e => !IsNullOrWhiteSpace(e))
                        ? $"{Join(";", Email_To.Where(e => !IsNullOrWhiteSpace(e)).ToArray())};"
                        : "";

                    //Sanitize email to prevent XSS
                    var sanitizer = new HtmlSanitizer();
                    viewModel.Email_Message = sanitizer.Sanitize(viewModel.Email_Message);

                    var EmailParameters = new EmailParameters("", toFormatted, "", "", Email_SenderName,
                        Email_SenderEmail, Email_ReplyTo, viewModel.Email_Subject, viewModel.Email_Message,
                        viewModel.Email_MinsToWait);

                    #endregion

                    #region Send Email

                    var webPortalURL = string.Empty;
                    if (RetrieveSession.Classification != null)
                    {
                        webPortalURL = !string.IsNullOrEmpty(RetrieveSession.Classification.URL) ? RetrieveSession.Classification.URL : URL.GetBaseURL(false);
                    }

                    string EmailBody = Templates.GetTemplate("StatLogTemplates\\StandardEmail.txt");
                    EmailBody = EmailBody.Replace(EmailReplacementTypes.BODY.ReplacementValue(), EmailParameters.Body);
                    EmailBody = EmailBody.Replace(EmailReplacementTypes.IMG_LOGO.ReplacementValue(),
                        PortalConfiguration.ImageLogo);
                    EmailBody = EmailBody.Replace(EmailReplacementTypes.URL.ReplacementValue(), webPortalURL);
                    EmailBody = EmailBody.Replace(EmailReplacementTypes.IGNORED_RECIPIENTS.ReplacementValue(),
                        Empty);
                    EmailBody = EmailBody.Replace(EmailReplacementTypes.EMAIL_SIGNATURE.ReplacementValue(), StatlogARUMConfiguration.GetEmailSignature(RetrieveSession.Classification));

                    //Loop through Email_To users 
                    foreach (var emailUser in Email_To)
                    {
                        if (!PortalConfiguration.EnableSendEmailToExternalUsers &&
                            !emailUser.ToUpper().EndsWith("@MSLC.COM"))
                            continue;

                        if (PortalConfiguration.EnvironmentString.ToUpper() != "Production" &&
                            !emailUser.ToUpper().EndsWith("@MSLC.COM"))
                            continue;

                        var _minutes2wait = viewModel.Email_MinsToWait.Trim();

                        if (IsNullOrWhiteSpace(_minutes2wait))
                            _minutes2wait = "1"; //default to 1 minute

                        var emaillogID = EDSTools.InsertEmail(RetrieveSession.Classification, new EDSEmailParams()
                        {
                            emailParams = new EmailParams()
                            {
                                emailParameters = new EDSProxy.EmailParameters()
                                {
                                    Type = "External",
                                    NotificationType = $"{PortalConfiguration.Application} Provider Emails",
                                    Recipients = emailUser,
                                    Body = EmailBody,
                                    From_Address = Format("{0}", PortalConfiguration.EmailFrom),
                                    Importance = "Normal",
                                    Subject = EmailParameters.Subject,
                                    URL = webPortalURL,
                                    Minutes2Wait = Int32.Parse(_minutes2wait),
                                    Reply_To = Email_ReplyTo,
                                    Sensitivity = "Normal"
                                }
                            }
                        });


                        //var emaillogID = EmailLogBL.Insert(EmailParameters);
                        if (emaillogID.emailLog.ID > 0)
                        {
                            //int EventID = StatLogConfiguration.EventIDForEmail;
                            //StatLogBL.InsertHistory(viewModel.CostReportID, EventID, 0, "", false, user.Email, null, DateTime.Now, null, null, emaillogID.emailLog.ID.ToString());
                            TempData["Message"] = "Email(s) queued successfully.";
                        }
                        else
                        {
                            string jsonStringException = Newtonsoft.Json.JsonConvert.SerializeObject(EmailParameters);
                            Exception ex = new Exception(
                                "Email failed to be inserted into the database (SendProviders) - " +
                                jsonStringException);
                            Log.Error(ex, ex.Message);
                        }
                    }

                    #endregion
                }

                return RedirectToAction("SendProviderEmail", "Communication");
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }
            return RedirectToAction("Login", "Home");
        }
        #endregion

        #region Helper methods
        private ActionResult StatlogMessageRedirect(int? providerID, int? costReportID, string message)
        {
            TempData["CostReportID"] = costReportID;
            TempData["ARUMProviderID"] = providerID;
            TempData["Message"] = message;
            return RedirectToAction("Index", "StatLog");
        }
        private ActionResult StatlogMessageRedirect(Mapping request, string message) =>
            StatlogMessageRedirect(request?.Provider?.ARUMProviderID, request?.StatLog?.CRID, message);

        private static ActionResult ErrorJson(string message) => 
            new JsonResult { JsonRequestBehavior = JsonRequestBehavior.AllowGet, Data = new { Error = message } };
        #endregion
    }
}