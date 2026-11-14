// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using MSLC.ArumProxy;
using MSLC.ArumProxy.Interfaces;
using MSLC.ArumProxy.Models;
using MSLC.FileProxy;
using MSLC.FileProxy.Interfaces;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.IP;
using MSLC.Portal.MVC.URL;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.File;
using MSLC.Portal.Utility.Proxies;
using MSLC.ProviderProxy;
using MSLC.StatLog.BL.AuditType;
using MSLC.StatLog.BL.CostReportHistory;
using MSLC.StatLog.BL.Email;
using MSLC.StatLog.BL.EventLifeCycle;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.BL.Role;
using MSLC.StatLog.BL.StatLogBL;
using MSLC.StatLog.BL.Validation;
using MSLC.StatLog.Configuration;
using MSLC.StatLog.DAL.ProjectSpecific;
using MSLC.StatLog.Domain;
using MSLC.StatLog.DTO;
using MSLC.StatLog.Enumerations;
using MSLC.StatLog.Models;
using MSLC.StatLog.MVC.MVC;
using MSLC.StatLog.MVC.ViewModels;
using MSLC.StatLog.StatlogUtility.Dependency;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Ganss.Xss;
using MSLC.StatLog.DAL.Migrations;
using MSLC.StatLog.Helpers;
using static MSLC.StatLog.BL.MappedObjectsNamespace.MappedObjects;
using static MSLC.StatLog.UI.Models.ClientMappingStatLog;
using static MSLC.StatLog.UI.Models.StatLogMapping;
using static MSLC.StatLog.UI.Models.StatLogScopes;
using static System.Int32;
using static System.String;
using Serilog;
using System.Diagnostics;
using MSLC.ApplicationProxy.Interfaces.Models;
using OpenXmlPowerTools;

// ReSharper disable once CheckNamespace
namespace MSLC.StatLog.Controllers
{
    /// <summary>
    /// Controls all Actions for StatLog Pages.
    /// </summary>
    public class StatLogController : LoggedInBaseController
    {
        #region Controller Initialization
        // ReSharper disable once InconsistentNaming
        private const string FILE_DOWNLOAD_COOKIE_NAME = "fileDownload";
        private readonly ArumServiceProxy _arumUserProxy;
        private readonly FileServiceProxy _fileServiceProxy;
        private readonly ARUMProviderProxy _providerProxy;
        private readonly ARUMProxy_User _user;
        private readonly ClassificationDTO _classification;
        private readonly HtmlSanitizer _sanitizer;

        public StatLogController()
        {
            _user = RetrieveSession.ARUMUser;
            _classification = RetrieveSession.Classification;
            _arumUserProxy = ARUMUser.GetArumServiceProxy(_classification);
            _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(_classification);
            _providerProxy = ARUMProvider.GetARUMProviderServiceProxy(_classification);
            _sanitizer = new HtmlSanitizer();
        }

        protected override void OnResultExecuting(ResultExecutingContext context)
        {
            CheckAndHandleFileResult(context);

            base.OnResultExecuting(context);
        }

        //<summary>
        //If the current response is a FileResult (an MVC base class for files) then write a
        //cookie to inform jquery.fileDownload that a successful file download has occurred
        //</summary>
        //<param name="context"></param>
        private void CheckAndHandleFileResult(ResultExecutingContext context)
        {
            //else if(context.Result is RedirectToRouteResult)
            //    Response.SetCookie(new HttpCookie(FILE_DOWNLOAD_COOKIE_NAME, "-1") { Path = "/" });
            if (context.Result is FileResult)
            {
                //jquery.fileDownload uses this cookie to determine that a file download has completed successfully
                Response.SetCookie(new HttpCookie(FILE_DOWNLOAD_COOKIE_NAME, "true") { Path = "/" });

            }
            //ensure that the cookie is removed in case someone did a file download without using jquery.fileDownload
            else if (Request.Cookies[FILE_DOWNLOAD_COOKIE_NAME] != null && Response.Cookies != null)
            {
                HttpCookie fileDownloadCookie = Response.Cookies[FILE_DOWNLOAD_COOKIE_NAME];
                if (fileDownloadCookie != null)
                {
                    fileDownloadCookie.Expires = DateTime.Now.AddYears(-1);
                }
            }
        }
        #endregion

        #region Main StatLog Page
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Sets up the basic View Model.</returns>
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:View")]
        public ActionResult Index(string sProviderID)
        {
            var enableFullLogging = StatlogARUMConfiguration.EnableFullStatLogControllerLogging(_classification);
            if (enableFullLogging)
                Log.Information("StatLog/{Action} for {Classification} called by user {UserEmail}", "Index", _classification.DisplayName, _user.Email);

            Stopwatch statLogTime = Stopwatch.StartNew();
            if (_user.Providers == null || !_user.Providers.Any())
            {
                TempData["Message"] = "No Providers Assigned to Current User.  Contact Support.  Click the icon below.";
                return RedirectToAction("Login", "Home");
            }

            Stopwatch identifierTime = Stopwatch.StartNew();
            string primaryIdentifier = ProviderBL.GetPrimaryIdentifierDisplayName(_classification);
            identifierTime.Stop();
            if (enableFullLogging)
                Log.Information("StatLog/{Action} primary identifier load for {Classification} took {Elapsed} s to run", "Index", _classification.DisplayName, identifierTime.Elapsed.TotalSeconds);

            TryParse(TempData["ARUMProviderID"]?.ToString(), out int arumProviderID);
            TryParse(TempData["CostReportID"]?.ToString(), out int costReportID);

            Stopwatch providerTime = Stopwatch.StartNew();
            var providerList1 = ProviderBL.GetAllARUMProviderInfoByARUMUserID(_classification, _user, "Statlog:View");
            var providerList = providerList1.Select(p => new StatLogViewModel.ProviderDropDown
                {
                    Name = p.Name,
                    Number = p.Number,
                    EffectiveDate = p.EffectiveDate,
                    EffectiveDateString = p.EffectiveDate != null ? p.EffectiveDate.Value.ToString("MM/dd/yyyy") : string.Empty,
                    ARUMProviderID = ToClientID(p.ARUMProviderID, PROVIDER_ID_STATLOG)
                }).OrderBy(p => p.Name).ThenBy(p => p.Number).ThenBy(p => p.EffectiveDate).ToList();
            providerTime.Stop();
            if (enableFullLogging)
                Log.Information("StatLog/{Action} provider load for {Classification} took {Elapsed} s to run", "Index", _classification.DisplayName, providerTime.Elapsed.TotalSeconds);

            int providerID = ToInternalID(sProviderID, PROVIDER_ID_STATLOG);
            var myViewModel = new StatLogViewModel.View
            {
                PageTitle = "Stat Log",
                myAutoProviderInformation = providerList,
                StatLogTable = new List<StatLogViewModel.StatLogRow>(),
                BeginningOfPeriod = null,
                EndOfPeriod = null,
                ContactID = 0, //Might store ARUM User ID here shortly.
                IsPHISafe = StatlogSession.IsPHISafe(providerID, RetrieveSession.ARUMUser),
                PrimaryIdentifier = !IsNullOrEmpty(primaryIdentifier) ? primaryIdentifier : "MCAID",
                readOnly = true
            };

            #region Provider Information
            int newProviderID = ToInternalID(sProviderID, PROVIDER_ID_STATLOG);
            if (newProviderID != -1)
                arumProviderID = newProviderID;

            if (arumProviderID <= 0)
            {
                statLogTime.Stop();
                if (enableFullLogging)
                    Log.Information("StatLog/{Action} loading for {Classification} took {Elapsed} s to run", "Index", _classification.DisplayName, statLogTime.Elapsed.TotalSeconds);
                return View(myViewModel);
            }

            Mapping map = MapByProviderID(arumProviderID, "Statlog:View");
            if (map.Error)
            {
                TempData["Message"] = map.ErrorMessage;
                return View(myViewModel);
            }

            myViewModel.sProviderID = ToClientID(map.Provider.ARUMProviderID, PROVIDER_ID_STATLOG);
            myViewModel.PageTitle = map.Provider.Name;
            myViewModel.SelectedProviderName = map.Provider.Name;
            myViewModel.SelectedProviderIdentifier = map.Provider.Number;
            RetrieveSession.SelectedProviderID = map.Provider.ARUMProviderID;
            myViewModel.readOnly = !ARUMConfiguration.FeatureMatchesRole(_classification, "StatLog:Interact", RetrieveSession.CurrentRoleName);
			#endregion

			#region StatLog Dropdown Information
			bool canHide = StatLogConfiguration.CanHide(new[] { RetrieveSession.CurrentRole });

            Stopwatch statlogDBTime = Stopwatch.StartNew();
            var costReports = ProviderBL.GetStatLogs(_classification, arumProviderID)
                .Where(c => c.IsActive != false || canHide)
                .OrderByDescending(c => c.EndDate)
                .ToList();
            statlogDBTime.Stop();
            if (enableFullLogging)
                Log.Information("StatLog/{Action} retrieving statlogs for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", _classification.DisplayName, map.StatLog?.ARUMProviderID, statlogDBTime.Elapsed.TotalSeconds);

            myViewModel.myCostReportsEndDateConcatAuditTypeDesc = new SelectList(
                costReports.Select(m => new SelectListItem
                {
                    Text = m.EndDate?.ToShortDateString() + " - " + m.AuditTypeDesc.ToString(),
                    Value = ToClientID(m.CostReportID, COST_REPORT_ID_STATLOG) + "|" + m.BeginDate?.ToShortDateString()
                }), "Value", "Text");
            #endregion

            #region Selected StatLog Information
            if (costReportID <= 0)
            {
                statLogTime.Stop();
                if (enableFullLogging)
                    Log.Information("StatLog/{Action} loading for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", _classification.DisplayName, map.StatLog?.ARUMProviderID, statLogTime.Elapsed.TotalSeconds);
                return View(myViewModel);
            }

            map = MapByCostReportID(costReportID, "Statlog:View");
            if (map.Error)
            {
                TempData["Message"] = map.ErrorMessage;
                return View(myViewModel);
            }

            if (map.StatLog.IsActive == false && !StatLogConfiguration.CanHide(RetrieveSession.UserRoles))
                return View(myViewModel);

			// If selected StatLog doesn't match selected provider, deselect that StatLog.
			if (map.Provider.ARUMProviderID != arumProviderID)
                return View(myViewModel);

            // Page Title = ProviderName - EndDate
            myViewModel.sCostReportID = ToClientID(map.StatLog.CRID, COST_REPORT_ID_STATLOG);
            myViewModel.PageTitle = $"{map.Provider.Name} - {map.StatLog.EndDate.ToShortDateString()}";
            myViewModel.IsActive = map.StatLog.IsActive;
            myViewModel.BeginningOfPeriod = map.StatLog.BeginDate;
            myViewModel.EndOfPeriod = map.StatLog.EndDate;

            Stopwatch historyTime = Stopwatch.StartNew();
            myViewModel.myCostReportHistory = CostReportHistoryBL.GetHistoryByCostReportID(_classification, map.StatLog.CRID, _user.UserID, "StatLog", "Index", map.Provider.ARUMProviderID);
            historyTime.Stop();
            if (enableFullLogging)
                Log.Information("StatLog/{Action} history retrieval for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", _classification.DisplayName, map.StatLog?.ARUMProviderID, historyTime.Elapsed.TotalSeconds);

            // Change all history usernames that contain '@' to 'SYSTEM'
            myViewModel.myCostReportHistory?.Where(cr => !cr.UserName?.Contains("@") ?? true).ToList().ForEach(cr => cr.UserName = "SYSTEM");

			// Select StatLog in drop down
			myViewModel.CostReportIDConcatBeginDate = $"{ToClientID(map.StatLog.CRID, COST_REPORT_ID_STATLOG)}|{map.StatLog.BeginDate.ToShortDateString()}";

            // Load interactive events into the Add Event Dropdown
            Stopwatch interactiveTime = Stopwatch.StartNew();
            var interactiveEvents = CostReportHistoryBL
                .GetInteractiveEvents(_classification, new[] { RetrieveSession.CurrentRole }, map.StatLog.CRID, true)
                .OrderBy(e => e.Name)
                .Select(e => new SelectListItem
                {
                    Text = $"{e.Name}{(IsNullOrWhiteSpace(e.Descript) ? Empty : " - " + e.Descript)}",
                    Value = ToClientID(e.ID, EVENT_ID_STATLOG),
                    Selected = false,
                    Disabled = false,
                })
                .ToList();
            interactiveTime.Stop();
            if (enableFullLogging)
                Log.Information("StatLog/{Action} interactive event retrieval for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", _classification.DisplayName, map.StatLog?.ARUMProviderID, interactiveTime.Elapsed.TotalSeconds);

            myViewModel.myInteractiveEvents = new SelectList(interactiveEvents, "Value", "Text");
            #endregion

            Stopwatch actionTime = Stopwatch.StartNew();
            GenerateActionItems.generateActionItems(myViewModel);
            actionTime.Stop();
            if (enableFullLogging)
                Log.Information("StatLog/{Action} generate action items for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", _classification.DisplayName, map.StatLog?.ARUMProviderID, actionTime.Elapsed.TotalSeconds);

            myViewModel.ContainsNAEvents = myViewModel.myCostReportHistory
                .Any(h => h.Event.ID == StatLogConfiguration.EventIDForNA || h.Event.CanUseNA);

            statLogTime.Stop();
            if (enableFullLogging)
                Log.Information("StatLog/{Action} loading for {Classification} provider {ARUMProviderID}, cost report {CostReportID} took {Elapsed} s to run", "Index", _classification.DisplayName, map.StatLog?.ARUMProviderID, map.StatLog.CRID, statLogTime.Elapsed.TotalSeconds);

            return View(myViewModel);
        }

        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:View")]
        public ActionResult SelectProvider(string sProviderID)
        {
            int providerID = ToInternalID(sProviderID, PROVIDER_ID_STATLOG);
            Mapping map = MapByProviderID(providerID, "Statlog:View");

            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            RetrieveSession.SelectedProviderID = map.Provider.ARUMProviderID;
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = new { success = true, role = RetrieveSession.CurrentRoleName }
            };
        }

		/// <summary>
		/// Fills the StatLog end dates.
		/// </summary>
		/// <param name="sProviderID">The ARUMProviderID identifier.</param>
		/// <returns>JSON data for EndDates to put into drop down box.</returns>
		[HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:View")]
        public JsonResult FillCostReportEndDates(string sProviderID)
        {
            int providerID = ToInternalID(sProviderID, PROVIDER_ID_STATLOG);
            Mapping map = MapByProviderID(providerID, "Statlog:View");
            if (map.Error)
                throw new Exception(map.ErrorMessage);
            RetrieveSession.SelectedProviderID = map.Provider.ARUMProviderID;

            var providerCostReportInformation = ProviderBL.GetStatLogs(_classification, map.Provider.ARUMProviderID).OrderByDescending(c => c.EndDate).ToList();
            if (!StatLogConfiguration.CanHide(new[] { RetrieveSession.CurrentRole }))
                providerCostReportInformation.RemoveAll(pci => pci.IsActive == false);

            var phyData = providerCostReportInformation.Select(m => new SelectListItem
            {
                Text = m.EndDate?.ToShortDateString(),
                Value = ToClientID(m.CostReportID, COST_REPORT_ID_STATLOG) + "|" + m.BeginDate?.ToShortDateString(),
            });

            TempData["ARUMProviderID"] = map.Provider.ARUMProviderID;
            return Json(phyData, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Fills the history.
        /// </summary>
        /// <param name="sCostReportID">The cost report identifier.</param>
        /// <returns>Fills the History Grid and then returns success as true to calling routine/javascript request.</returns>
        [HttpPost]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:View")]
        public JsonResult FillHistory(string sCostReportID)
        {
            int costReportID = ToInternalID(sCostReportID, COST_REPORT_ID_STATLOG);
            Mapping map = MapByCostReportID(costReportID, "Statlog:View");
            if (map.Error)
                throw new Exception(map.ErrorMessage);
            RetrieveSession.SelectedProviderID = map.Provider.ARUMProviderID;

            TempData["ARUMProviderID"] = map.Provider.ARUMProviderID;
            TempData["CostReportID"] = map.StatLog.CRID;

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:View")]
        public ActionResult Refresh(string sCostReportID)
        {
            int costReportID = ToInternalID(sCostReportID, COST_REPORT_ID_STATLOG);
            Mapping map = MapByCostReportID(costReportID, "Statlog:View");

            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);
            RetrieveSession.SelectedProviderID = map.Provider.ARUMProviderID;

            IARUMUserProviders[] providerArray;
            providerArray = RetrieveSession.ARUMUser.Providers;
            foreach (var provider in providerArray)
            {
                if (provider.ProviderID == RetrieveSession.SelectedProviderID)
                    return StatlogMessageRedirect(map, "");
            }
            return StatlogMessageJson("Error: Unable to retrieve statlog.");
        }
        #endregion

        #region Search Provider
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:View")]
        public ActionResult SearchProvider()
        {
            var myViewModel = new SearchProviderViewModel.View
            {
                ProviderNames = ProviderBL.GetAllARUMProviderInfoByARUMUserID(_classification, _user, "Statlog:View")
	                .OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToList(),
                ProviderPrimaryIdentifierType = ProviderBL.GetPrimaryIdentifierDisplayName(_classification),
                mySearchResults = null
            };

            return View(myViewModel);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        [ARUMFeatureActionFilterAttribute("Statlog:View")]
        public JsonResult SearchProviderName(string query)
        {
            var providerNames = ProviderBL.GetAllARUMProviderInfoByARUMUserID(_classification, _user, "Statlog:View")
                .Where(p => p.Name.StartsWith(query))
                .Select(p => p.Name)
                .ToList();

            return Json(providerNames, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:View")]
        [ValidateAntiForgeryToken]
        public ActionResult SearchProvider(SearchProviderViewModel.Search search)
        {
            string primaryIdentifierDisplayName = ProviderBL.GetPrimaryIdentifierDisplayName(_classification);
            var viewModel = new SearchProviderViewModel.View
            {
                ProviderNames = ProviderBL.GetAllARUMProviderInfoByARUMUserID(_classification, _user, "Statlog:View")
	                .OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToList(),
                ProviderPrimaryIdentifierType = primaryIdentifierDisplayName,
                mySearchResults = null,
                MedicaidNumber = search.MedicaidNumber,
                ProviderPrimaryIdentifier = search.ProviderPrimaryIdentifier,
                QueryProviderName = search.QueryProviderName
            };

            var providers = ProviderBL.GetAllARUMProviderInfoByARUMUserID(_classification, _user, "Statlog:View")
                .OrderBy(p => p.Name).ToList();

            var queryString = new List<string>();
            var possibleProviders = providers.ToList();

            string medicaidNumber = search.MedicaidNumber?.Trim();
            if (!IsNullOrEmpty(medicaidNumber))
            {
                queryString.Add($"Medicaid ID: {medicaidNumber}");
                possibleProviders.RemoveAll(p => p.MCAID.Trim() != medicaidNumber);
            }

            string piNumber = search.ProviderPrimaryIdentifier?.Trim();
            if (!IsNullOrEmpty(piNumber))
            {
                queryString.Add($"{primaryIdentifierDisplayName}: {medicaidNumber}");
                possibleProviders.RemoveAll(p => p.Number.Trim() != piNumber);
            }

            string providerName = search.QueryProviderName?.Trim();
            if (!IsNullOrEmpty(providerName))
            {
                queryString.Add($"Name: {providerName}");
                possibleProviders.RemoveAll(p => p.Name.Trim() != providerName);
            }

            viewModel.FullQuery = $"\"{string.Join(" | ", queryString)}\"";

            if (!possibleProviders.Any())
            {
                TempData["Message"] = "Provider not found";
            }
            else
            {
                var providerIDs = possibleProviders.Select(p => p.ARUMProviderID).ToList();
                var tempProvIDMap = CreateTempLinkingIDs(providerIDs);

                ProviderInfo providerToReturn = possibleProviders.First();
                RetrieveSession.SelectedProviderID = providerToReturn.ARUMProviderID;

                var statLogs = StatLogBL.GetStatLogsByARUMProviderList(_classification, providerIDs);
                if (!StatLogConfiguration.CanHide(new[] { RetrieveSession.CurrentRole }))
                {
                    statLogs.RemoveAll(sl => sl.IsActive == false);
                }

                viewModel.ARUMProviderName = providerToReturn.Name;
                viewModel.ARUMProviderMCAID = providerToReturn.MCAID;
                viewModel.ARUMProviderPrimaryIdentifier = providerToReturn.Number;
                viewModel.myProviderResults = possibleProviders.Select(p => new SearchProviderViewModel.SearchProviderResults
                {
                    ProviderID = tempProvIDMap[(int)p.ARUMProviderID].ToString(),
                    Name = p.Name,
                    MCAID = p.MCAID,
                    Number = p.Number,
                    EffectiveDate = p.EffectiveDate
                }).ToList();
                viewModel.mySearchResults = statLogs.Select(m => new SearchProviderViewModel.SearchProviderStatLogResults
                {
                    CRID = ToClientID(m.CRID, COST_REPORT_ID_STATLOG),
                    ProviderID = tempProvIDMap[(int)m.ARUMProviderID].ToString(),
                    AuditType = m.AuditType,
                    BeginDate = m.BeginDate,
                    BeginDateString = m.BeginDate.ToString(),
                    EndDate = m.EndDate,
                    EndDateString = m.EndDate.ToString(),
                    Stamp = m.Stamp
                }).ToList();
            }

            return View(viewModel);
        }
        #endregion

        #region File Upload / Download
        /// <summary>
        /// Uploads the specified s initial identifier.
        /// </summary>
        /// <param name="sHistoryID">The s initial identifier.</param>
        /// <returns>Sets up the Upload Page and passes the view model to the page.</returns>
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult Upload(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "Upload", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            if (!map.History.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (!map.History.Uploadable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: History can not be uploaded to.");

            if (!map.History.IsActive)
                return StatlogMessageJson("Error: History can not not active and can't be uploaded to.");

            var applicationFileType = _fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "StatLog/SaveFile")
                .FileTypes.FirstOrDefault(aft => aft.ApplicationFileTypeID == Parse(map.History.Event.ARUMFileType));

            if (applicationFileType == null)
            {
                return StatlogMessageJson("Error: Issue Retrieving File Type.");
            }

            var myViewModel = new UploadViewModel.View
            {
                sHistoryID = sHistoryID,
                ContainsPHI = map.History.IsPHI,
                ARUMFileTypeID = map.History.Event.ARUMFileType,
                ContactID = _user.UserID,
                InitialID = historyID,
                InitialEventName = map.History.Event.IsSystemEvent
                    ? map.History.ParentHistory.EventName
                    : map.History.EventName,
                CostReportID = map.StatLog.CRID,
                ARUMProviderID = map.Provider.ARUMProviderID,
                BlackList = map.History.Event.BlackList,
                WhiteList = map.History.Event.WhiteList,
                WhiteListExtensions = applicationFileType.AllowedFileExtensions,
                MaxFileSize = applicationFileType.AllowedFileSize,
                CostReportEndDate = map.StatLog.EndDate,
                ProviderName = map.Provider.Name
            };

            return View(myViewModel);

        }

        /// <summary>
        /// Saves the file.
        /// </summary>
        /// <param name="myViewModel">My view model.</param>
        /// <returns>To Index Page with message that data has been saved correctly.</returns>
        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFile(UploadViewModel.Save myViewModel)
        {
            int historyID = ToInternalID(myViewModel.sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "SaveFile", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            HistoryDO history = map.History;
            StatLogDTO costReport = map.StatLog;
            ProviderInfo provider = map.Provider;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");

            if (!history.Uploadable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: History can not be uploaded to.");

            if (!history.IsActive)
                return StatlogMessageRedirect(map, "Error: History can not not active and can't be uploaded to.");

            HttpPostedFileBase uploadedFile = myViewModel.UploadedFile;

            if (uploadedFile == null)
                return StatlogMessageRedirect(map, "Error: File is missing from upload. Please try uploading your file again.");

            if (uploadedFile.ContentLength <= 0)
                return StatlogMessageRedirect(map, "Error: The uploaded file was empty.");

            if (!history.ValidFileName(uploadedFile.FileName))
                return StatlogMessageRedirect(map, "Error: The file name in this upload is not valid.");

            IFileType applicationFileType = _fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "StatLog/SaveFile")?
                .FileTypes?.FirstOrDefault(aft => aft.ApplicationFileTypeID == Parse(history.Event.ARUMFileType));

            if (applicationFileType == null || applicationFileType.ApplicationFileTypeID < 1)
                return StatlogMessageRedirect(map, "Error: Issue Retrieving File Type.");

            if (applicationFileType.ParentFileTypeID == null)
                return StatlogMessageRedirect(map, "Error: Issue Retrieving File Type Parent.");

            if (uploadedFile.ContentLength > applicationFileType.AllowedFileSize)
                return StatlogMessageRedirect(map, $"Error: Uploaded file is too large. Maximum file size is: {applicationFileType.AllowedFileSize} bytes.");

            if (!IsValidFileExtension(uploadedFile.FileName, applicationFileType))
                return StatlogMessageRedirect(map, "Error: Invalid file extension for event. Please upload a different file.");

            
            #region Upload File

            string sourceType = RetrieveSession.IsMSLCEmployee ? "MSU" : "PU"; // Myers and Stauffer upload / Provider Upload
            IRemoteFileUploadResult myUploadService = _fileServiceProxy.UploadFileForProvider("SaveFile",
                0, applicationFileType.ApplicationFileTypeID, 0, 0,
                uploadedFile.FileName, uploadedFile.ContentLength, Empty,
                "Upload/UploadFile", provider.ARUMProviderID, "StatLog/SaveFile", sourceType, _user.UserID,
                uploadedFile.InputStream, true);

            if (!myUploadService.Result?.Successful ?? true)
            {
                string unsuccessful = CreateUl(new List<string> { uploadedFile.FileName }, "There were issues uploading the following files:");
                EmailLogic.InsertErrorEmail(_classification, unsuccessful, $"Upload Controller:  Issue Uploading. ProviderID: {provider.ARUMProviderID} Provider Name: {provider.Name}");
                return StatlogMessageRedirect(map, "Error: Unable to upload file, please contact support.");
            }


            Guid fileID = myUploadService.File.FileID;
            string fileName = myViewModel.UploadedFile.FileName.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last();

            #endregion

            #region Add Upload History

            bool fileContainsPHI = history.IsPHI || myViewModel.ContainsPHI;
            var sanitizer = new HtmlSanitizer();
            HistoryDTO uploadHistory = StatLogBL.InsertHistory(_classification, costReport.CRID,
                    StatLogConfiguration.EventIDForUpload, history.ID,
                    sanitizer.Sanitize(myViewModel.Notes),
                    fileContainsPHI, _user.Email, myUploadService.File.FileID, DateTime.Now, null,
                    null, null, _user.UserID, "StatLog", "SaveFile", provider.ARUMProviderID);

            if (uploadHistory == null)
                return StatlogMessageRedirect(map, "Error with inserting upload into StatLog History.");

            #endregion

            HistoryDTO parentHistory = history.ToDTO();
            EventDO @event = history.Event;

            string emailResult = ProcessLifeCycle.Upload(parentHistory, @event, _user, RetrieveSession.IsMSLCEmployee, costReport, provider,
                applicationFileType.ParentFileTypeName, (int)applicationFileType.ParentFileTypeID, uploadHistory.ID, fileID, fileName);

            if (!IsNullOrEmpty(emailResult))
            {
                EmailLogic.InsertErrorEmail(_classification, "There was an issue with sending an email. SendEmail Result: " + emailResult, $"StatLog Controller: Issue Uploading. ProviderID: {provider.ARUMProviderID} Provider Name: {provider.ARUMProviderID}");
            }

            //Commenting this out for now as newer Excel versions cause this check to fail. Will be readded when we upgrade Syncfusion. -GSturgill 04/29/2024
            //byte[] bytes = FileUtil.GetByteArraryFromHttpPostedFileBase(uploadedFile);
            //var memStream = new MemoryStream(bytes);

            //var errorMessage = "Error with file upload.";
            //if (FileHelp.IsExcelFile(uploadedFile.ContentType) && !FileHelp.IsWorkbookCompatible(memStream, ref errorMessage))
            //{
            //    TempData["SuccessMessageAttachmentUpload"] = errorMessage;
            //    CostReportHistoryBL.Review(_classification, uploadHistory.ID, @event.ID, new[] { RetrieveSession.CurrentRole }, _user.Email, false, TempData["SuccessMessageAttachmentUpload"].ToString());
            //}

            return StatlogMessageRedirect(map, "File Upload Successful.");
        }

        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult DownloadFile(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "DownloadFile", "Statlog:View");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            if (!map.History.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");

            if (!map.History.Downloadable(new[] { RetrieveSession.CurrentRole }, ProviderUserHelper.IsProviderUserPhiSafe(map.Provider.ARUMProviderID, RetrieveSession.ARUMUser)))
                return StatlogMessageRedirect(map, "Error: You do not have access to download this file.");

            if (!map.History.FileFoundInARUM)
                return StatlogMessageRedirect(map, "Error: File is not active or could not be found in system, please contact support.");

            if (!map.History.IsActive)
                return StatlogMessageRedirect(map, "Error: History is not active, and can't be downloaded from.");

            ISingleFileResult myDownloadedFile = _fileServiceProxy.GetFileByID(_user.UserID,
                (Guid)map.History.FileID, "Stat Log - Download", "DownloadFile", "Stat Log");

            if (myDownloadedFile == null ||
                IsNullOrEmpty(myDownloadedFile.FileName) ||
                myDownloadedFile.myFile == null ||
                myDownloadedFile.myFile.Length <= 0)
                return StatlogMessageRedirect(map, "Error: File cannot be retrieved at this time, please contact support.");

            return File(myDownloadedFile.myFile, "application/force-download",
                    myDownloadedFile.FileName.Trim() + "." + myDownloadedFile.Extension);

        }

        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult PhiWarning(string sHistoryID)
        {
            var view = new PHIWarningViewModel.View() { sHistoryID = sHistoryID };
            return View(view);
        }

        [HttpPost]
        [Route("PhiWarning")]
        public ActionResult PhiWarning(PHIWarningViewModel.Save myViewModel, string command)
        {
            var sc = new StatLogController();
            return sc.DownloadFile(myViewModel.sHistoryID);
        }

        #endregion

        #region Review
        /// <summary>
        /// Reviews this instance.
        /// </summary>
        /// <returns>Setup the view model and pass it to the review page.</returns>
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult Review(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "Review", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("This history is not viewable.");

            if (!history.Reviewable(new[] { RetrieveSession.CurrentRole }, _user.Email))
                return StatlogMessageJson("This history is not reviewable.");

            if (history.IsActive == false)
                return StatlogMessageJson("This history has been disabled.");


            string reviewAcceptText = IsNullOrWhiteSpace(history.Event.ReviewAcceptText) ? "Accept" : history.Event.ReviewAcceptText;
            string reviewRejectText = IsNullOrWhiteSpace(history.Event.ReviewRejectText) ? "Reject" : history.Event.ReviewRejectText;

            #region Retrieve file type
            var fileType = "";
            if (!history.Event.NeedsTextReview && !IsNullOrWhiteSpace(history.GetFileType))
            {
                var applicationFileTypes = _fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "StatLog/SaveFile")?.FileTypes;
                IFileType applicationFileType = applicationFileTypes?.FirstOrDefault(aft => aft.ApplicationFileTypeID == Parse(history.GetFileType));
                if (applicationFileType != null)
                {
                    fileType = applicationFileType.ParentFileTypeName + (applicationFileType.ChildFileTypeName != "N/A" ? " - " + applicationFileType.ChildFileTypeName : Empty);
                }
            }
            #endregion

            var myViewModel = new ReviewViewModel.View
            {
                InitialEventName = history.Event.IsSystemEvent ? history.ParentHistory?.EventName ?? history.EventName : history.EventName,
                sHistoryID = sHistoryID,
                FileType = fileType,
                ReviewUserName = _user.Email,
                ReviewDate = DateTime.Now.ToShortDateString(),
                ReviewUserType = $"{RetrieveSession.CurrentRoleName} Review",
                ReviewAcceptText = reviewAcceptText,
                ReviewRejectText = reviewRejectText,
                CanAcceptReview = !history.HasReview && history.HasPermissionToAcceptReview(new[] { RetrieveSession.CurrentRole }),
                CanRejectReview = !history.HasReview && history.HasPermissionToRejectReview(new[] { RetrieveSession.CurrentRole }),
                CanReleaseReview = history.HasReview && history.HasPermissionToReleaseReview(new[] { RetrieveSession.CurrentRole }),
                IsEventReview = history.Event.NeedsTextReview,
                Notes = Empty,
                ReviewText = AddValuesToReviewText(history, history.GetReviewText, map.StatLog)
            };

            return View(myViewModel);
        }

        /// <summary>
        /// Saves the review.
        /// </summary>
        /// <param name="myViewModel">My view model.</param>
        /// <param name="command">The command.</param>
        /// <returns>To Index Page with message that data has been saved correctly.</returns>
        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveReview(ReviewViewModel.Save myViewModel, string command)
        {
            var reviewTime = new Stopwatch();
            reviewTime.Start();
            int historyID = ToInternalID(myViewModel.sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "SaveReview", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            if (!map.History.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");

            if (!map.History.Reviewable(new[] { RetrieveSession.CurrentRole }, _user.Email))
                return StatlogMessageRedirect(map, "Error: You do not have access to review this StatLog History.");

            if (map.History.IsActive == false)
                return StatlogMessageRedirect(map, "Error: This history is deactivated and can not be reviewed right now.");
            
            HistoryDO userDefinedEvent = map.History.Event.IsSystemEvent ? map.History.ParentHistory : map.History;

			#region Notes
			string reviewText = AddValuesToReviewText(userDefinedEvent, userDefinedEvent.GetReviewText, map.StatLog) ?? "";
            string userNotes = _sanitizer.Sanitize(myViewModel.Notes?.Trim()) ?? "";

            string notes = reviewText;

            // If there is review text and user notes, add a line break between them.
            if (!IsNullOrWhiteSpace(reviewText) && !IsNullOrWhiteSpace(userNotes))
                notes += "\r\n\r\n----\r\n\r\nUser Notes: ";

            notes += userNotes;
            #endregion
            
            string message = Empty;
            try
            {
                HistoryDTO historyDTO = CostReportHistoryBL.GetHistoryDTOByHistoryID(_classification, userDefinedEvent.ID);

                switch (command)
                {
                    case "Accept":
                        CostReportHistoryBL.Review(_classification, map.History.ID, userDefinedEvent.EventID, new[] { RetrieveSession.CurrentRole }, _user.Email.Trim(), true, _sanitizer.Sanitize(notes));
                        ProcessLifeCycle.Review(LifeCycleEnum.REVIEW_APPROVED, historyDTO, userDefinedEvent.Event, _user, RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider, map.History.FileID);
                        message = "Review Successfully Accepted";
                        break;
                    case "Reject":
                        CostReportHistoryBL.Review(_classification, map.History.ID, userDefinedEvent.EventID, new[] { RetrieveSession.CurrentRole }, _user.Email.Trim(), false, _sanitizer.Sanitize(notes));
                        ProcessLifeCycle.Review(LifeCycleEnum.REVIEW_REJECTED, historyDTO, userDefinedEvent.Event, _user, RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider, map.History.FileID);
                        message = "Review Successfully Rejected";
                        break;
                    case "ReleaseReview":
                        CostReportHistoryBL.Review(_classification, map.History.ID, userDefinedEvent.EventID, new[] { RetrieveSession.CurrentRole }, _user.Email.Trim(), null, _sanitizer.Sanitize(notes));
                        ProcessLifeCycle.Review(LifeCycleEnum.REVIEW_RELEASED, historyDTO, userDefinedEvent.Event, _user, RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider, map.History.FileID);
                        message = "Successfully Released/Reviewed";
                        break;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                message = "Error taking action on review.";
            }

            reviewTime.Stop();
            Log.Information("StatLog/{Action} for {Classification} provider {ARUMProviderID} on history {HistoryID} took {Elapsed} s to run", "SaveReview", _classification.DisplayName, map.StatLog?.ARUMProviderID, historyID, reviewTime.Elapsed.TotalSeconds);
            return StatlogMessageRedirect(map, message);
        }
        #endregion

        #region Issue (used to be NA)
        [HttpGet]
        // TODO: The ViewIssueIcon is tied to this endpoint it needs to use showNotes instead so that way this endpoint can be decorated with StatLog:Interact
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult ShowNotApplicable(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowNotApplicable", "Statlog:View");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (history.EventID == StatLogConfiguration.EventIDForIssue)
            {
                if (IsNullOrWhiteSpace(history.Notes))
                    return StatlogMessageJson("Error: This history is has no issues to view.");
            }
            else
            {
                if (!history.Uploadable(new[] { RetrieveSession.CurrentRole }))
                    return StatlogMessageJson(
                        "Error: Can not log a issue with a history that can not be uploaded to.");

                if (!history.IsActive)
                    return StatlogMessageJson("Error: History is not active and can't be have an issue logged to it.");

                // We might need to limit how many issues can be logged to an upload. The following would be the logic to do it:
                // if (history.ChildHistory.Any(ch => ch.EventID == StatLogConfiguration.EventIDForIssue))
                //     return StatlogMessageJson("Error: This upload already has an issue logged to it.");
                //
                // If we decide to add this check, it needs to be added to the SaveNotApplicable function.
            }

            bool readOnly = history.Event.ID == StatLogConfiguration.EventIDForIssue;
            string notes = history.Event.ID == StatLogConfiguration.EventIDForIssue ? history.Notes : Empty;

            var sanitizer = new HtmlSanitizer();
            var myViewModel = new NotApplicableViewModel.View
            {
                sHistoryID = sHistoryID,
                InitialEventName = history.Event.IsSystemEvent ? history.ParentHistory?.EventName ?? history.EventName : history.EventName,
                ReadOnly = readOnly,
                Notes = sanitizer.Sanitize(notes)
            };

            return View(myViewModel);
        }

        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveNotApplicable(NotApplicableViewModel.Save myViewModel)
        {
            int historyID = ToInternalID(myViewModel.sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "SaveNotApplicable", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");

            if (!history.Uploadable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: Can not log a issue with a history that can not be uploaded to.");

            if (!history.IsActive)
                return StatlogMessageRedirect(map, "Error: History is not active and can't be have an issue logged to it.");

            if (IsNullOrWhiteSpace(myViewModel.Notes))
                return StatlogMessageRedirect(map, "Error: Notes were empty and could not be saved.");

            var sanitizer = new HtmlSanitizer();
            CostReportHistoryBL.NAUpload(_classification, map.History, _user.Email,
                sanitizer.Sanitize(myViewModel.Notes));

            return StatlogMessageRedirect(map, "Note Successfully Reported on History");
        }
        #endregion

        #region N/A (Not Applicable has made a comeback)

        /// <summary>
        /// Shows the new N/A (not applicable) view
        /// </summary>
        /// <param name="sHistoryID"></param>
        /// <returns></returns>
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult ShowNA(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowNotApplicable", "Statlog:View");
            if (map.Error)
            {
                return StatlogMessageJson(map.ErrorMessage);
            }

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (history.EventID == StatLogConfiguration.EventIDForNA)
            {
                if (IsNullOrWhiteSpace(history.Notes))
                {
                    return StatlogMessageJson("Error: This history has no issues to view.");
                }
            }
            else
            {
                if (history.Uploadable(new[] { RetrieveSession.CurrentRole }))
                {
                    if (!history.IsActive)
                        return StatlogMessageJson(
                            "Error: History is not active and can't be have N/A logged to it.");
                }
                else
                {
                    return StatlogMessageJson(
                        "Error: Can not log N/A with a history that can not be uploaded to.");
                }
            }

            bool readOnly = history.Event.ID == StatLogConfiguration.EventIDForNA;
            string notes = history.Event.ID == StatLogConfiguration.EventIDForNA ? history.Notes : Empty;

            var sanitizer = new HtmlSanitizer();
            var myViewModel = new NotApplicableViewModel.View
            {
                sHistoryID = sHistoryID,
                InitialEventName = history.Event.IsSystemEvent ? history.ParentHistory?.EventName ?? history.EventName : history.EventName,
                ReadOnly = readOnly,
                Notes = sanitizer.Sanitize(notes)
            };

            return View(myViewModel);
        }

        /// <summary>
        /// Saves the new N/A (not applicable event)
        /// </summary>
        /// <param name="myViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveNA(NotApplicableViewModel.Save myViewModel)
        {
            int historyID = ToInternalID(myViewModel.sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "SaveNA", "Statlog:Interact");
            if (map.Error)
            {
                return StatlogMessageRedirect(map, map.ErrorMessage);
            }

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
            {
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");
            }

            if (!history.Uploadable(new[] { RetrieveSession.CurrentRole }))
            {
                return StatlogMessageRedirect(map, "Error: Can not log an N/A with a history that can not be uploaded to.");
            }

            if (!history.IsActive)
            {
                return StatlogMessageRedirect(map, "Error: History is not active and can't be have an N/A logged to it.");
            }

            if (IsNullOrWhiteSpace(myViewModel.Notes))
            {
                return StatlogMessageRedirect(map, "Error: Notes were empty and could not be saved.");
            }

            var sanitizer = new HtmlSanitizer();
            CostReportHistoryBL.NotApplicableSave(_classification, map.History, _user.Email, sanitizer.Sanitize(myViewModel.Notes));

            var processLifeCycle = new ProcessLifeCycle(_classification, PortalURL, _arumUserProxy, _fileServiceProxy, _providerProxy);
            processLifeCycle.NotApplicableEventAdded(map.History.ToDTO(), map.History.Event, RetrieveSession.ARUMUser, RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider);


            return StatlogMessageRedirect(map, "Note Successfully Reported on History");
        }

        #endregion

        #region Due Date
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult EditEventDate(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "EditEventDate", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            if (!map.History.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (map.History.IsActive == false)
                return StatlogMessageJson("Error: This history is deactivated and can not be edited right now.");

            //This check means you can only modify a due date if it's present.  Can't modify blank due dates.
            bool canEditResponseDate = map.History.HasPermissionToEditResponseDate(new[] { RetrieveSession.CurrentRole });
            bool canEditDueDate = map.History.ExpectDate != null && map.History.HasPermissionToEditDueDate(new[] { RetrieveSession.CurrentRole });
            bool canEditEventDate = map.History.EventDate != null && map.History.HasPermissionToEditEventDate(new[] { RetrieveSession.CurrentRole });

            if (!canEditResponseDate && !canEditDueDate && !canEditEventDate)
                return StatlogMessageJson("Error: You do not have access to do this on this history.");


            var myViewModel = new EditEventDateViewModel.View
            {
                sHistoryID = sHistoryID,
                oldDueDate = map.History.ExpectDate,
                newDueDate = map.History.ExpectDate,
                providerName = map.Provider.Name,
                InitialEventName = map.History.Event.IsSystemEvent
                    ? map.History.ParentHistory?.EventName ?? map.History.EventName
                    : map.History.EventName, 
                oldResponseDate = map.History.Response, 
                newResponseDate = map.History.Response,
                oldEventDate = map.History.EventDate,
                newEventDate = map.History.EventDate,
                canEditDueDate = (map.History.ExpectDate != null && map.History.HasPermissionToEditDueDate(new[] { RetrieveSession.CurrentRole })),
                canEditResponseDate = map.History.HasPermissionToEditResponseDate(new[] { RetrieveSession.CurrentRole }),
                canEditEventDate = (map.History.EventDate != null && map.History.HasPermissionToEditEventDate(new[] { RetrieveSession.CurrentRole }))
            };

            return View(myViewModel);
        }

        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveEventDates(EditEventDateViewModel.Save myViewModel, string command)
        {
            int historyID = ToInternalID(myViewModel.sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "EditEventDate", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            if (!map.History.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");

            if (map.History.IsActive == false)
                return StatlogMessageRedirect(map, "Error: This history is deactivated and can not be edited right now.");

            //This check means you can only modify a due date if it's present.  Can't modify blank due dates.
            bool canEditResponseDate = map.History.HasPermissionToEditResponseDate(new[] { RetrieveSession.CurrentRole });
            bool canEditDueDate = map.History.ExpectDate != null && map.History.HasPermissionToEditDueDate(new[] { RetrieveSession.CurrentRole });
            bool canEditEventDate = map.History.EventDate != null && map.History.HasPermissionToEditEventDate(new[] { RetrieveSession.CurrentRole });

            if (!canEditResponseDate && !canEditDueDate && !canEditEventDate)
                return StatlogMessageRedirect(map, "Error: You do not have access to do this on this history.");

            if (canEditDueDate && !myViewModel.newDueDate.HasValue)
                return StatlogMessageRedirect(map, "Error:  Due Date cannot be set to blank.");

            if (canEditEventDate && !myViewModel.newEventDate.HasValue)
                return StatlogMessageRedirect(map, "Error:  Event Date cannot be set to blank.");

            string message = "No change occurred to Event Date(s).";

            if (canEditDueDate && map.History.ExpectDate != myViewModel.newDueDate)
            {
                //save the response date if something changed.  Ok to change to null.
                CostReportHistoryBL.ChangeDueDate(_classification, map.History.ID, (int)map.StatLog.ARUMProviderID, myViewModel.newDueDate, _user.Email);
                message = "Due Date(s) Successfully changed.";
            }
            if (canEditResponseDate && map.History.Response != myViewModel.newResponseDate)
            {
                //save the response date if something changed.  Ok to change to null.
                CostReportHistoryBL.ChangeResponseDate(_classification, map.History.ID, myViewModel.newResponseDate, _user.Email);
                message = "Response Date(s) Successfully changed.";
            }
            if (canEditEventDate && map.History.EventDate != myViewModel.newEventDate)
            {
                //save the event date if something changed.  Ok to change to null.
                CostReportHistoryBL.ChangeEventDate(_classification, map.History.ID, myViewModel.newEventDate, _user.Email);
                message = "Event Date(s) Successfully changed.";
            }
            return StatlogMessageRedirect(map, message);
        }
        #endregion

        #region Add New Event
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult ShowAddInteractive(string sCostReportID, string sEventID)
        {
            int costReportID = ToInternalID(sCostReportID, COST_REPORT_ID_STATLOG);
            int eventID = ToInternalID(sEventID, EVENT_ID_STATLOG);

            Mapping map = MapByCostReportID(costReportID, "Statlog:Interact");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            EventDO eventDO = CostReportHistoryBL
                .GetInteractiveEvents(_classification, new[] { RetrieveSession.CurrentRole }, costReportID, false)
                .FirstOrDefault(x => x.ID == eventID);

            if (eventDO == null)
                return StatlogMessageJson("Error: Unable to retrieve selected event, please try again.");

            var myViewModel = new AddInteractiveViewModel.View
            {
                sCostReportID = sCostReportID,
                sEventID = sEventID,
                Event = $"{eventDO.Name}{(IsNullOrWhiteSpace(eventDO.Descript) ? Empty : $" - {eventDO.Descript}")}",
                SetEventDateState = eventDO.SetEventDateState,
                SetExpectedDateState = eventDO.SetExpectedDateState,
                AssignUser = eventDO.EnableAssignUser
            };

            if (myViewModel.AssignUser)
            {
                var allAssignableUsers = UserHelper.GetAllAssignableUsers(_classification, map.Provider.ARUMProviderID, eventDO.AssignableUserRoleList);
                var assignableUserDictionary = allAssignableUsers
                    .OrderBy(y => y.LastName)
                    .ThenBy(y => y.FirstName)
                    .ToDictionary(
                        user => ToClientID(user.ARUMUserID, ASSIGNED_STATE_USER_STATLOG),
                        user => user.FirstName + " " + user.LastName);

                myViewModel.AssignableUsersSelectList = new SelectList(assignableUserDictionary, "Key", "Value");
            }

            return View(myViewModel);
        }

        // TODO: Clean up this function to be more streamlined. 
        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveAddInteractive(AddInteractiveViewModel.Save myViewModel)
        {
            int costReportID = ToInternalID(myViewModel.sCostReportID, COST_REPORT_ID_STATLOG);
            int eventID = ToInternalID(myViewModel.sEventID, EVENT_ID_STATLOG);
            int assignedUserID = ToInternalID(myViewModel.sAssignedUserID, ASSIGNED_STATE_USER_STATLOG);

            Mapping map = MapByCostReportID(costReportID, "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            EventDO eventDO = CostReportHistoryBL
                .GetInteractiveEvents(_classification, new[] { RetrieveSession.CurrentRole }, costReportID, false)
                .FirstOrDefault(x => x.ID == eventID);

            if (eventDO == null)
                return StatlogMessageRedirect(map, "Error: Unable to retrieve selected event, please try again.");

            if (assignedUserID > 0)
            {
                if (!eventDO.EnableAssignUser)
                    return StatlogMessageRedirect(map, "Error: Selected event can't have a user assigned, please try again.");

                var allAssignableUsers = UserHelper.GetAllAssignableUsers(_classification, map.Provider.ARUMProviderID, eventDO.AssignableUserRoleList);
                bool validAssignedUser = allAssignableUsers.Any(user => user.ARUMUserID == assignedUserID);

                if (!validAssignedUser)
                    return StatlogMessageRedirect(map, "Error: Selected assigned user is not a valid option, please try again.");
            }

            try
            {
                var sanitizer = new HtmlSanitizer();
                var historyDTO = CostReportHistoryBL.SaveAddInteractive(_classification,
                    map.StatLog.CRID, eventID, myViewModel.EventDate,
                    myViewModel.ExpectedDate, assignedUserID, _user.Email,
                    sanitizer.Sanitize(myViewModel.Notes));

                ProcessLifeCycle.Create(historyDTO, eventDO, _user, RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider);

                if (assignedUserID > 0)
                {
                    ProcessLifeCycle.UserChanged(LifeCycleEnum.USER_ASSIGNED, historyDTO, eventDO, _user, RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider, map.History?.FileID);
                }

                return StatlogMessageRedirect(map, "Event Successfully Added");
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return StatlogMessageRedirect(map, "Error adding new event.");
            }
        }
        #endregion

        #region Assign User
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult AssignUser(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "AssignUser", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (!history.Assignable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history can not have a user assigned to it.");

            if (!history.IsActive)
                return StatlogMessageJson("Error: This history is deactivated and can not have a user assigned to it right now.");

            var allProviderUsers = UserHelper.GetAllAssignableUsers(_classification, map.Provider.ARUMProviderID, map.History.Event.AssignableUserRoleList);
            
            var assignableUsersDictionary = allProviderUsers.OrderBy(y => y.LastName)
                .ThenBy(y => y.FirstName)
                .ToDictionary(user => ToClientID(user.ARUMUserID, ASSIGNED_STATE_USER_STATLOG), assignableUser => assignableUser.FirstName + " " + assignableUser.LastName);

            var myViewModel = new AssignUserViewModel.View()
            {
                sHistoryID = sHistoryID,
                InitialEventName = history.Event.IsSystemEvent ? history.ParentHistory?.EventName ?? history.EventName : history.EventName,
                AssignedName = history.UserAssignedName,
                sAssignedUserID = ToClientID(history.AssignedUserID, ASSIGNED_STATE_USER_STATLOG),
                AssignableUsersSelectList = new SelectList(assignableUsersDictionary, "Key", "Value")
            };

            return View(myViewModel);
        }

        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveAssignedUser(AssignUserViewModel.Save myViewModel)
        {
            int historyID = ToInternalID(myViewModel.sHistoryID, HISTORY_ID_STATLOG);
            int newAssignedUserID = ToInternalID(myViewModel.sAssignedUserID, ASSIGNED_STATE_USER_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "SaveAssignedUser", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");

            if (!history.Assignable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history can not have a user assigned to it.");

            if (!history.IsActive)
                return StatlogMessageRedirect(map, "Error: This history is deactivated and can not have a user assigned to it right now.");

            if (newAssignedUserID < 1)
                return StatlogMessageRedirect(map, "Error: Invalid user selected.");

            var allProviderUsers = UserHelper.GetAllAssignableUsers(_classification, map.Provider.ARUMProviderID, map.History.Event.AssignableUserRoleList);
            var validAssignedUser = allProviderUsers.Any(user => user.ARUMUserID == newAssignedUserID);
            if (!validAssignedUser)
                return StatlogMessageRedirect(map, "Error: Selected assigned user is not a valid option, please try again.");
            if (map.History.AssignedUserID == newAssignedUserID)
                return StatlogMessageRedirect(map, "Selected assigned user is already assigned.");

            try
            {
                StatLogDTO costReport = map.StatLog;
                ProviderInfo provider = map.Provider;

                //insert history
                var changeAssignedUserHistoryID =
                    CostReportHistoryBL.SaveAssignedUser(_classification, costReport.CRID, history.ID, _user.UserID, newAssignedUserID);

                var previousAssignedUserID = history.AssignedUserID;
                if (previousAssignedUserID != null)
                {
                    ProcessLifeCycle.UserChanged(LifeCycleEnum.USER_REMOVED, map.History.ToDTO(), map.History.Event, _user,
                        RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider, map.History.FileID);
                }

                map.History.AssignedUserID = newAssignedUserID;
                if (myViewModel.sAssignedUserID != null)
                {
                    ProcessLifeCycle.UserChanged(LifeCycleEnum.USER_ASSIGNED, map.History.ToDTO(),
                        map.History.Event, _user, RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider, map.History.FileID);
                }

                return StatlogMessageRedirect(map, "User Assignment Completed.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return StatlogMessageRedirect(map, "Error: Could not save assigned user to history, please contact support.");
            }
        }
        #endregion

        #region Create StatLog
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult CreateStatLog(string sArumProviderID, string sCostReportID)
        {
            int providerID = ToInternalID(sArumProviderID, PROVIDER_ID_STATLOG);
            int costReportID = ToInternalID(sCostReportID, COST_REPORT_ID_STATLOG);

            Mapping map = costReportID > 0 ? MapByCostReportID(costReportID, "Statlog:Interact") : MapByProviderID(providerID, "Statlog:Interact");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            if (!StatLogConfiguration.CanCreateStatLog(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: You currently do not have access to create/edit StatLogs.");

            ProviderInfo provider = map.Provider;

            var providerTypes = ProviderBL.GetAllActiveProvTypes(_classification);

            if (!providerTypes.Any())
                StatlogMessageJson($"Error: Currently there are no active provider types for {_classification.DisplayName}, please contact support.");

            var auditTypes = AuditTypeBL.GetAllActiveAuditTypes(_classification);

            if (!auditTypes.Any())
                StatlogMessageJson($"Error: Currently there are no active {PortalConfiguration.AuditTypeReplacementName}s for {_classification.DisplayName}, please contact support.");

            bool hideDueDate = StatlogARUMConfiguration.HideDueDateField(_classification);

            var myViewModel = new EditStatLogViewModel.View
            {
                sARUMProviderID = ToClientID(map.Provider.ARUMProviderID, PROVIDER_ID_STATLOG),
                ProviderName = provider.Name,
                ProviderTypes = providerTypes,
                AuditTypes = auditTypes,
                ProviderPrimaryIdentifierType = provider.NumberTypeDisplay,
                ProviderPrimaryIentifier = IsNullOrEmpty(provider.Number) ? provider.MCAID : provider.Number,
                HideDueDateField = hideDueDate,

                ProviderType = providerTypes.FirstOrDefault()?.ProvType1,
                BeginDate = new DateTime(DateTime.Now.Year, 1, 1),
                DueDate = hideDueDate ? DateTime.MaxValue : new DateTime(DateTime.Now.Year + 1, 5, 31),
                EndDate = new DateTime(DateTime.Now.Year, 12, 31),
                Notes = "",
                IsActive = true
            };

            var sanitizer = new HtmlSanitizer();

            // ReSharper disable once InvertIf
            if (map.StatLog != null)
            {
                myViewModel.sCostReportID = ToClientID(map.StatLog.CRID, PROVIDER_ID_STATLOG);

                // TODO: Go through whole package to figure out how to deal with disabled audit types / provider types
                myViewModel.ProviderType = map.StatLog.ProviderType;
                myViewModel.AuditType = map.StatLog.AuditType;
                myViewModel.BeginDate = map.StatLog.BeginDate;
                myViewModel.DueDate = map.StatLog.DueDate;
                myViewModel.EndDate = map.StatLog.EndDate;
                myViewModel.Notes = sanitizer.Sanitize(map.StatLog.Notes);
                myViewModel.IsActive = map.StatLog.IsActive ?? true;
            }
            return View(myViewModel);
        }

        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult StatlogValidation(EditStatLogViewModel.Save statLog)
        {
            List<StatLogValidation.ValidationMessage> validationMessages = new List<StatLogValidation.ValidationMessage>();

            int providerID = ToInternalID(statLog.sARUMProviderID, PROVIDER_ID_STATLOG);
            int costReportID = ToInternalID(statLog.sCostReportID, COST_REPORT_ID_STATLOG);

            Mapping map = costReportID > 0 ? MapByCostReportID(costReportID, "Statlog:Interact") : MapByProviderID(providerID, "Statlog:Interact");
            if (map.Error)
            {
                validationMessages.Add(new StatLogValidation.ValidationMessage(map.ErrorMessage, StatLogValidation.ValidationType.Critical));
                return Json(validationMessages);
            }

            if (!StatLogConfiguration.CanCreateStatLog(new[] { RetrieveSession.CurrentRole }))
            {
                var errorMessage = "Error: You currently do not have access to create/edit StatLogs.";
                validationMessages.Add(new StatLogValidation.ValidationMessage(errorMessage, StatLogValidation.ValidationType.Critical));
                return Json(validationMessages);
            }

            var providerTypes = ProviderBL.GetAllActiveProvTypes(_classification);

            if (providerTypes.All(pt => pt.ProvType1.Trim() != statLog.ProviderType?.Trim()))
            {
                var errorMessage = "Error: Selected provider type could not be retrieved at this time, please contact support.";
                validationMessages.Add(new StatLogValidation.ValidationMessage(errorMessage, StatLogValidation.ValidationType.Critical));
                return Json(validationMessages);
            }

            var auditTypes = AuditTypeBL.GetAllActiveAuditTypes(_classification);

            if (auditTypes.All(at => at.AuditType.Trim() != statLog.AuditType?.Trim()))
            {
                var errorMessage = $"Error: Selected {PortalConfiguration.AuditTypeReplacementName} could not be retrieved at this time, please contact support.";
                validationMessages.Add(new StatLogValidation.ValidationMessage(errorMessage, StatLogValidation.ValidationType.Critical));
                return Json(validationMessages);
            }

            var sanitizer = new HtmlSanitizer();
            var newStatLog = new StatLogDTO
            {
                ARUMProviderID = map.Provider.ARUMProviderID,
                CRID = map.StatLog?.CRID ?? 0,
                ProviderType = statLog.ProviderType,
                AuditType = statLog.AuditType,
                BeginDate = statLog.BeginDate,
                DueDate = statLog.DueDate,
                EndDate = statLog.EndDate,
                Notes = sanitizer.Sanitize(statLog.Notes) ?? "",
                IsActive = statLog.IsActive,
                UserID = _user.Email,
                Stamp = DateTime.Now,
                MCDVersion = "", // Depreciated
                MCRVersion = "", // Depreciated
                MSComplete = 0, // Depreciated
                CHOWID = 0, // Depreciated
                OwnerID = 0, // Foreign key to the Owner table. The owner can be an ID from various tables. Chain, Rate, etc. 
                OwnerType = ""
                // The type indicating the owner of this StatLog record. Some records are owned by providers, others by firms:
                // Chains, Mgmt Firms, etc, or perhaps by Rate records. Use P = Providers, C = Chains, A = Accounting firms, M = Mgmt, R = Rates.
            };

            validationMessages = FauxDependencyInjection.StatLogDateValidation.Validate(_classification, newStatLog);
            return Json(validationMessages);
        }

        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveStatLog(EditStatLogViewModel.Save statLog, string command)
        {
            int providerID = ToInternalID(statLog.sARUMProviderID, PROVIDER_ID_STATLOG);
            int costReportID = ToInternalID(statLog.sCostReportID, COST_REPORT_ID_STATLOG);

            Mapping map = costReportID > 0 ? MapByCostReportID(costReportID, "Statlog:Interact") : MapByProviderID(providerID, "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            if (!StatLogConfiguration.CanCreateStatLog(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: You currently do not have access to create/edit StatLogs.");

            if ((command == "Activate" || command == "Deactivate") && !StatLogConfiguration.CanHide(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This user can not activate/deactivate this statlog.");

            var sanitizer = new HtmlSanitizer();
            var newStatLog = new StatLogDTO
            {
                ARUMProviderID = map.Provider.ARUMProviderID,
                CRID = map.StatLog?.CRID ?? 0,
                ProviderType = statLog.ProviderType,
                AuditType = statLog.AuditType,
                BeginDate = statLog.BeginDate,
                DueDate = statLog.DueDate,
                EndDate = statLog.EndDate,
                Notes = sanitizer.Sanitize(statLog.Notes) ?? "",
                IsActive = map.StatLog?.IsActive ?? true,
                UserID = _user.Email,
                Stamp = DateTime.Now,
                MCDVersion = "", // Depreciated
                MCRVersion = "", // Depreciated
                MSComplete = 0, // Depreciated
                CHOWID = 0, // Depreciated
                OwnerID = 0, // Foreign key to the Owner table. The owner can be an ID from various tables. Chain, Rate, etc. 
                OwnerType = ""
                // The type indicating the owner of this StatLog record. Some records are owned by providers, others by firms:
                // Chains, Mgmt Firms, etc, or perhaps by Rate records. Use P = Providers, C = Chains, A = Accounting firms, M = Mgmt, R = Rates.
            };
            var validationMessages = FauxDependencyInjection.StatLogDateValidation.Validate(_classification, newStatLog);

            try
            {
                switch (command)
                {
                    case "Save":
                        //Verify Statlog can be created/edited
                        if (validationMessages.Any())
                            return StatlogMessageRedirect(map, validationMessages.First()._message);

                        if (ProviderBL.GetAllActiveProvTypes(_classification).All(pt => pt.ProvType1.Trim() != statLog.ProviderType?.Trim()))
                            return StatlogMessageRedirect(map, "Error: Selected provider type could not be retrieved at this time, please contact support.");

                        if (AuditTypeBL.GetAllActiveAuditTypes(_classification).All(at => at.AuditType.Trim() != statLog.AuditType?.Trim()))
                            return StatlogMessageRedirect(map, $"Error: Selected {PortalConfiguration.AuditTypeReplacementName} could not be retrieved at this time, please contact support.");

                        int newCostReportID = StatLogBL.InsertStatLog(_classification, newStatLog, "StatLog", "SaveStatLog", map.Provider, ProcessLifeCycle);

                        if (newCostReportID == 0)
                            return StatlogMessageRedirect(map, $"Stat Log was not {(map.StatLog == null ? "saved" : "created")}, please contact support.");

                        return StatlogMessageRedirect(map.Provider.ARUMProviderID, newCostReportID,
                            $"Stat Log has been successfully {(map.StatLog == null ? "created." : "saved.")}");

                    case "Activate":
                        if (validationMessages.Any())
                            return StatlogMessageRedirect(map, validationMessages.First()._message);

                        //Activate StatLog
                        StatLogBL.ChangeHideStatLog(_classification, costReportID);
                        return StatlogMessageRedirect(map, "Stat Log Successfully Activated.");

                    case "Deactivate":
                        StatLogBL.ChangeHideStatLog(_classification, costReportID);
                        return StatlogMessageRedirect(map, "Stat Log Successfully Hidden.");

                    default:
                        return StatlogMessageRedirect(map, "Unknown command.");

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return StatlogMessageRedirect(map, "Error saving statlog.");
            }
        }
        #endregion

        #region Disable/Enable History
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult ShowHideHistory(string sHistoryID, bool hide)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowHideHistory", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);
            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (hide)
            {
                if (!history.Hideable(new[] { RetrieveSession.CurrentRole }))
                    return StatlogMessageJson("Error: This history is can not be hidden.");
            }
            else
            {
                if (!history.Showable(new[] { RetrieveSession.CurrentRole }))
                    return StatlogMessageJson("Error: This history is can not be unhidden.");

            }

            var myViewModel = new HideHistoryViewModel.View
            {
                sHistoryID = sHistoryID,
                Hide = hide,
                InitialEventName = history.Event.IsSystemEvent ? history.ParentHistory?.EventName ?? history.EventName : history.EventName,
            };
            return View(myViewModel);

        }

        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveHideHistory(HideHistoryViewModel.Save myViewModel)
        {
            int historyID = ToInternalID(myViewModel.sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "SaveHideHistory", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);
            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");

            if (myViewModel.Hide)
            {
                if (!history.Hideable(new[] { RetrieveSession.CurrentRole }))
                    return StatlogMessageRedirect(map, "Error: This history is can not be hidden.");
            }
            else
            {
                if (!history.Showable(new[] { RetrieveSession.CurrentRole }))
                    return StatlogMessageRedirect(map, "Error: This history is can not be unhidden.");
            }

            bool? isActive = CostReportHistoryBL.ChangeHideHistoryEvent(_classification, historyID, RetrieveSession.ARUMUser.Email);

            string message = isActive == true ? "Event Successfully Activated" : "Event Successfully Hidden";
            return StatlogMessageRedirect(map, message);
        }
        #endregion

        #region View Only
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult ShowNotes(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowNotes", "Statlog:View");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (history.ParentHistory != null ||  // TODO: Figure out why the history can't have a parent to view notes
                IsNullOrWhiteSpace(history.Notes))
                return StatlogMessageJson("Error: This history does not have a note to view.");

            var sanitizer = new HtmlSanitizer();
            var myViewModel = new NotesViewModel.View
            {
                Notes = sanitizer.Sanitize(history.Notes),
                InitialEventName = history.Event.IsSystemEvent ? history.ParentHistory?.EventName ?? history.EventName : history.EventName
            };
            return View(myViewModel);
        }

        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult ShowFileInfo(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowFileInfo", "Statlog:View");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            if (!map.History.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (!map.History.HasFileUpload)
                return StatlogMessageJson("Error: This history does not have a file uploaded to it.");

            long.TryParse(map.History.FileSize, out long fileSize);
            DateTime.TryParse(map.History.FileModified, out DateTime modifiedDate);

            var sanitizer = new HtmlSanitizer();
            var myViewModel = new FileInfoViewModel.View
            {
                Size = fileSize,
                FileSizeString = map.History.FileSize,
                FileName = map.History.FileName,
                UploadedBy = map.History.UserName,
                ModifiedDate = modifiedDate,
                Notes = sanitizer.Sanitize(map.History.Notes ?? ""),
                FileFoundInARUM = map.History.FileFoundInARUM,
                InitialEventName = map.History.Event.IsSystemEvent ? map.History.ParentHistory?.EventName ?? map.History.EventName : map.History.EventName
            };

            return View(myViewModel);
        }


        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult ShowNotApplicableInfo(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowNotApplicableInfo", "Statlog:View");

            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            if (!map.History.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            var sanitizer = new HtmlSanitizer();
            var myViewModel = new NotApplicableInfoViewModel.View
            {
                UploadedBy = map.History.UserName,
                Notes = sanitizer.Sanitize(map.History.Notes ?? ""),
                InitialEventName = map.History.Event.IsSystemEvent ? map.History.ParentHistory?.EventName ?? map.History.EventName : map.History.EventName,
            };

            return View(myViewModel);
        }


        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult ShowReviewInfo(string sHistoryID)
        {
            int historyID = ToInternalID(sHistoryID, HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowReviewInfo", "Statlog:View");
            if (map.Error)
                return StatlogMessageJson(map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageJson("Error: This history is not viewable.");

            if (!history.HasLastReview)
                return StatlogMessageJson("Error: This history does not have a review.");

            HistoryDO lastReviewHistory = history.LastReviewHistory;

            string fileType = Empty;
            #region Get File Type

            var applicationFileTypes = _fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "StatLog/SaveFile")?.FileTypes;
            IFileType applicationFileType = applicationFileTypes?.FirstOrDefault(aft => aft.ApplicationFileTypeID == Parse(lastReviewHistory.GetFileType));
            if (applicationFileType != null)
                fileType = applicationFileType.ParentFileTypeName +
                           (applicationFileType.ChildFileTypeName != "N/A" ? " - " + applicationFileType.ChildFileTypeName : Empty);

            #endregion

            string reviewDate = history.LastReviewDate?.ToShortDateString() ?? Empty;
            var sanitizer = new HtmlSanitizer();
            var myViewModel = new ReviewInfoViewModel.View
            {
                ReviewUserName = lastReviewHistory.ReviewUsername,
                ReviewDate = reviewDate,
                FileType = fileType,
                IsReviewOK = lastReviewHistory.IsReviewOkay,
                IsFileReview = !lastReviewHistory.Event.IsSystemEvent ? lastReviewHistory.Event.NeedsFileReview
                    : lastReviewHistory.ParentHistory.Event.NeedsFileReview,
                InitialEventName = !history.Event.IsSystemEvent ? history.EventName
                    : history.ParentHistory.EventName,
                Notes = sanitizer.Sanitize(lastReviewHistory.ReviewNotes)
            };

            return View(myViewModel);
        }

        #endregion

        #region Helper Methods
        private ActionResult StatlogMessageRedirect(int? providerID, int? costReportID, string message)
        {
            if (message.ToUpper().Contains("ERROR"))
                Log.Error(message);

            TempData["CostReportID"] = costReportID;
            TempData["ARUMProviderID"] = providerID;
            TempData["Message"] = message;
            return RedirectToAction("Index", "StatLog");
        }
        private ActionResult StatlogMessageRedirect(Mapping request, string message) =>
            StatlogMessageRedirect(request?.Provider?.ARUMProviderID, request?.StatLog?.CRID, message);

        private ActionResult StatlogMessageJson(string message)
        {
            if (message.ToUpper().Contains("ERROR"))
                Log.Error(message);

            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = new { Error = message }
            };
        }

        private ProcessLifeCycle ProcessLifeCycle => new ProcessLifeCycle(_classification, PortalURL, _arumUserProxy, _fileServiceProxy, _providerProxy);

        private static string PortalURL =>
            new UrlHelper(((MvcHandler)System.Web.HttpContext.Current.Handler).RequestContext).AbsoluteAction("Login", "Home");

        private string CreateUl(List<string> items, string prefix = null, string suffix = null)
        {
            var fullString = "";

            if (!IsNullOrWhiteSpace(prefix))
                fullString = prefix;

            fullString += "<ul>";
            foreach (string item in items)
                fullString += $"<li>{item}</li>";
            fullString += "</ul>";

            if (!IsNullOrWhiteSpace(suffix))
                fullString += suffix;

            return fullString;
        }

        private static bool ContainsSpecialCharacters(string uploadedFileName)
        {
            Regex nonAlphaNum = new Regex(@"[^a-zA-Z0-9.]");
            bool hasNonAlphanumeric = nonAlphaNum.IsMatch(uploadedFileName);
            if (hasNonAlphanumeric) return true; else return false;
        }

        private static bool IsValidFileExtension(string fileName, IFileType applicationFileType)
        {
            if (fileName == null) return false;

            string[] validFileExtensions = applicationFileType?.AllowedFileExtensions?.ToLower().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[] { };
            string fileExtension = Path.GetExtension(fileName);
            fileExtension = fileExtension.Substring(1, fileExtension.Length - 1).ToLower();
            return !IsNullOrWhiteSpace(fileExtension) && validFileExtensions.Any(ext => fileExtension == ext);
        }

        private string AddValuesToReviewText(HistoryDO history, string pEventText, StatLogDTO statLog = null)
        {
            if (IsNullOrWhiteSpace(pEventText))
                return Empty;

            string eventText = pEventText ?? "";
            #region Portal Replacements
            eventText = eventText.Replace("xx_IMGLogo_xx", PortalConfiguration.ImageLogo);
            eventText = eventText.Replace("xx_PortalURL_xx", PortalConfiguration.PortalLoginURL);
            eventText = eventText.Replace("xx_State_xx", PortalConfiguration.Application);
            eventText = eventText.Replace("xx_URL_xx", _classification.URL);
            #endregion
            #region user replacements
            eventText = eventText.Replace("xx_UserFullName_xx", $"{_user.FirstName ?? Empty} {_user.LastName ?? Empty}".Trim());
            eventText = eventText.Replace("xx_UserFirstName_xx", _user.FirstName ?? Empty);
            eventText = eventText.Replace("xx_UserLastName_xx", _user.LastName ?? Empty);
            eventText = eventText.Replace("xx_UserEmail_xx", _user.Email ?? Empty);
            eventText = eventText.Replace("xx_UserRole_xx", new[] { RetrieveSession.CurrentRole }.FirstOrDefault()?.RoleName ?? Empty);

            #endregion
            if (history == null) return eventText;

            #region Statlog/Provider replacements
            statLog = statLog ?? StatLogBL.GetStatLogByStatLogID(_classification, history.CRID);
            if (statLog == null) return eventText;
            eventText = eventText.Replace("xx_BeginDate_xx", statLog.BeginDate.ToShortDateString());
            eventText = eventText.Replace("xx_EndDate_xx", statLog.EndDate.ToShortDateString());
            eventText = eventText.Replace("xx_DueDate_xx", statLog.DueDate.ToShortDateString());

            ProviderInfo myProvider = ProviderBL.GetProviderInfoByARUMUserAndProviderID(_classification, _user, statLog.ARUMProviderID ?? 0);
            if (myProvider == null) return eventText;
            eventText = eventText.Replace("xx_ProviderName_xx", myProvider.Name);
            eventText = eventText.Replace("xx_Identifier_xx", myProvider.Number);
            eventText = eventText.Replace("xx_MCAID_xx", myProvider.MCAID);
            #endregion
            return eventText;
        }

        private Dictionary<int, Guid> CreateTempLinkingIDs(List<int> ids)
        {
            Dictionary<int, Guid> returnMapping = new Dictionary<int, Guid>();
            foreach (int id in ids)
            {
                returnMapping.Add(id, Guid.NewGuid());
            }
            return returnMapping;
        }
#endregion
    }
}
