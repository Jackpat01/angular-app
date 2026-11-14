using ARUMFileWebService;
using MLSC.EDSProxy.Models;
using MSLC.ApplicationProxy.Interfaces.Models;
using MSLC.ArumProxy;
using MSLC.ArumProxy.Interfaces;
using MSLC.ArumProxy.Models;
using MSLC.BulkProcessing.BL;
using MSLC.BulkProcessing.Parameters;
using MSLC.EDSProxy;
using MSLC.FileProxy;
using MSLC.FileProxy.Interfaces;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.URL;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.EDS;
using MSLC.Portal.Utility.Email;
using MSLC.Portal.Utility.ExtensionMethods;
using MSLC.Portal.Utility.Proxies;
using MSLC.StatLog.Controllers;
using MSLC.StatLog.Models.ViewModels;
using Serilog;
using Syncfusion.Compression.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using WebPortal_OH_FI.Models;
using WebPortal_OH_FI.Web;
using static MSLC.Portal.Utility.EDS.EDSTools;

namespace WebPortal_OH_FI.Controllers
{
    /// <summary>
    /// Controls the uploading of Files to ARUM from web Portal.
    /// </summary>
    public class UploadController : LoggedInBaseController
    {
        private const string TYPE_MCAID = "MCAID";

        #region Properties

        private ArumServiceProxy _arumUserProxy;
        private FileServiceProxy _arumFileProxy;

        #endregion

        public UploadController()
        {
            _arumUserProxy = ARUMUser.GetArumServiceProxy(RetrieveSession.Classification);
            _arumFileProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
        }

        // GET: Upload
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns></returns>
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Portal:Upload")]
        public ActionResult Index()
        {
            try
            {
                var Message = string.Empty;
                if (!UploadDownloadConfiguration.EnableUpload)
                {
                    return RedirectToAction("Index", "Secure");
                }

                var myViewModel = new UploadViewModel()
                {
                    Providers = new SelectList(new List<SelectListItem>())
                };
                myViewModel.HidePeriod = !UploadDownloadConfiguration.EnablePeriod;
                myViewModel.HideVersion = !UploadDownloadConfiguration.EnableVersion;

                var myUser = RetrieveSession.ARUMUser;
                if (myUser == null)
                {
                    return RedirectToAction("Login", "Home");
                }

                if (UploadDownloadConfiguration.HideUploadForRole().Contains(myUser.Roles[0].RoleName))
                {
                    return RedirectToAction("Login", "Home");
                }

                myViewModel.HideUploadButton = UploadDownloadConfiguration.UploadReadOnlyForRole().Contains(myUser.Roles[0].RoleName);

                if (!IsEDSAvailable())
                {
                    TempData["Message"] = "Our system is not available at this time, please try again later.";
                    return View(myViewModel);
                }

                var appFilesTypes =
                    _arumFileProxy.GetApplicationFileTypesByApplicationID(myUser.UserID, "Upload/Index").FileTypes;
                if (appFilesTypes != null && appFilesTypes.Any())
                {
                    if (appFilesTypes.Length == 1)
                    {
                        if (appFilesTypes[0].ParentFileTypeID.HasValue)
                        {
                            myViewModel.FileTypeId = appFilesTypes[0].ParentFileTypeID.Value;

                        }
                        if (appFilesTypes[0].ChildFileTypeID.HasValue)
                        {
                            myViewModel.FileSubTypeId = appFilesTypes[0].ChildFileTypeID.Value;
                        }
                    }
                    myViewModel.FileTypes =
                        appFilesTypes.Where(n => n.IsExternal == true).Select(n => new SelectListItem()
                        {
                            Text = n.ParentFileTypeName,
                            Value = n.ParentFileTypeID.ToString(),
                        }).ToList();

                    myViewModel.FileTypes = myViewModel.FileTypes.GroupBy(x => x.Value).Select(y => y.First()).ToList();
                    //remove duplicates
                }
                else
                {
                    Message = "No file types setup for appliction.";
                }

                myViewModel.ApplicationPeriods = new List<SelectListItem>();

                if (UploadDownloadConfiguration.EnablePeriod)
                {
                    var appPeriods = _arumFileProxy.GetApplicationPeriodsByApplicationID(myUser.UserID, "Upload/Index").Periods;
                    if (appPeriods != null && appPeriods.Any())
                    {
                        myViewModel.ApplicationPeriods =
                            appPeriods.Where(n => n.IsExternal).Select(n => new SelectListItem()
                            {
                                Text = (!string.IsNullOrEmpty(n.DisplayName)) ? n.DisplayName : n.PeriodText,
                                Value = n.ApplicationPeriodID.ToString()
                            }).ToList();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(Message))
                        {
                            Message += "<br>";
                        }
                        Message += "No periods setup for appliction.";
                    }
                }

                if (!string.IsNullOrEmpty(Message))
                {
                    Message += "<br/>  Please contact the support help desk.";
                    TempData["Message"] = Message;
                }

                if (RetrieveSession.IsAdministrator || RetrieveSession.IsState)
                    myViewModel.Providers = List.SelectList.Providers.PopulateProviders_Administrator(myUser, "Upload");
                else
                    myViewModel.Providers = List.SelectList.Providers.PopulateProviders(myUser, "Upload");

                return View(myViewModel);

            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
            }
            return RedirectToAction("Login", "Home");
        }

        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("BulkProcessing:Upload")]
        public ActionResult BulkUpload()
        {
            try
            {
                var myUser = RetrieveSession.ARUMUser;

                var myViewModel = new BulkUploadViewModel
                {
                    HidePeriod = !UploadDownloadConfiguration.EnablePeriod,
                    HideVersion = !UploadDownloadConfiguration.EnableVersion,
                    HideUploadButton = UploadDownloadConfiguration.UploadReadOnlyForRole().Contains(myUser.Roles[0].RoleName)
                };
                
                if (!UploadDownloadConfiguration.EnableUpload)
                {
                    TempData["Message"] = "Upload is not enabled for this application.";
                    return View(myViewModel);
                }

                if (UploadDownloadConfiguration.HideUploadForRole().Contains(myUser.Roles[0].RoleName))
                {
                    TempData["Message"] = "Your role as '" + myUser.Roles[0].RoleName + "' is not allowed to upload.";
                    return View(myViewModel);
                }

                if (!IsEDSAvailable())
                {
                    TempData["Message"] = "Our system is not available at this time, please try again later.";
                    return View(myViewModel);
                }

                var appFilesTypes = _arumFileProxy
                    .GetApplicationFileTypesByApplicationID(myUser.UserID, "Upload/BulkUpload").FileTypes;

                var Message = string.Empty;
                if (appFilesTypes != null && appFilesTypes.Any())
                {
                    if (appFilesTypes.Length == 1)
                    {
                        if (appFilesTypes[0].ParentFileTypeID.HasValue)
                        {
                            myViewModel.FileTypeId = appFilesTypes[0].ParentFileTypeID.Value;

                        }
                        if (appFilesTypes[0].ChildFileTypeID.HasValue)
                        {
                            myViewModel.FileSubTypeId = appFilesTypes[0].ChildFileTypeID.Value;
                        }
                    }

                    myViewModel.FileTypes = ReturnBulkUploadFileParentNames(appFilesTypes);
                }
                else
                {
                    Message = "No file types setup for application.";
                }

                myViewModel.ApplicationPeriods = new List<SelectListItem>();

                if (!myViewModel.HidePeriod)
                {
                    var appPeriods = _arumFileProxy.GetApplicationPeriodsByApplicationID(myUser.UserID, "Upload/Index").Periods;
                    if (appPeriods != null && appPeriods.Any())
                    {
                        myViewModel.ApplicationPeriods =
                            appPeriods.Where(n => n.IsExternal).Select(n => new SelectListItem
                            {
                                Text = !string.IsNullOrEmpty(n.DisplayName) ? n.DisplayName : n.PeriodText,
                                Value = n.ApplicationPeriodID.ToString()
                            }).ToList();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(Message))
                        {
                            Message += "<br>";
                        }
                        Message += "No periods setup for application.";
                    }
                }

                if (!string.IsNullOrEmpty(Message))
                {
                    Message += "<br/>  Please contact the support help desk.";
                    TempData["Message"] = Message;
                }
                
                return View(myViewModel);

            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
            }
            return RedirectToAction("Login", "Home");
        }

        [HttpPost]
        public ActionResult SaveSynchronous(string UploadInfo, HttpPostedFileBase[] UploadDefault)
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="myViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Portal:Upload")]
        public ActionResult UploadFile(IEnumerable<HttpPostedFileBase> UploadDefault, string UploadDefault_data)
        {
            if (!UploadDownloadConfiguration.EnableUpload)
            {
                return Json(new { redirect = this.Url.AbsoluteAction("Login", "Home") });
            }
            try
            {
                Guid BatchID = Guid.NewGuid();

                #region Check Login User
                var MyUser = RetrieveSession.ARUMUser;
                if (MyUser == null)
                    return Json(new { redirect = this.Url.AbsoluteAction("Login", "Home") });
                if (MyUser.UserID < 1)
                    return Json(new { redirect = this.Url.AbsoluteAction("Login", "Home") });
                #endregion

                #region Provider
                IARUMUserProviders MyProviderUser = null;
                #endregion

                #region Check Parse and Verify Passed in Data
                string[] Parts;
                int ProviderId = 0;
                int FileTypeId = 0;
                int FileSubTypeId = 0;
                int PeriodId = 0;
                string ProviderName = string.Empty;
                int DeleteAfterDays = 0;

                try
                {
                    Parts = UploadDefault_data.Replace("\"", "").Split(';');
                    ProviderId = Convert.ToInt32(Parts[0]);
                    //This session variable is used for when they choose to go to the 
                    //download page the provider id is already selected.
                    Session["DownloadProviderID"] = ProviderId;
                    MyProviderUser = MyUser.Providers.Single(n => n.ProviderID == ProviderId);
                    FileTypeId = Convert.ToInt32(Parts[1]);
                    FileSubTypeId = Convert.ToInt32(Parts[2]);
                    PeriodId = Parts[3] == "" ? 0 : Convert.ToInt32(Parts[3]);
                    ProviderName = Parts[4];
                    DeleteAfterDays = UploadDownloadConfiguration.DeleteAfterDays;
                    if (Parts[5] != null)
                        if (Parts[5].IsInt())
                            DeleteAfterDays = Convert.ToInt32(Parts[5]);
                }
                catch (Exception e)
                {
                    InsertErrorEmail("There was an issue with the javascript.  Unable to parse UploadDefault_data: Value: " + UploadDefault_data + " Stack Trace: " + e.ToString(), "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue validating submitted information.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }

                if (ProviderId < 1)
                {
                    InsertErrorEmail("There was an issue with the javascript.  Unable to parse providerid: Value: " + UploadDefault_data, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving provider information.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                if (FileTypeId < 1)
                {
                    InsertErrorEmail("There was an issue with the javascript.  Unable to parse FileTypeId: Value: " + UploadDefault_data, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving file type information.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                if (FileSubTypeId < 1)
                {
                    InsertErrorEmail("There was an issue with the javascript.  Unable to parse FileSubTypeId: Value: " + UploadDefault_data, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving file sub type information.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                if (PeriodId < 0)
                {
                    InsertErrorEmail("There was an issue with the javascript.  Unable to parse PeriodId: Value: " + UploadDefault_data, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving period information.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                if (string.IsNullOrEmpty(ProviderName))
                {
                    InsertErrorEmail("There was an issue with the javascript.  Unable to parse ProviderName: Value: " + UploadDefault_data, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving provider name.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                if (DeleteAfterDays < 1)
                {
                    InsertErrorEmail("There was an issue with the javascript.  Unable to parse DeleteAfterDays: Value: " + UploadDefault_data, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving delete after days.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }

                if (!MyUser.Providers.Any(n => n.ProviderID == ProviderId))
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Technical Issue.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                #endregion

                #region Check Files
                if ((UploadDefault == null) || (!UploadDefault.Any()))
                {
                    InsertErrorEmail("There was an issue receiving files.  Received files collection is either null or empty.  UploadDefault_data: " + UploadDefault_data, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue receiving files.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                #endregion

                #region Retrieve File Type
                int ApplicationFileTypeId = 0;
                var SelectedFileType_ParentName = string.Empty;
                var SelectedFileType_ChildName = string.Empty;
                string ApplicationFileTypeExtensions = "";

                try
                {
                    var ApplicationFileType =
                        _arumFileProxy.GetApplicationFileTypesByApplicationID(MyUser.UserID, "Upload/UploadFile")
                            .FileTypes.Single(n => n.ParentFileTypeID == FileTypeId && n.ChildFileTypeID == FileSubTypeId);
                    if (ApplicationFileType != null)
                    {
                        ApplicationFileTypeId = ApplicationFileType.ApplicationFileTypeID;
                        SelectedFileType_ParentName = ApplicationFileType.ParentFileTypeName;
                        SelectedFileType_ChildName = ApplicationFileType.ChildFileTypeName;
                        ApplicationFileTypeExtensions = ApplicationFileType.AllowedFileExtensions?.ToLower();
                    }

                    if (ApplicationFileTypeId < 1)
                        throw new Exception("Issue retrieving application file type. Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + ".");
                }
                catch (Exception ex)
                {
                    InsertErrorEmail("There was an issue with retrieving ApplicationFileTypeID.  UploadDefault_data: Value: " + UploadDefault_data + " Stack Trace: " + ex.ToString(), "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving file type information.  Upload canceled.  Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                #endregion

                #region Retrieve Period Info
                var PeriodText = string.Empty;

                if (UploadDownloadConfiguration.EnablePeriod)
                {
                    try
                    {
                        var periodresult =
                            _arumFileProxy.GetApplicationPeriodsByApplicationID(MyUser.UserID, "Upload/UploadFile");

                        if (periodresult.Periods != null)
                        {
                            var period = periodresult.Periods.FirstOrDefault(c => c.ApplicationPeriodID == PeriodId);

                            if (period != null)
                            {
                                PeriodText = (!string.IsNullOrEmpty(period.DisplayName))
                                    ? period.DisplayName
                                    : period.PeriodText;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        InsertErrorEmail("There was an issue with retrieving GetApplicationPeriodsByApplicationID.  UploadDefault_data: Value: " + UploadDefault_data + " Stack Trace: " + ex.ToString(),
                            "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                        return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving period information.  Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                    }
                }
                #endregion

                #region Retrieve SourceType
                var SourceType = string.Empty;

                if (RetrieveSession.IsProvider || RetrieveSession.IsPreparerLimited || RetrieveSession.IsPreparerFull || RetrieveSession.IsFacility || RetrieveSession.IsManagementCompany)
                    SourceType = "PU";
                else if (RetrieveSession.IsState)
                    SourceType = "SU";
                else if (RetrieveSession.IsAdministrator)
                    SourceType = "MSU";
                else
                    SourceType = "PU";
                #endregion

                #region Retrieve User Info
                //Retrieve this before an upload is attempted.
                IARUMProviderUsers ProviderUsers = null;
                List<IARUM_User> ArumUsers = new List<IARUM_User>();

                try
                {
                    if (!RetrieveSession.IsExternal)
                    {
                        ProviderUsers = _arumUserProxy.GetProviderUsersByApplicationIDAndProviderID(ProviderId, "Upload");
                        foreach (var singleprovideruser in ProviderUsers.providerUsers)
                            ArumUsers.Add(_arumUserProxy.GetUserRolesByUserIDAndApplication(singleprovideruser.UserID, "UploadController/UploadFile", null, false));
                    }
                }
                catch (Exception ex)
                {
                    InsertErrorEmail("There was an issue with retrieving Provider Users or Roles for the User.  UploadDefault_data: Value: " + UploadDefault_data + " Stack Trace: " + ex.ToString(), "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving provider user information.  Upload canceled.  Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });

                }
                #endregion

                #region Upload Files
                List<KeyValuePair<Guid, string>> uploadedFiles = new List<KeyValuePair<Guid, string>>();
                List<string> filesNotUploaded = new List<string>();

                foreach (var file in UploadDefault.Where(m => m != null))
                {
                    //Upload one file at a time.  
                    //Take note of which ones are not successful.
                    try
                    {
                        //Verify file has valid extension for file type
                        var validFileExtensions = ApplicationFileTypeExtensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        var fileExtension = Path.GetExtension(file.FileName);
                        fileExtension = fileExtension?.Substring(1, fileExtension.Length - 1).ToLower();
                        bool goodExtension = false;
                        foreach (var validExtension in validFileExtensions)
                        {
                            if (!String.IsNullOrWhiteSpace(validExtension))
                            {
                                Regex extensionRegex = new Regex("^" + validExtension + "$");
                                if (extensionRegex.IsMatch(fileExtension))
                                    goodExtension = true;
                            }
                        }
                        if (!goodExtension)
                        {
                            filesNotUploaded.Add($"{file.FileName}");
                            continue;
                        }


                        var myUploadService = _arumFileProxy.UploadFileForProvider("Upload",
                            DeleteAfterDays, ApplicationFileTypeId, PeriodId, 0,
                            file.FileName, file.ContentLength, string.Empty,
                            "Upload/UploadFile", ProviderId, "Upload/UploadFile", SourceType, MyUser.UserID,
                            file.InputStream, false);

                        if (!myUploadService?.Result?.Successful ?? false)
                            filesNotUploaded.Add($"{file.FileName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last()}");
                        else
                            uploadedFiles.Add(new KeyValuePair<Guid, string>(myUploadService.File.FileID, file.FileName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last()));
                    }
                    catch
                    {
                        filesNotUploaded.Add($"{file.FileName}");
                    }
                }
                #endregion

                //At this point all files should be uploaded.
                //Send an email.  In the event that the email fails than send an Error Email.  

                if (uploadedFiles.Any())
                {
                    var emailResult = SendEmail(uploadedFiles, ProviderId, FileTypeId, SelectedFileType_ParentName, SelectedFileType_ChildName, PeriodText, ProviderName, MyUser, ProviderUsers, ArumUsers, BatchID);

                    if (!string.IsNullOrEmpty(emailResult))
                    {
                        InsertErrorEmail("There was an issue with sending an email.  UploadDefault_data: Value: " + UploadDefault_data + " SendEmail Result : " + emailResult, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                    }
                }

                if (filesNotUploaded.Any())
                {
                    var unsuccessful = CreateUL(filesNotUploaded, $"There were issues uploading the following files:");
                    InsertErrorEmail(unsuccessful, "Upload Controller:  Issue Uploading. Environment: " + PortalConfiguration.EnvironmentString + " ProviderID: " + ProviderId + " Provider Name: " + ProviderName);
                }

                return Json(new { SuccessfulUploads = uploadedFiles, UnsuccessfulUploads = filesNotUploaded, Message = "" });
            }
            catch (Exception ex)
            {
                InsertErrorEmail("Unknown Error.  UploadDefault_data: Value: " + UploadDefault_data + " Stack Trace: " + ex.ToString(), "Upload Controller:  Issue Uploading.  Environment: " + PortalConfiguration.EnvironmentString);
                return Json(new { redirect = this.Url.AbsoluteAction("Login", "Home") });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="UploadDefault"></param>
        /// <param name="FileTypeID"></param>
        /// <param name="FileSubTypeID"></param>
        /// <param name="DeleteAfterDays"></param>
        /// <param name="ApplicationPeriodID"></param>
        /// <param name="myViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ARUMFeatureActionFilterAttribute("BulkProcessing:Upload")]
        public ActionResult UploadBulkFile(IEnumerable<HttpPostedFileBase> UploadDefault, int FileTypeID,
            int FileSubTypeID, int? DeleteAfterDays, int? PeriodID)
        {
            try
            {
                #region Check Login User
                var MyUser = RetrieveSession.ARUMUser;
                var classification = RetrieveSession.Classification;
                if (MyUser == null || MyUser.UserID < 1)
                    return Json(new { redirect = this.Url.AbsoluteAction("Login", "Home") });
                #endregion
                
                DeleteAfterDays = DeleteAfterDays ?? UploadDownloadConfiguration.DeleteAfterDays;
                PeriodID = PeriodID ?? 0;

                #region Check Parse and Verify Passed in Data

                if (DeleteAfterDays < 1)
                {
                    InsertErrorEmail(
                        $"There was an issue with the javascript.  Unable to parse DeleteAfterDays: Value: '{DeleteAfterDays}'",
                        $"Upload Controller:  Issue With Bulk Upload. Environment: {PortalConfiguration.EnvironmentString}");
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving delete after days.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }

                if (UploadDefault == null || !UploadDefault.Any())
                {
                    InsertErrorEmail(
                        $"There was an issue receiving files.  Received files collection is either null or empty.",
                        $"Upload Controller:  Issue With Bulk Upload. Environment: {PortalConfiguration.EnvironmentString}");
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue receiving files.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                #endregion

                #region Retrieve File Type
                int ApplicationFileTypeId = 0;
                string ApplicationFileTypeExtensions = "";

                IFileType applicationFileType;
                try
                {
                    applicationFileType = _arumFileProxy.GetApplicationFileTypesByApplicationID(MyUser.UserID, "Upload/UploadFile")
                        .FileTypes.Single(n => n.ParentFileTypeID == FileTypeID && n.ChildFileTypeID == FileSubTypeID);

                    ApplicationFileTypeId = applicationFileType.ApplicationFileTypeID;
                    ApplicationFileTypeExtensions = applicationFileType.AllowedFileExtensions?.ToLower();

                    if (ApplicationFileTypeId < 1)
                        throw new Exception("Issue retrieving application file type. Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + ".");
                }
                catch (Exception ex)
                {
                    InsertErrorEmail(
                        $"There was an issue with retrieving ApplicationFileTypeID. FileTypeID: {FileTypeID} | FileSubTypeID: {FileSubTypeID} Stack Trace: {ex}",
                        $"Upload Controller:  Issue With Bulk Upload. Environment: {PortalConfiguration.EnvironmentString}");
                    return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving file type information.  Upload canceled.  Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                }
                #endregion

                #region Retrieve Period Info
                var PeriodText = string.Empty;

                if (UploadDownloadConfiguration.EnablePeriod)
                {
                    try
                    {
                        var periodresult =
                            _arumFileProxy.GetApplicationPeriodsByApplicationID(MyUser.UserID, "Upload/UploadFile");

                        if (periodresult.Periods != null)
                        {
                            // TODO: Check that this should be comparing between Application Period ID and Period ID. Seems weird to mention period ID at all.
                            var period = periodresult.Periods.FirstOrDefault(c => c.ApplicationPeriodID == PeriodID);

                            if (period != null)
                            {
                                PeriodText = (!string.IsNullOrEmpty(period.DisplayName))
                                    ? period.DisplayName
                                    : period.PeriodText;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        InsertErrorEmail(
                            $"There was an issue with retrieving GetApplicationPeriodsByApplicationID. PeriodID: {PeriodID} Stack Trace: {ex}",
                            $"Upload Controller:  Issue With Bulk Upload. Environment: {PortalConfiguration.EnvironmentString}");
                        return Json(new { SuccessfulUploads = new List<string>(), UnsuccessfulUploads = new List<string>(), Message = "Issue retrieving period information.  Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "." });
                    }
                }
                #endregion

                #region Retrieve SourceType
                var SourceType = string.Empty;

                if (RetrieveSession.IsProvider || RetrieveSession.IsPreparerLimited || RetrieveSession.IsPreparerFull || RetrieveSession.IsFacility || RetrieveSession.IsManagementCompany)
                    SourceType = "PU";
                else if (RetrieveSession.IsState)
                    SourceType = "SU";
                else if (RetrieveSession.IsAdministrator)
                    SourceType = "MSU";
                else
                    SourceType = "PU";
                #endregion

                #region Upload Files
                List<KeyValuePair<Guid, string>> uploadedFiles = new List<KeyValuePair<Guid, string>>();
                List<string> filesNotUploaded = new List<string>();

                foreach (var file in UploadDefault.Where(m => m != null))
                {
                    //Upload one file at a time.  
                    //Take note of which ones are not successful.
                    try
                    {
                        //Check if file is a zip
                        var extension = file.FileName.Substring((file.FileName.LastIndexOf('.') + 1));
                        if (extension.ToLower() != "zip")
                        {
                            filesNotUploaded.Add($"{file.FileName}");
                            continue;
                        }
                        //Check if files inside zip match file type extensions
                        byte[] fileByteArray;
                        using (var memoryStream = new MemoryStream())
                        {
                            file.InputStream.CopyTo(memoryStream);
                            fileByteArray = memoryStream.ToArray();
                        }

                        var validProviderIDs = MyUser.Providers.Select(p => p.ProviderID).ToArray();
                        var _applicationProxy = ARUMApplication.GetARUMApplicationProxy(classification);
                        var applicationProviders = _applicationProxy.GetAllActiveProvidersWithOptions(classification.ARUMToken, false, false, false, false, false, false, false, "OHFI UploadController:CheckFileNames")?.lstProviders;
                        bool allGoodFiles = true;

                        var zipArchive = new ZipArchive();

                        zipArchive.DefaultCompressionLevel = Syncfusion.Compression.CompressionLevel.Best;

                        var unzipStream = new MemoryStream();
                        unzipStream.Write(fileByteArray, 0, fileByteArray.Length);
                        unzipStream.Position = 0;
                        zipArchive.Open(unzipStream, false);
                        foreach (ZipArchiveItem childFile in zipArchive.Items)
                        {

                            var providerValidationResult = GetProviderIdFromFilename(applicationProviders, childFile.ItemName, applicationFileType.ParentFileTypeName, applicationFileType.ChildFileTypeName, out var providerId);
                            if (!providerValidationResult.Valid)
                            {
                                allGoodFiles = false;
                                break;
                            }
                            if (!validProviderIDs.Contains(providerId))
                            {
                                allGoodFiles = false;
                                break;
                            }

                            var validFileExtensions = ApplicationFileTypeExtensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            var fileExtension = Path.GetExtension(childFile.ItemName);
                            fileExtension = fileExtension?.Substring(1, fileExtension.Length - 1).ToLower();
                            bool goodExtension = false;
                            foreach (var validExtension in validFileExtensions)
                            {
                                if (!String.IsNullOrWhiteSpace(validExtension))
                                {
                                    Regex extensionRegex = new Regex(validExtension);
                                    if (extensionRegex.IsMatch(fileExtension))
                                        goodExtension = true;
                                }
                            }
                            if (!goodExtension)
                            {
                                allGoodFiles = false;
                                break;
                            }
                        }
                        if (!allGoodFiles)
                        {
                            filesNotUploaded.Add($"{file.FileName}");
                            continue;
                        }


                        var arumUploadStream = new MemoryStream();
                        arumUploadStream.Write(fileByteArray, 0, fileByteArray.Length);
                        arumUploadStream.Position = 0;
                        
                        var myUploadService = _arumFileProxy.UploadFile("Bulk File Upload",
                            ApplicationFileTypeId,
                            PeriodID ?? 0,
                            DeleteAfterDays ?? UploadDownloadConfiguration.DeleteAfterDays,
                            0,
                            file.FileName,
                            file.ContentLength,
                            string.Empty,
                            "Upload/UploadFile",
                            "Upload/UploadFile",
                            SourceType,
                            MyUser.UserID,
                            arumUploadStream,
                            false);

                        if (!(myUploadService?.Result?.Successful ?? false))
                            filesNotUploaded.Add($"{file.FileName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last()}");
                        else
                        {
                            string arumConnectionString = ARUMConfiguration.GetARUMConnectionString(classification);
                            JobBL.QueueJob(arumConnectionString, new CreateJobParameters
                            {
                                Name = $"{file.FileName}",
                                FileGuid = myUploadService.File.FileID,
                                JobName = "Bulk Upload",
                                JobType = "UNZIP",
                                
                                ItemType = "ZIP",
                                Status = "UPLOADED",
                                
                                ItemAttributes = new Dictionary<string, string>
                                {
                                    {"FileTypeID", ApplicationFileTypeId.ToString()},
                                    {"PeriodID", (PeriodID ?? 0).ToString()},
                                    {"PeriodI1D", (PeriodID ?? 0).ToString()},
                                },
                                Username = MyUser.Email,
                                Messages = new List<string> { "Initial Upload" }
                            }); 
                            uploadedFiles.Add(new KeyValuePair<Guid, string>(myUploadService.File.FileID, file.FileName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last()));
                        }
                    }
                    catch
                    {
                        filesNotUploaded.Add($"{file.FileName}");
                    }
                }
                #endregion

                //At this point all files should be uploaded.
                //Send an email.  In the event that the email fails than send an Error Email.  
                
                if (filesNotUploaded.Any())
                {
                    var unsuccessful = CreateUL(filesNotUploaded, $"Bulk Upload: There were issues uploading the following files:");
                    InsertErrorEmail(unsuccessful,
                        $"Upload Controller:  Issue Uploading. Environment: {PortalConfiguration.EnvironmentString}");
                }

                return Json(new { SuccessfulUploads = uploadedFiles, UnsuccessfulUploads = filesNotUploaded, Message = "" });
            }
            catch (Exception ex)
            {
                InsertErrorEmail("Unknown Error. Stack Trace: " + ex.ToString(),
                    "Upload Controller:  Issue With Bulk Upload.  Environment: " + PortalConfiguration.EnvironmentString);
                return Json(new { redirect = this.Url.AbsoluteAction("Login", "Home") });
            }
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("BulkProcessing:Upload")]
        public ActionResult CheckFileNames(string[] fileNames, int fileTypeID, int fileSubTypeID, int? periodID)
        {
            try
            {
                ARUMProxy_User myUser = RetrieveSession.ARUMUser;
                string[] validFileExtensions = {};
                IFileType applicationFileType;
                var classification = RetrieveSession.Classification;

                #region Retrieve File Type
                try
                {
                    IFileTypesResults applicationFileTypes = _arumFileProxy.GetApplicationFileTypesByApplicationID(myUser.UserID, "Upload/UploadFile");
                    applicationFileType = applicationFileTypes.FileTypes.FirstOrDefault(n => n.ParentFileTypeID == fileTypeID && n.ChildFileTypeID == fileSubTypeID);
                    if (applicationFileType == null || applicationFileType.ApplicationFileTypeID < 1)
                        return Json(new
                        {
                            Successful = true,
                            Message = "Invalid File Type selected."
                        });

                    validFileExtensions = applicationFileType.AllowedFileExtensions.ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                }
                catch (Exception ex)
                {
                    InsertErrorEmail($"There was an issue with retrieving ApplicationFileTypeID in CheckFileNames. ParentFileTypeID: {fileTypeID} | ChildFileTypeID: {fileSubTypeID} | Stack Trace: {ex}",
                        $"Upload Controller: CheckFileNames. Environment: {PortalConfiguration.EnvironmentString}. Classification: {RetrieveSession.Classification.DisplayName}");
                    
                    return Json(new
                    {
                        Successful = false,
                        Message = "Issue retrieving file type information.  Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "."
                    });
                }
                #endregion

                #region Retrieve Period Info
                if (UploadDownloadConfiguration.EnablePeriod)
                {
                    try
                    {
                        var periodresult =
                            _arumFileProxy.GetApplicationPeriodsByApplicationID(myUser.UserID, "Upload/UploadFile");

                        var period = periodresult.Periods?.FirstOrDefault(c => c.ApplicationPeriodID == periodID);

                        if (period == null)
                        {
                            return Json(new
                            {
                                Successful = true,
                                Message = "Invalid Period Selected."
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        InsertErrorEmail($"There was an issue with retrieving period information. | Period: {periodID} | Stack Trace: {ex}",
                            $"Upload Controller: CheckFileNames. Environment: {PortalConfiguration.EnvironmentString}. Classification: {RetrieveSession.Classification.DisplayName}");
                        
                        return Json(new
                        {
                            Successful = false,
                            Message = "Issue retrieving period information.   Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "."
                        });
                    }
                }
                #endregion
                
                #region File Name Validation

                var validProviderIDs = myUser.Providers.Select(p => p.ProviderID).ToArray();

                var _applicationProxy = ARUMApplication.GetARUMApplicationProxy(classification);
                var applicationProviders = _applicationProxy.GetAllActiveProvidersWithOptions(classification.ARUMToken, false, false, false, false, false, false, false, "OHFI UploadController:CheckFileNames")?.lstProviders;

                BulkUploadViewModel.FileNameValidation[] returnTable = fileNames.Select(file =>
                {
                    var providerValidationResult = GetProviderIdFromFilename(applicationProviders, file, applicationFileType.ParentFileTypeName, applicationFileType.ChildFileTypeName, out var providerId);
                    if (!providerValidationResult.Valid)
                        return providerValidationResult;
                    
                    if (!validProviderIDs.Contains(providerId))
                        return ValidationMessage(providerId, file, false, "This file couldn't be connected to a provider.");

                    var fileExtension = Path.GetExtension(file);
                    fileExtension = fileExtension?.Substring(1, fileExtension.Length - 1).ToLower();
                    bool goodExtension = false;
                    foreach (var validExtension in validFileExtensions)
                    {
                        if (!String.IsNullOrWhiteSpace(validExtension))
                        {
                            Regex extensionRegex = new Regex(validExtension);
                            if (extensionRegex.IsMatch(fileExtension))
                                goodExtension = true;
                        }
                    }
                    if (!goodExtension)
                    {
                        return ValidationMessage(providerId, file, false, "This isn't a valid file extension.");
                    }

                    return new BulkUploadViewModel.FileNameValidation
                    {
                        Order = providerId,
                        FileName = file,
                        Valid = true
                    };
                }).OrderBy(f => f.Order).ToArray();
                #endregion

                return Json(new
                {
                    Successful = true,
                    valid = returnTable.All(x => x.Valid),
                    returnTable
                });
            }
            catch (Exception ex)
            {
                InsertErrorEmail("Unknown Error in Upload - CheckFileNames. Stack Trace: " + ex.ToString(), "Upload Controller:  Issue Uploading.  Environment: " + PortalConfiguration.EnvironmentString);
                
                return Json(new
                {
                    Successful = false,
                    Message = "Unknown Error. Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + "."
                });
            }
        }

        /// <summary>
        /// Gets the ProviderID from the filename. The filename will be validated first.
        /// </summary>
        /// <param name="classification"></param>
        /// <param name="fileName">The filename being processed</param>
        /// <param name="parentName">The parent dropdown text</param>
        /// <param name="childName">The child dropdown text</param>
        /// <param name="providerId">an out variable which is the providerId for the file</param>
        /// <returns></returns>
        private static BulkUploadViewModel.FileNameValidation GetProviderIdFromFilename(List<Provider> applicationProviders, string fileName, string parentName, string childName, out int providerId)
        {
            providerId = 0;

            var result = ValidateFileNameValues(fileName, parentName, childName);
            if (result.Valid == false) return result;

            var mcaid = ExtractMCAIDFromFilename(fileName, parentName, childName);
            if (mcaid == string.Empty)
                return ValidationMessage(0, fileName, false, "The MCAID could not be determined for this file.");

            if (!applicationProviders.Any(p => p.MCAID?.Trim() == mcaid.Trim()))
                return ValidationMessage(0, fileName, false, "The Provider could not be found for this MCAID.");

            try
            {
                providerId = applicationProviders.Single(p => p.MCAID?.Trim() == mcaid?.Trim())?.ProviderID ?? 0;
            }
            catch
            {
                return ValidationMessage(0, fileName, false, $"No provider or more than one provider matches this MCAID: {mcaid}");
            }

            return ValidationMessage(providerId, fileName, true, string.Empty);
        }

        /// <summary>
        /// Returns a ValidationMessage object
        /// </summary>
        /// <param name="order">The providerId. This will be 0 if the validation fails</param>
        /// <param name="fileName">The filename being processed</param>
        /// <param name="valid"></param>
        /// <param name="message">The message to display to the user about why the file failed validation</param>
        /// <returns></returns>
        private static BulkUploadViewModel.FileNameValidation ValidationMessage(int order, string fileName, bool valid, string message)
        {
            return new BulkUploadViewModel.FileNameValidation
            {
                Order = order, // int.Parse(providerId),
                FileName = fileName,
                Valid = valid,
                ValidationMessage = message
            };
        }

        /// <summary>
        /// Extracts the MCAID number from a given filename using the parent and child dropdown values to get the file template.
        /// </summary>
        /// <param name="fileName">The filename to extract the data from</param>
        /// <param name="parentName">The parent dropdown value</param>
        /// <param name="childName">The child dropdown value</param>
        /// <returns>The MCAID number from the filename</returns>
        private static string ExtractMCAIDFromFilename(string fileName, string parentName, string childName)
        {
            var template = ARUMConfigurationOHFI.BulkProcesingFilenameTemplate(RetrieveSession.Classification, parentName, childName);
            var groups = GetGroupsFromTemplate(template);
            var mcaidGroup = groups.FirstOrDefault(i => i.Key.ToUpper().Contains("MCAID"));
            if (mcaidGroup.Key == null)
                return string.Empty;

            foreach (var group in groups)
            {
                if (group.Key == mcaidGroup.Key) continue;

                template = template.Replace($"{{{group.Key}:{group.Value}}}", new string('.', group.Value));
            }
            var mcaidStartIndex = template.IndexOf($"{{{mcaidGroup.Key}:", StringComparison.OrdinalIgnoreCase);
            return fileName.Substring(mcaidStartIndex, mcaidGroup.Value);
        }

        /// <summary>
        /// General Validation routine
        /// </summary>
        /// <param name="fileName">The filename being processed</param>
        /// <param name="parentName">The parent dropdown value</param>
        /// <param name="childName">The child dropdown value</param>
        /// <returns></returns>
        private static BulkUploadViewModel.FileNameValidation ValidateFileNameValues(string fileName, string parentName, string childName)
        {
            var template = ARUMConfigurationOHFI.BulkProcesingFilenameTemplate(RetrieveSession.Classification, parentName, childName); ;
            if (string.IsNullOrWhiteSpace(template))
                return ValidationMessage(0, fileName, false, "Unable to find template for the selected values.");

            var groups = GetGroupsFromTemplate(template);
            foreach (var group in groups)
            {
                template = template.Replace($"{{{group.Key}:{group.Value}}}", new string('.', group.Value));
            }
            
            // TODO: Deal with this nonsense. - Adam Holbert
            {
                // It's 1/20/2023, OHFI goes to production today. This is a stopgap measure so we don't have to change 60+ application settings to fit the correct functionality.
                // As of right now, templates look like:
                // RP_P{mcaid:7}_Eff_YYYYMMDD_Run_YYYYMMDD
                // when they should look like:
                // RP_P{mcaid:7}_Eff_{effectiveDate:8}_Run_{runDate:8}

                template = template.Replace("YYYYMMDD", "........");
                template = template.Replace("MMDDYYYY", "........");
                template = template.Replace("MMYYYY", "......");
                template = template.Replace("HHMMSS", "......");
                template = template.Replace("YYYY", "....");
            }

            return Regex.IsMatch(fileName, $"^{template}.*")
                ? ValidationMessage(0, fileName, true, string.Empty)
                : ValidationMessage(0, fileName, false, "Invalid Filename format for this upload type.");
        }
        
        /// <summary>
        /// Searches a given string for patterns in the format {key:length} and returns a dictionary containing the found key/length pairings.
        /// </summary>
        /// <param name="fileNameTemplate">The string to be searched.</param>
        /// <returns>A dictionary containing the found key/length pairings.</returns>
        private static Dictionary<string, int> GetGroupsFromTemplate(string fileNameTemplate)
        {
            var matches = Regex.Matches(fileNameTemplate, @"\{(?<key>\w+):(?<length>\d+)\}");

            var lengths = new Dictionary<string, int>();
            foreach (Match m in matches)
            {
                var key = m.Groups["key"].Value;
                var length = int.Parse(m.Groups["length"].Value);
                lengths[key] = length;
            }
            return lengths;
        }

        /// <summary>
        /// Returns the parent Dropdown names using the ApplicationSettings to get the valid values.
        /// </summary>
        /// <param name="appFileTypes">The list of file types for the application</param>
        /// <returns></returns>
        private List<SelectListItem> ReturnBulkUploadFileParentNames(IFileType[] appFileTypes)
        {
            var validParentFileTypes = ARUMConfigurationOHFI.BulkProcesingParentFileType(RetrieveSession.Classification)?.Split(',');
            if (validParentFileTypes == null || validParentFileTypes.Length == 0)
            {
                return new List<SelectListItem>();
            }

            var parentFileTypes = appFileTypes
                                    .Where(x => validParentFileTypes?.Any(ft => ft.Contains(x.ParentFileTypeName.Trim())) ?? false)
                                    .Select(li => new SelectListItem { Value = li.ParentFileTypeID.ToString(), Text = li.ParentFileTypeName.Trim() })
                                    .ToList();

            // Return the distinct values for parentFileTypes
            return parentFileTypes.GroupBy(x => x.Value)
                                        .Select(x => x.First())
                                        .ToList();
        }

        /// <summary>
        /// Returns file sub types.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        [HttpPost]
        [ARUMFeatureActionFilterAttribute("")]
        public JsonResult ReturnFileSubTypes(int parentId)
        {
            var fileSubTypes = new List<SelectListItem>();
            var fileTypes = _arumFileProxy.GetApplicationFileTypesByApplicationID(RetrieveSession.ARUMUser.UserID, "Upload / ReturnFileSubTypes");
            var parentFileTypes = fileTypes.FileTypes.Where(n => n.ParentFileTypeID == parentId && n.IsExternal).ToList();
            fileSubTypes = parentFileTypes.Select(n => new SelectListItem()
            {
                Text = (!n.ChildFileTypeID.HasValue) ? "N/A" : (string.IsNullOrWhiteSpace(n.DisplayName)) ? n.ChildFileTypeName : n.DisplayName,
                Value = (!n.ChildFileTypeID.HasValue) ? "0" : n.ChildFileTypeID.ToString(),
            }).ToList();
            return Json(fileSubTypes, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Returns UploadParametersModel.  Every parameter required to use the sync fusion control.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="parentId"></param>
        /// <param name="childId"></param>
        /// <returns></returns>
        [HttpPost]
        [ARUMFeatureActionFilterAttribute("")]
        public JsonResult ReturnExtensionAndFileSize(int parentId, int childId)
        {
            var FileTypes = _arumFileProxy.GetApplicationFileTypesByApplicationID(RetrieveSession.ARUMUser.UserID, "Upload / ReturnExtensionAndFileSize");
            var selectedFileType = FileTypes.FileTypes.Single(n => n.ParentFileTypeID == parentId && n.ChildFileTypeID == childId);
            var extensions = selectedFileType.AllowedFileExtensions;
            var extensionArray = extensions.ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var fileCodes = ARUMConfiguration.GetApplicationCodes(RetrieveSession.Classification).Where(c => c.Type == "AllowedFileExtensions" && c.IsActive && extensionArray.Contains(c.Value.ToLower()));
            string allowedExtensions = String.Join(", ", fileCodes.Select(c => c.Name));
            return Json(new UploadParametersModel()
            {
                DeleteAfterDays = selectedFileType.DeleteAfterDays,
                DisplayAllowedSize = FileHelper.GetBytesToString(selectedFileType.AllowedFileSize),
                AllowedSize = selectedFileType.AllowedFileSize,
                AllowedExtensions = allowedExtensions,
                ExtensionFormats = extensions
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("BulkProcessing:Upload")]
        public JsonResult SetProvider(int providerID)
        {
            Session["DownloadProviderID"] = providerID;
            return Json(new ReturnJson.Info(true, string.Empty), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Takes a string from the database and formats it to use with the sync fusion upload control.
        /// </summary>
        /// <param name="stringToFormat"></param>
        /// <returns></returns>
        private string FormatExtensions(string stringToFormat)
        {
            string result = string.Empty;
            foreach (var s in stringToFormat.Split(';'))
                if (!string.IsNullOrEmpty(s))
                    result += "." + s + ",";
            return result.Substring(0, result.Length - 1);
        }

        private EDSEmailParams SetupEmailParameters_FileNotification(string Subject, ICustomProvider Provider, string UserEmail, string Body, int Min2Wait)
        {
            var ProviderMCAID = Provider.MCAID;
            var ep = new EDSEmailParams();
            ep.emailParams = new EmailParams();
            if (Provider.myProviderIdentifiers != null &&
                Provider.myProviderIdentifiers.Any())
            {
                foreach (var singleProviderIdenrifiers in Provider.myProviderIdentifiers)
                {
                    if (string.IsNullOrEmpty(ProviderMCAID))
                    {
                        if (singleProviderIdenrifiers.Type == "LTCFACID")
                        {
                            ProviderMCAID = singleProviderIdenrifiers.Value;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(ProviderMCAID)) continue;
                    if (singleProviderIdenrifiers.Type == "SN")
                    {
                        ProviderMCAID = singleProviderIdenrifiers.Value;
                        break;
                    }
                }
            }

            ep.emailParams.emailParameters = new EmailParameters()
            {
                Type = "External",
                NotificationType = "FileNotification",
                Recipients = string.Format("{0};", UserEmail),
                Copy_Recipients = null,
                Blind_Copy_Recipients = null,
                Body = Body,
                From_Address = string.Format("{0}<{1}>", PortalConfiguration.EmailFromName, PortalConfiguration.EmailFrom),
                Importance = "High",
                Sensitivity = "Confidential",
                Subject = Subject,
                URL = URL.GetBaseURL(false),
                Minutes2Wait = Min2Wait,
                ProviderName = Provider.Name,
                ProviderMCAID = ProviderMCAID,
                Reply_To = null
            };
            return ep;
        }
        private EDSEmailParams SetupEmailParameters_Provider(string ProviderName, string MCAID, string Subject, string Body, int Min2Wait)
        {
	        var ep = new EDSEmailParams
	        {
		        emailParams = new EmailParams
		        {
			        emailParameters = new EmailParameters
			        {
				        Type = "Internal",
				        NotificationType = "FileNotification",
				        Recipients = UploadDownloadConfiguration.UploadNotificationEmail,
				        Copy_Recipients = null,
				        Blind_Copy_Recipients = null,
				        Body = Body,
				        From_Address = string.Format("{0}<{1}>", PortalConfiguration.EmailFromName, PortalConfiguration.EmailFrom),
				        Importance = "High",
				        Sensitivity = "Confidential",
				        Subject = Subject,
				        URL = URL.GetBaseURL(false),
				        Minutes2Wait = Min2Wait,
				        ProviderName = null,
				        ProviderMCAID = null,
				        Reply_To = null
			        }
		        }
	        };
	        return ep;
        }

        /// <summary>
        /// Sends an email with notification.
        /// </summary>
        /// <param name="fileNames"></param>
        /// <param name="ProviderID"></param>
        /// <param name="ParentFileTypeID"></param>
        /// <param name="ParentFileType"></param>
        /// <param name="SubFileType"></param>
        /// <param name="PeriodType"></param>
        /// <param name="providerName"></param>
        /// <returns>An error message.  Empty string is no error.</returns>
        private string SendEmail(List<KeyValuePair<Guid, string>> fileNames, int ProviderID, int ParentFileTypeID, string ParentFileType, string SubFileType, string PeriodType, string providerName, ARUMProxy_User User, IARUMProviderUsers ProviderUsers, List<IARUM_User> ARUMUsers, Guid BatchID)
        {
            var classificationFromSession = RetrieveSession.Classification;
            var Subject = "File Upload Notification: {0} - {1}";
            var Direction = "MSLC";
            var Url = "<b>URL:</b> <a href=\"{0}\">{0}</a>";
            var UserInfo_UserName = "<b>Uploaded By User:</b> {0}<br/>";
            var UserInfo_Email = "<b>Uploaded By Email:</b> {0}<br/>";
            var ProviderInfo = "<b>Provider:</b> {0}";
            var FileType = "<b>File Type:</b> {0}";
            var Period = "<b>Period:</b> {0}";
            string EmailMessage = string.Empty;
            List<string> EDSMessages = new List<string>();
            string ExceptionMessage = string.Empty;

            try
            {
                #region Initialization and Validation
                EmailMessage = GetEmailNotificationText();

                if (string.IsNullOrWhiteSpace(EmailMessage))
                    return "Email Notfication Text empty.  Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + ".";

                if ((fileNames == null) || (!fileNames.Any()))
                    return "No files detected.  Unable to send email.  Try again.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + ".";

                Subject = string.Format(Subject, providerName, ParentFileType);
                EmailMessage = EmailMessage.Replace("xx_URL_xx", string.Format(Url, URL.GetBaseURL(false)))
                                           .Replace("xx_ProviderInfo_xx", string.Format(ProviderInfo, providerName))
                                           .Replace("xx_FileType_xx", string.Format(FileType, ParentFileType))
                                           .Replace("xx_Period_xx", string.Format(Period, PeriodType))
                                           .Replace("xx_IMGLogo_xx", PortalConfiguration.ImageLogo);

                var fileList = new StringBuilder();
                foreach (var singleFileName in fileNames)
                {
                    fileList.Append(string.Format("<li>{0}</li>", singleFileName.Value));
                    fileList.Append("<br/>");
                }
                EmailMessage = EmailMessage.Replace("xx_Files_xx", fileList.ToString());
                #endregion

                #region  External User Sending Email to Internal MSLC <---
                if (RetrieveSession.IsExternal)
                {
                    Direction = "An external web portal user";
                    EmailMessage = EmailMessage.Replace("xx_Direction_xx", Direction);
                    if (User != null)
                    {
                        EmailMessage = EmailMessage.Replace("xx_UserInfoUserName_xx", string.Format(UserInfo_UserName, string.Format("{0} {1}", User.FirstName, User.LastName)));
                        EmailMessage = EmailMessage.Replace("xx_UserInfoEmail_xx", string.Format(UserInfo_Email, User.Email));
                    }
                    else
                    {
                        EmailMessage = EmailMessage.Replace("xx_UserInfoUserName_xx", string.Empty);
                        EmailMessage = EmailMessage.Replace("xx_UserInfoEmail_xx", string.Empty);
                    }

                    var EmailResults = EDSTools.InsertEmail(classificationFromSession, SetupEmailParameters_Provider(providerName, "", Subject, EmailMessage, EDSConfiguration.DefaultWaitToMinutesForInternal));
                    if (EmailResults.Result.Successful)
                    {
                        InsertFileEmailIntegration(fileNames, EmailResults.emailLog.ID, ProviderID, User.Email, BatchID);
                    }
                    else if (!string.IsNullOrEmpty(EmailResults.Result.Message))
                    {
                        EDSMessages.Add("Unable to send email through EDS. Message: " + EmailResults.Result.Message);
                    }
                    else
                    {
                        EDSMessages.Add("Unable to send email through EDS.");
                    }

                    return "";
                }
                #endregion

                #region Internal User Sending Email External <---

                if (!PortalConfiguration.EnableSendEmailToExternalUsers)
                    return string.Empty;  //safety check to stop emails going out to external users via webconfig.

                EmailMessage = EmailMessage.Replace("xx_Direction_xx", Direction)
                                            .Replace("xx_UserInfoUserName_xx", string.Empty)
                                            .Replace("xx_UserInfoEmail_xx", string.Empty);

                var provideruser = ProviderUsers;
                if ((provideruser == null) || !(provideruser.providerUsers.Any()))
                    return "Unable to send email.  Cannot retrieve provider user info.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + ".";

                int MinutesToWait = EDSTools.CalculateMinutesToWaitForExternalEmailCommunication();
                if (MinutesToWait < 0)
                    return ("Unable to calculate minutes to wait.  Unable to send email.  If problem persists contact support at " + PortalConfiguration.HelpDeskEmail + ".");

                foreach (var singleprovideruser in provideruser.providerUsers)
                {
                    //no email address on file.  Skip User.
                    if (string.IsNullOrEmpty(singleprovideruser.Email))
                        continue;

                    //if the user doesn't have access to this file type. Skip User.
                    if (!singleprovideruser.ParentsFilesTypes.Any(c => c.Key == ParentFileTypeID))
                        continue;

                    var checkRolePermissions = ARUMUsers.Single(n => n.UserID == singleprovideruser.UserID);

                    // get the provider roles for the given user on the current provider.
                    IARUMUserProviders provider = checkRolePermissions.Providers
                            .Where(m => m.ProviderID == singleprovideruser.provider.ProviderID)
                            .FirstOrDefault();

                    //if it's null or contains limited go on to the next user.
                    //If the provider role is null in the database it will populate the provider user role with the 
                    //least level permission that the user might have.
                    if (provider == null || provider.ProviderUserRole.ToUpper().Contains("LIMITED"))
                        continue;
                    try
                    {
                        var EmailResults = EDSTools.InsertEmail(classificationFromSession, SetupEmailParameters_FileNotification(Subject,
                            singleprovideruser.provider, singleprovideruser.Email, EmailMessage, MinutesToWait));

                        if (EmailResults.Result.Successful)
                        {
                            EDSMessages.Add(EmailResults.Result.Message);
                            InsertFileEmailIntegration(fileNames, EmailResults.emailLog.ID, ProviderID, User.Email, BatchID);
                        }
                        else if (!string.IsNullOrEmpty(EmailResults.Result.Message))
                        {
                            EDSMessages.Add("Unable to send email through EDS. Message: " + EmailResults.Result.Message);
                        }
                        else
                        {
                            EDSMessages.Add("Unable to send email through EDS.");
                        }
                    }
                    catch (Exception Exc)
                    {
                        EDSMessages.Add($"Issue sending email through EDS.  To: {singleprovideruser.Email}");
                        ExceptionMessage = Exc.ToString();
                    }
                }
                //To summarize what is happening here.  
                //An empty returned string is success.  
                //A returned string is unsuccessful.
                string ReturnMessage = string.Join(", ", EDSMessages.Where(n => !string.IsNullOrEmpty(n))).Trim();

                if (!string.IsNullOrEmpty(ReturnMessage))
                    ReturnMessage = EmailMessage + " " + ReturnMessage;

                if (!string.IsNullOrEmpty(ExceptionMessage))
                    ReturnMessage = ReturnMessage + " Exception: " + ExceptionMessage;

                return ReturnMessage;

                #endregion
            }
            catch (Exception ex)
            {
                InsertErrorEmail(EmailMessage + " Stack Trace: " + ex.ToString(), "There was an issue sending an email in " + PortalConfiguration.EnvironmentString + ".  " + Subject);
                return ("");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetEmailNotificationText()
        {
            string ReturnValue = string.Empty;
            string FilePath = HttpContext.Server.MapPath("~") + @"Documents";
            string FullPath = string.Format(@"{0}\UploadEmailNotification.txt", FilePath);
            if (System.IO.File.Exists(FullPath))
            {
                ReturnValue = System.IO.File.ReadAllText(FullPath).ToString();
                if (ReturnValue.Contains("xx_URL_xx"))
                {
                    ReturnValue = ReturnValue.Replace("xx_URL_xx", URL.GetBaseURL(false));
                }
                if (ReturnValue.Contains("xx_PortalURL_xx"))
                {
                    var urlHelper = new UrlHelper(((MvcHandler)HttpContext.Handler).RequestContext);
                    string fullUrl = urlHelper.AbsoluteAction("Login", "Home");
                    ReturnValue = ReturnValue.Replace("xx_PortalURL_xx", fullUrl);
                }
                if (ReturnValue.Contains("xx_State_xx"))
                {
                    ReturnValue = ReturnValue.Replace("xx_State_xx", PortalConfiguration.Application);
                }
            }
            return ReturnValue;
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("")]
        public JsonResult FormatBytes(string Bytes)
        {
            try
            {
                return Json(FileHelper.GetBytesToString(Convert.ToInt64(Bytes)));
            }
            catch
            {
                return Json("Error Formatting Bytes.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetErrorNotificationText(string Body)
        {
            string ReturnValue = string.Empty;
            string FilePath = HttpContext.Server.MapPath("~") + @"Documents";
            string FullPath = string.Format(@"{0}\ExceptionNotification.txt", FilePath);
            if (System.IO.File.Exists(FullPath))
            {
                ReturnValue = System.IO.File.ReadAllText(FullPath).ToString();
                if (ReturnValue.Contains("xx_URL_xx"))
                {
                    ReturnValue = ReturnValue.Replace("xx_URL_xx", URL.GetBaseURL(false));
                }
                ReturnValue = ReturnValue.Replace("xx_Text_xx", Body);
                ReturnValue = ReturnValue.Replace("xx_IMGLogo_xx", PortalConfiguration.ImageLogo);
                if (ReturnValue.Contains("xx_State_xx"))
                    ReturnValue = ReturnValue.Replace("xx_State_xx", PortalConfiguration.Application);
            }
            return ReturnValue;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ErrorMessage"></param>
        /// <returns></returns>
        [HttpPost]
        [ARUMFeatureActionFilterAttribute("")]
        public JsonResult FormatBytesFromError(string ErrorMessage)
        {
            var bytes = Convert.ToInt64(ErrorMessage.Split(' ')[ErrorMessage.Split(' ').Length - 1]);
            return Json("The selected file size is too large.  Please select a file within " + FileHelper.GetBytesToString(bytes));
        }

        private void InsertErrorEmail(string MyBody, string TheSubject)
        {
            string FormattedBody = MyBody;
            var classificationFromSession = RetrieveSession.Classification;
            try
            {
                if (!FormattedBody.Contains("<html>"))
                    FormattedBody = GetErrorNotificationText(MyBody);

                EDSTools.InsertEmail(classificationFromSession, new EDSEmailParams()
                {
                    emailParams = new EmailParams()
                    {
                        emailParameters = new EmailParameters()
                        {
                            Type = "Internal",
                            NotificationType = "Error Notification",
                            Recipients = PortalConfiguration.ExceptionTo,
                            Body = FormattedBody,
                            From_Address = string.Format("{0}<{1}>", PortalConfiguration.EmailFromName, PortalConfiguration.EmailFrom),
                            Importance = "High",
                            Subject = TheSubject,
                            URL = URL.GetBaseURL(false),
                            Minutes2Wait = 1,
                            Reply_To = null,
                            Sensitivity = "Confidential"
                        }
                    }
                });
            }
            catch
            {
                try
                {
                    EmailSender.SetupMailMessage(PortalConfiguration.ExceptionTo, TheSubject, FormattedBody);
                }
                catch { } //nothing else that can be done.
            }
        }

        /// <summary>
        /// Creates an unordered list from a list of items, suffix, and prefix
        /// </summary>
        /// <param name="items"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        private string CreateUL(List<string> items, string prefix = null, string suffix = null)
        {
            string fullString = "";

            if (!string.IsNullOrWhiteSpace(prefix))
                fullString = prefix;

            fullString += "<ul>";
            foreach (var item in items)
                fullString += $"<li>{item}</li>";
            fullString += "</ul>";

            if (!string.IsNullOrWhiteSpace(suffix))
                fullString += suffix;

            return fullString;
        }

        private void InsertFileEmailIntegration(List<KeyValuePair<Guid, string>> fileNames, int EDSEmailID, int ProviderID, string CreatedBy, Guid BatchID)
        {
            try
            {
                List<FileEmailIntegrationDTO> listFileEmails = new List<FileEmailIntegrationDTO>();
                foreach (var v in fileNames)
                {
                    var fei = new FileEmailIntegrationDTO
                    {
                        BatchID = BatchID,
                        EDSEmailID = EDSEmailID,
                        FileID = v.Key,
                        IsActive = true,
                        ProviderID = ProviderID,
                        CreatedBy = CreatedBy,
                        CreateDate = DateTime.Now
                    };
                    listFileEmails.Add(fei);
                }

                _arumFileProxy.InsertFileEmailIntegrationList(CreatedBy, listFileEmails.ToArray());
            }
            catch (Exception ex)
            {
                InsertErrorEmail(ex.ToString(), "File Email Integration Table Error.");
                //File and email will be sent.  Just the link won't be made on the report.
            }
        }
    }
}