using ARUMWebServiceApplication;
using Microsoft.EntityFrameworkCore.Internal;
using MSLC.ApplicationProxy;
using MSLC.ApplicationProxy.Interfaces;
using MSLC.ArumProxy.Interfaces;
using MSLC.BulkProcessing.BL;
using MSLC.BulkProcessing.Parameters;
using MSLC.FileProxy.Interfaces;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.ProviderProxy.ARUMProviderService;
using MSLC.RateSetting.Data.Models;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.Controllers;
using Serilog;
using Syncfusion.JavaScript;
using Syncfusion.JavaScript.DataSources;
using Syncfusion.XlsIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using WebPortal_OH_FI.BL;
using WebPortal_OH_FI.BL.CostReport;
using WebPortal_OH_FI.BL.MappedObjects;
using WebPortal_OH_FI.Models;
using WebPortal_OH_FI.Models.ViewModels;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.crdata;
using WebPortal_OH_FI.Reports.BL;
using WebPortal_OH_FI.Web;
using DataTable = System.Data.DataTable;
using MDSReportOptions = WebPortal_OH_FI.App_Start.List.MDSReportOptions;


namespace WebPortal_OH_FI.Controllers
{
    public static class SESSION_KEYS_REPORTS
    {
        //public const string COST_REPORT_YEAR = "CostReportYear";
        //public const string COST_REPORT_TYPE_CODE = "CostReportTypeCode";
        //public const string COST_REPORT_STATUS = "CostReportStatus";
        public const string DW_NEW_COPIED_REPORT_ID = "NewCopiedReportId";
        public const string DW_SELECTED_DATA_POINTS = "SelectedDataPoints";
        //public const string SELECTED_YEAR = "SelectedYear";
        public const string DW_TEMP_REPORT_GUID = "TemporaryReportGuid";
        public const string DW_PROVIDER_LIST = "ProviderList";
    }

    public class ReportsController : LoggedInBaseController
    {
        #region Properities
        private static List<MDSReportOptions.ReportOptions> _mdsReportOptions;
        private readonly MSLC.ProviderProxy.ARUMProviderProxy _providerServiceProxy;
        private readonly ARUMApplicationServiceProxy _applicationServiceProxy;

        private static string SimplifiedType =>
            Common.Extensions.ClassificationDTOExtensions.GetSimplifiedType(RetrieveSession.Classification);
        #endregion

        public ReportsController()
        {
            _providerServiceProxy = MSLC.Portal.Utility.Proxies.ARUMProvider.GetARUMProviderServiceProxy(RetrieveSession.Classification);
            _applicationServiceProxy = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);
            _mdsReportOptions = MDSReportOptions.GetReportOptionsForUserRoles(RetrieveSession.Classification, RetrieveSession.ARUMUser.Roles);
        }

        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Reports:UploadDownload")]
        public ActionResult StatLogUploadDownloadReport()
        {
            var viewModel = new StatLogUploadDownloadReport
            {
                ProviderIdentifiers = GetProviderIdentifiersSelectList()
            };
            return View(viewModel);
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

        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Reports:MassUpload")]
        public ActionResult StatLogMassUploadReport()
        {
            var viewModel = new StatLogMassUploadReport
            {
                ProviderIdentifiers = GetProviderIdentifiersSelectList()
            };
            return View(viewModel);
        }

        [HttpGet]
        [ARUMFeatureActionFilterAttribute("Reports:CostReportSubmissionMonitoring")]
        public ActionResult CRSubmissionMonitoringReport()
        {
            var viewModel = new CRSMonitoringReport
            {

            };
            return View(viewModel);
        }

        [HttpGet]
        [ARUMFeatureActionFilterAttribute("Reports:ARAP")]
        public ActionResult ARAPReport()
        {
            var viewModel = new ARAPReportViewModel.View();
            return View(viewModel);
        }


        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:UploadDownload")]
        public JsonResult GenerateStatLogUploadDownloadReport(DateTime beginDate, DateTime endDate, string[] providerIdentifiers)
        {
            var myUser = RetrieveSession.ARUMUser;
            try
            {
                DataTable dataTable = StatLogReportsBL.GenerateStatLogUploadDownloadReport(beginDate, endDate, providerIdentifiers);

                var fileGuid = Guid.NewGuid();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}"] = CreateExcelFile(dataTable, "Upload Reviews").ToArray();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}:fileName"] = "StatlogReport-" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".xlsx";

                return Json(new
                {
                    Success = true,
                    id = fileGuid
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MassUpload")]
        public JsonResult GenerateStatLogMassUploadReport(DateTime beginDate, DateTime endDate, string[] providerIdentifiers)
        {
            var myUser = RetrieveSession.ARUMUser;
            try
            {
                var arumFileProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);

                var filesTypes = arumFileProxy.GetApplicationFileTypesByApplicationID(myUser.UserID, "ReportsController/GenerateStatLogMassUploadReport").FileTypes;

                DataTable dataTable = StatLogReportsBL.GenerateStatLogMassUploadReport(beginDate, endDate, providerIdentifiers, filesTypes);

                if (!dataTable.Rows.Any())
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Report is empty, please try a different date range."
                    }, JsonRequestBehavior.AllowGet);
                }

                var fileGuid = Guid.NewGuid();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}"] = CreateExcelFile(dataTable, "Mass Uploads").ToArray();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}:fileName"] = "StatlogReport-" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".xlsx";

                return Json(new
                {
                    Success = true,
                    id = fileGuid
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:CostReportSubmissionMonitoring")]
        public ActionResult GenerateCRSubmissionMonitoringReport(DateTime beginDate, DateTime endDate, bool includeFirstAndLastName)
        {
            var myUser = RetrieveSession.ARUMUser;
            try
            {
                DataTable dataTable = CRSMonitoringReportBL.GenerateCRSMonitoringReport(beginDate, endDate, includeFirstAndLastName);

                var fileGuid = Guid.NewGuid();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}"] = CreateExcelFile(dataTable, "Cost Report Submission Monitoring Report").ToArray();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}:fileName"] = "Cost Report Submission Monitoring Report-" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".xlsx";

                return Json(new
                {
                    Success = true,
                    id = fileGuid
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:ARAP")]
        public ActionResult GenerateARAPReport()
        {
            var myUser = RetrieveSession.ARUMUser;
            try
            {
                byte[] file = ARAPReportBL.GenerateARAPReport();

                var fileGuid = Guid.NewGuid();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}"] = file;
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}:fileName"] =
                    $"ARAP_{SimplifiedType} Report - {DateTime.Now:yyyy-dd-M--HH-mm-ss}.xlsm";

                return Json(new
                {
                    Success = true,
                    id = fileGuid
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        #region MDS Report
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public ActionResult MDSReport()
        {
            var myUser = RetrieveSession.ARUMUser;

            var arumFileProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);

            var appFilesTypes = arumFileProxy
                .GetApplicationFileTypesByApplicationID(myUser.UserID, "Reports/MDSReport").FileTypes;

            var hidePeriod = !UploadDownloadConfiguration.EnablePeriod;
            var hideVersion = !UploadDownloadConfiguration.EnableVersion;

            var message = string.Empty;
            int fileTypeId = 0;
            int fileSubTypeId = 0;
            List<SelectListItem> fileTypes = new List<SelectListItem>();

            if (appFilesTypes != null && appFilesTypes.Any())
            {
                if (appFilesTypes.Length == 1)
                {
                    if (appFilesTypes[0].ParentFileTypeID.HasValue)
                    {
                        fileTypeId = appFilesTypes[0].ParentFileTypeID.Value;
                    }
                    if (appFilesTypes[0].ChildFileTypeID.HasValue)
                    {
                        fileSubTypeId = appFilesTypes[0].ChildFileTypeID.Value;
                    }
                }

                fileTypes = ReturnBulkUploadFileParentNames(appFilesTypes);
            }
            else
            {
                message = "No file types setup for application.";
            }

            var applicationPeriods = new List<SelectListItem>();

            if (!hidePeriod)
            {
                var appPeriods = arumFileProxy.GetApplicationPeriodsByApplicationID(myUser.UserID, "Upload/Index").Periods;
                if (appPeriods != null && appPeriods.Any())
                {
                    applicationPeriods =
                        appPeriods.Where(n => n.IsExternal).Select(n => new SelectListItem
                        {
                            Text = !string.IsNullOrEmpty(n.DisplayName) ? n.DisplayName : n.PeriodText,
                            Value = n.ApplicationPeriodID.ToString()
                        }).ToList();
                }
                else
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        message += "<br>";
                    }
                    message += "No periods setup for application.";
                }
            }

            var providerList = ProviderBL.GetAllARUMProviderInfoByARUMUserID(RetrieveSession.Classification, RetrieveSession.ARUMUser, "Statlog:View")
                .Select(p => new MDSReport.ProviderSelection
                {
                    Name = p.Name,
                    MCAID = p.MCAID,
                    Number = p.Number,
                    ARUMProviderID = ClientMapping.MapInternalIdToClientId(p.ARUMProviderID.ToString(), "MDSReportProviderID"),
                    Selected = false
                }).ToList();

            if (!string.IsNullOrEmpty(message))
            {
                message += "<br/>  Please contact the support help desk.";
                TempData["Message"] = message;
            }

            var viewModel = new MDSReport.View
            {
                ApplicationPeriods = applicationPeriods,
                UserId = myUser.UserID,

                EndingDate = DateTime.Now,
                ReportPeriod = Models.ViewModels.MDSReport.ReportPeriodOptions.Weekly,
                ReportType = Models.ViewModels.MDSReport.ReportTypeOptions.Annual,
                providerList = providerList
            };

            return View(viewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public JsonResult ReturnFileSubTypes(int parentId)
        {
            var fileSubTypes = new List<SelectListItem>();
            var _arumFileProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
            var fileTypes = _arumFileProxy.GetApplicationFileTypesByApplicationID(RetrieveSession.ARUMUser.UserID, "Upload / ReturnFileSubTypes");
            var parentFileTypes = fileTypes.FileTypes.Where(n => n.ParentFileTypeID == parentId && n.IsExternal).ToList();
            fileSubTypes = parentFileTypes.Select(n => new SelectListItem()
            {
                Text = (!n.ChildFileTypeID.HasValue) ? "N/A" : (string.IsNullOrWhiteSpace(n.DisplayName)) ? n.ChildFileTypeName : n.DisplayName,
                Value = (!n.ChildFileTypeID.HasValue) ? "0" : n.ChildFileTypeID.ToString(),
            }).ToList();
            return Json(fileSubTypes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public JsonResult ReturnReportingPeriods()
        {
            var reportingPeriods = _mdsReportOptions
		        // .Where(o => o.Period.Equals("Weekly")) // Right now we are limiting the reports to weekly.
		        .GroupBy(o => o.Period)
		        .Select(o => new SelectListItem
		        {
			        Text = o.Key.ToString(),
			        Value = o.Key.ToString(),
		        }).ToList();
	        return Json(reportingPeriods, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public JsonResult ReturnReportingExtensionTypes(string period, string type)
        {
            var reportType = _mdsReportOptions
                .FirstOrDefault(rp => rp.Period == period && $"{rp.Version} - {rp.Name}" == type);
            var extensionTypes = reportType?.ExtensionTypes
		        .Select(extensionType => new SelectListItem
		        {
			        Text = extensionType,
			        Value = extensionType
				}).ToList();
            var singleSelectProvider = reportType?.SingleSelect;
	        return Json(new { ExtensionTypes = extensionTypes, SingleSelect = singleSelectProvider}, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public JsonResult ReturnReportingRateVersions(string period)
        {
            var reportRateVersions = _mdsReportOptions
                .Where(x => x.Period == period)
                .GroupBy(y => y.RateVersions)
                .SelectMany(z => z.Key)
                .Distinct()
                .Select(a => new SelectListItem()
                {
                    Text = a,
                    Value = a
                }).ToList();

            return Json(reportRateVersions, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public JsonResult ReturnReportingTypes(string period, string rateVersion)
        {
            var reportingPeriods = _mdsReportOptions
                // .Where(o => o.Period.Equals("Weekly")) // Right now we are limiting the reports to weekly.
                .Where(o => o.Period.Equals(period) && o.RateVersions.Contains(rateVersion))
                .Select(o => new SelectListItem
                {
                    Text = $"{o.Version} - {o.Name}",
                    Value = $"{o.Version} - {o.Name}",
                }).ToList();
            return Json(reportingPeriods, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public ActionResult GenerateMDSReport(MDSReport.Save myViewModel)
        {
            try
            {
                string reportFileName = "";
                string arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);
                var providerIDs = myViewModel.ProviderIDListString?.Split(',')
                    .Select(idString => int.TryParse(ClientMapping.GetInternalIdFromClientId(idString, "MDSReportProviderID"), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToArray() ?? new int[] { };

                if (!providerIDs.Any())
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Provider list can not be empty."
                    }, JsonRequestBehavior.AllowGet);
                }


                #region Validate selected extenion types
                var reportOption = _mdsReportOptions.FirstOrDefault(rp => rp.Period == myViewModel.ReportPeriod && $"{rp.Version} - {rp.Name}" == myViewModel.ReportType);
				if (reportOption == null)
				{
					return Json(new
					{
						Success = false, 
			            Message = "Could not validate selected report options."
		            }, JsonRequestBehavior.AllowGet);
	            }
                
                // Keep this code until we decide wether or not multiple extention types will be generated. 4/19/2023 - Adam Holbert
                // if (myViewModel.ExtensionTypes.Any(selectedExtension =>
                //      !reportOption.ExtensionTypes.Contains(selectedExtension)))
                if (!reportOption.ExtensionTypes.Contains(myViewModel.ReportExtension))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid selected extension for the given report options."
                    }, JsonRequestBehavior.AllowGet);
                }

                //Validate that for single provider reports only one provider was selected
                if (providerIDs.Count() > 1 && reportOption.SingleSelect)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Only one provider can be selected for this report type."
                    }, JsonRequestBehavior.AllowGet);
                }

                #endregion

                JobBL.QueueJob(arumConnectionString, new CreateJobParameters
                {
                    Name = $"{reportFileName}",
                    FileGuid = Guid.Empty,
                    JobName = "Generate MDS Report",
                    JobType = "GenerateMDSReport",

                    ItemType = "Request",
                    Status = "Created",

                    ItemAttributes = new Dictionary<string, string>
                    {
                        {"ApplicationPeriodId", myViewModel.ApplicationPeriodId.ToString()},
                        {"EndingDate", myViewModel.EndingDate.ToString("MM/dd/yyyy")},
                        {"ReportPeriod", myViewModel.ReportPeriod.ToString()},
                        {"ReportType", myViewModel.ReportType.ToString()},
                        {"ProviderIDs", providerIDs.Select(id => id.ToString()).Join(",")},
                        {"ReportExtension", myViewModel.ReportExtension},
                        {"ReportRateVersion", myViewModel.RateVersion}
                       
                    },
                    Username = RetrieveSession.ARUMUser.Email,
                    Messages = new List<string> { "MDS Report Generation requested." }
                });

                TempData["Message"] = "MDS Report Generation Successfully Requested.";

                return Json(new
                {
                    Success = true,
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Returns UploadParametersModel.  Every parameter required to use the sync fusion control.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="parentId"></param>
        /// <param name="childId"></param>
        /// <returns></returns>
        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public JsonResult ReturnExtensionAndFileSize(int parentId, int childId)
        {
            var _arumFileProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
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
        #endregion

        #region Section Q Report
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Reports:SectionQ")]
        public ActionResult SectionQReport()
        {
            var viewModel = new SectionQReportViewModel.View
            {
                EndingDate = DateTime.Now,
                CLSProvGroupingList = SectionQReportBL.CLSProviderDict.Select(kvp => new SelectListItem { Value = kvp.Key, Text = kvp.Value }).ToList(),
                //CLSProvGrouping = "ALL",
                ReportTypeList = SectionQReportBL.ReportTypesDict.Select(kvp => new SelectListItem { Value = kvp.Key, Text = kvp.Value }).ToList(),
                ReportVersionList = SectionQReportBL.ReportVersionDict.Select(kvp => new SelectListItem { Value = kvp.Key, Text = kvp.Value }).ToList(),
            };

            return View(viewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:SectionQ")]
        public ActionResult GenerateSectionQReport(SectionQReportViewModel.Save myViewModel)
        {
            try
            {
                string arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);

                #region Validate selected extenion types
                if (!SectionQReportBL.ReportVersionDict.ContainsKey(myViewModel.ReportVersion))
                    return Json(new { Success = false, Message = "Could not validate selected Report Version." }, JsonRequestBehavior.AllowGet);

                bool isSectionQPlus = myViewModel.ReportVersion.Equals("SectionQ+");

                if (!isSectionQPlus) // Report Type and CLS Provider grouping are not needed for SectionQPlus
                {
                    if (!SectionQReportBL.ReportTypesDict.ContainsKey(myViewModel.ReportType))
                        return Json(new { Success = false, Message = "Could not validate selected Report Type." }, JsonRequestBehavior.AllowGet);

                    if (!SectionQReportBL.CLSProviderDict.ContainsKey(myViewModel.CLSProvGrouping))
                        return Json(new { Success = false, Message = "Could not validate selected CLS Provider Grouping." }, JsonRequestBehavior.AllowGet);
                }

                #endregion

                JobBL.QueueJob(arumConnectionString, new CreateJobParameters
                {
                    Name = $"{myViewModel.ReportVersion} Report",
                    FileGuid = Guid.Empty,
                    JobName = "Generate Section Q Report",
                    JobType = "GenerateSectionQReport",

                    ItemType = "Request",
                    Status = "Created",

                    ItemAttributes = new Dictionary<string, string>
                    {
                        {"ReportVersion", myViewModel.ReportVersion},
                        {"ReportType", myViewModel.ReportType},
                        {"CLSProvGrouping", myViewModel.CLSProvGrouping},
                        {"EndingDate", myViewModel.EndingDate.ToString("MM/dd/yyyy")},
                    },
                    Username = RetrieveSession.ARUMUser.Email,
                    Messages = new List<string> { "Section Q Report Generation requested." }
                });

                TempData["Message"] = "Section Q Report Generation Successfully Requested.";

                return Json(new
                {
                    Success = true,
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region MDS Resident Report
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Reports:MDSResidentListing")]
        public ActionResult MDSResidentListingReport()
        {
            var myUser = RetrieveSession.ARUMUser;

            var message = string.Empty;

            var providerList = _applicationServiceProxy.GetAllActiveProvidersFromListWithParameters(RetrieveSession.Classification.ARUMToken, myUser.Providers.Select((IARUMUserProviders p) => p.ProviderID).ToList(), new List<ARUMWebServiceApplication.WebParameter>
            {
                new ARUMWebServiceApplication.WebParameter
                {
                    Key = "includeIdentifiers",
                    Value = "true",
                    DataType = "bool"
                },
                new ARUMWebServiceApplication.WebParameter
                {
                    Key = "includeEffectiveDates",
                    Value = "true",
                    DataType = "bool"
                }
            }, "Statlog.ProviderBL.GetAllARUMProviderInfoByARUMUserID")
            .lstProviders
            .Select(p => new MDSResidentListingReport.ProviderSelection
            {
                Name = p.Name,
                MCAID = p.MCAID,
                MedicareID = p.Identifiers.FirstOrDefault(i => i.Type == "MEDICAREID")?.Value,
                NPI = p.Identifiers.FirstOrDefault(i => i.Type == "NPI")?.Value,
                ARUMProviderID = ClientMapping.MapInternalIdToClientId(p.ProviderID.ToString(), "MDSResidentReportProviderID"),
                Selected = false
            }).ToList();

            if (!string.IsNullOrEmpty(message))
            {
                message += "<br/>  Please contact the support help desk.";
                TempData["Message"] = message;
            }

            var viewModel = new MDSResidentListingReport.View
            {
                UserId = myUser.UserID,
                providerList = providerList
            };

            return View(viewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Reports:MDSResidentListing")]
        public ActionResult GenerateMDSResidentListingReport(MDSResidentListingReport.Save myViewModel)
        {
            try
            {
                string reportFileName = $"MDSResidentList_{DateTime.Now.ToString("MMddyyyy")}";
                string arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);
                var providerIDs = myViewModel.ProviderIDListString?.Split(',')
                    .Select(idString => int.TryParse(ClientMapping.GetInternalIdFromClientId(idString, "MDSResidentReportProviderID"), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToArray() ?? new int[] { };

                if (!providerIDs.Any())
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Provider list can not be empty."
                    }, JsonRequestBehavior.AllowGet);
                }

                JobBL.QueueJob(arumConnectionString, new CreateJobParameters
                {
                    Name = $"{reportFileName}",
                    FileGuid = Guid.Empty,
                    JobName = "Generate MDS Resident Listing Report",
                    JobType = "GenerateMDSResidentListingReport",

                    ItemType = "Request",
                    Status = "Created",

                    ItemAttributes = new Dictionary<string, string>
                    {
                        {"EndingDate", DateTime.Now.ToString("MM/dd/yyyy")},
                        {"ReportType", "MDS Resident Listing Report"},
                        {"ProviderIDs", providerIDs.Select(id => id.ToString()).Join(",")}
                    },
                    Username = RetrieveSession.ARUMUser.Email,
                    Messages = new List<string> { "MDS Resident Listing Report Generation requested." }
                });

                TempData["Message"] = "MDS Resident Listing Report Generation Successfully Requested.";

                return Json(new
                {
                    Success = true,
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        [HttpGet]
        // [ARUMFeatureActionFilterAttribute("")] Many features use this endpoint
        public ActionResult DownloadReport(Guid id)
        {
            try
            {
                var myUser = RetrieveSession.ARUMUser;

                var file = (byte[])Session[$"{myUser.LastName}_{myUser.FirstName}:{id}"];
                Session[$"{myUser.LastName}_{myUser.FirstName}:{id}"] = null;
                var filename = (string)Session[$"{myUser.LastName}_{myUser.FirstName}:{id}:fileName"];
                Session[$"{myUser.LastName}_{myUser.FirstName}:{id}:fileName"] = null;

                return File(file, "text/plain", filename);
            }
            catch (Exception)
            {
                return new EmptyResult();
            }
        }

        #region Cost Report Data Wizard
        public ActionResult CostReportDataWizard()
        {
            try
            {
                List<SelectListItem> yearsCR = new List<SelectListItem>();
                List<SelectListItem> crTypes = new List<SelectListItem>();
                List<SelectListItem> crStatus = new List<SelectListItem>();
                CostReportDataWizardViewModel costReportDataWizardViewModel = new CostReportDataWizardViewModel();
                List<DataPointsReportsViewModel> lstsavedSearches = new List<DataPointsReportsViewModel>();
                List<DataPointReports> dataPointsProviderData = new List<DataPointReports>();

                // Check for and Delete Temp Providers 
                ReportBL.DeleteAllTempDataWizardData();

                var crYears = RateBL.GetCostReportYears().OrderBy(x => x);
                foreach (var year in crYears)
                {
                    yearsCR.Add(new SelectListItem {
                        Text = year.ToString(),
                        Value = year.ToString()
                        //Value = ClientMapping.MapInternalIdToClientId(year.ToString(), "costReportYear"),
                    });
                }
                costReportDataWizardViewModel.CostReportYears = yearsCR;

                costReportDataWizardViewModel.CostReportLengths = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "All", Value = "All" },
                    new SelectListItem() { Text = "12", Value = "12" }
                };
                var costReportTypes = RateBL.GetCostReportType();
                crTypes.Add(new SelectListItem
                { Value = "All", Text = "All" });

                foreach (var reportType in costReportTypes)
                {
                    crTypes.Add(new SelectListItem
                    {
                        Text = reportType.CostReportTypeCode + " " + reportType.Name,
                        Value = reportType.CostReportTypeCode 
                    });
                }

                costReportDataWizardViewModel.CostReportTypes = crTypes;

                crStatus = new List<SelectListItem>
                    {
                        new SelectListItem{ Text = "All", Value = "All" },
                        new SelectListItem{ Text = "Cleared", Value = "Cleared" },
                        new SelectListItem{ Text = "On-Hold", Value = "On-Hold" },
                        new SelectListItem{ Text = "Obsolete", Value = "Obsolete" }
                    };
                costReportDataWizardViewModel.CostReportStatuses = crStatus;

                dataPointsProviderData = ReportBL.GetSavedDataPointProviders();

                foreach (DataPointReports reportData in dataPointsProviderData)
                {
                    DateTimeOffset? dateTimeOffset = reportData.CreatedOn;
                    lstsavedSearches.Add(new DataPointsReportsViewModel
                    {
                        ReportId = ClientMapping.MapInternalIdToClientId(reportData.ReportId.ToString(), "savedsearch"),
                        ReportName = reportData.ReportName,
                        CRYear = reportData.CRYear,
                        CRLength = reportData.CRLength ?? "All",
                        CRType = reportData.CRType + (string.IsNullOrWhiteSpace(ReportBL.GetCostReportTypeName(reportData.CRType)) ? "" : " " + ReportBL.GetCostReportTypeName(reportData.CRType)),
                        CRStatus = reportData.CRStatus,
                        Creator = reportData.CreatedBy,
                        DateCreated = dateTimeOffset.Value.Date.ToShortDateString(),

                    });
                }

                costReportDataWizardViewModel.DataPointsReportsViewModel = lstsavedSearches;
                return View(costReportDataWizardViewModel);
            }
            catch (Exception ex)
            {
                if (!(ex is ThreadAbortException))
                {
                    var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                    Log.Error(ex, exMessage);
                }
                return Json(new { Success = false, Message = "Error occurred importing the file. Please check that headers are correct and this is the correct file type." }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetSelectedProviders(string costReportYear, string costReportLength, string costReportType, string costReportStatus, string reportName, bool isExisting)
        {
            CostReportDataWizardViewModel costReportDataWizardViewModel = new CostReportDataWizardViewModel();
            //string costReportTypeCode = string.Empty;
            string costReportTypeYear = string.Empty;
            List<int> selectedProviders = new List<int>();
            //costReportTypeCode = ClientMapping.GetInternalIdFromClientId(costReportType, "costReportType");
            costReportTypeYear = ClientMapping.GetInternalIdFromClientId(costReportYear, "costReportYear");
            int.TryParse(costReportTypeYear, out int crYear);
            Session[SESSION_KEYS_REPORTS.DW_PROVIDER_LIST] = null;
            //Session[SESSION_KEYS_REPORTS.COST_REPORT_YEAR] = crYear;
            //Session[SESSION_KEYS_REPORTS.COST_REPORT_TYPE_CODE] = costReportType;
            //Session[SESSION_KEYS_REPORTS.COST_REPORT_STATUS] = costReportStatus;

            if (costReportType != string.Empty)
            {
                var myUser = RetrieveSession.ARUMUser;
                //Include all providers whether active or closed
                bool? activeState = null;
                List<DataWizardProviders> providerList = new List<DataWizardProviders>();
               // List<int> providerIds
                providerList = RateBL.GetDataWizardReportProviderIds(crYear, costReportLength, costReportType, costReportStatus);
                if (isExisting)
                {
                    if (Session[SESSION_KEYS_REPORTS.DW_NEW_COPIED_REPORT_ID] != null) // Existing
                    {
                        int copiedReportId = (int)Session[SESSION_KEYS_REPORTS.DW_NEW_COPIED_REPORT_ID];
                        selectedProviders = ReportBL.GetSelectedProviders(copiedReportId);// 2012
                    }
                }
                else // New
                {
                    Session[SESSION_KEYS_REPORTS.DW_SELECTED_DATA_POINTS] = null;
                    DeleteEditingReport();
                    // For saving use - Full string
                    // For ARUM use - Type code
                    SaveDataPointReports(crYear, costReportLength, costReportType, costReportStatus, reportName);
                }

                if (providerList.Count > 0)
                {
                    Session[SESSION_KEYS_REPORTS.DW_PROVIDER_LIST] = providerList;
                    List<int> providerIds = providerList.Select(x => x.ProviderId).ToList();

                    MSLC.ApplicationProxy.Interfaces.IAllProviders providers = _applicationServiceProxy.GetAllActiveProvidersFromListWithOptions(RetrieveSession.Classification.ARUMToken,
                    providerIds, false, true, false, true, false, false, false, "ratesetting.providers");

                    var providersViewModel = providers.lstProviders
                        .Where(x => SelectByProviderEffectiveDate(activeState, x.EffectiveDates)
                               && x.IsActive == true)
                        .Select(x => new DataWizardProviderViewModel
                        {
                            ProviderValue = ClientMapping.MapInternalIdToClientId(x.ProviderID.ToString(), "provider"),
                            ProviderName = x.Name,
                            IsSelected = selectedProviders.Contains(x.ProviderID),
                        })
                        .OrderBy(p => p.ProviderName)
                        .ToList();

                    var selectedProvidersViewModel = providers.lstProviders
                        .Where(x => SelectByProviderEffectiveDate(activeState, x.EffectiveDates) && selectedProviders.Contains(x.ProviderID))
                        .Select(x => new DataWizardProviderViewModel
                        {
                            ProviderValue = ClientMapping.MapInternalIdToClientId(x.ProviderID.ToString(), "provider"),
                            ProviderName = x.Name,
                            IsSelected = selectedProviders.Contains(x.ProviderID),
                        })
                        .OrderBy(p => p.ProviderName)
                        .ToList();

                    costReportDataWizardViewModel.Providers = providersViewModel;
                    costReportDataWizardViewModel.SelectedProviders = selectedProvidersViewModel;
                }
                else
                {
                    // Grid needs an empty object to re-bind and show message - "No records to display". Otherwise throws binding error.
                    costReportDataWizardViewModel.Providers = new List<DataWizardProviderViewModel>();
                }
            }
            return Json(costReportDataWizardViewModel, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDataPoints(string selectedYear)
        {
            string wsName = string.Empty;
            bool isChecked = false;
            List<DataPointsViewModel> listDataPoints = new List<DataPointsViewModel>();
            List<DataPointHeaderText> fieldNamesColumnTwo = new List<DataPointHeaderText>();
            List<FieldTemplate> fieldTemplatesDataPoints = new List<FieldTemplate>();
            List<DataWizardProviders> providerList = new List<DataWizardProviders>();
            try
            {
                //var schedules = ScheduleBL.GetScheduleList(null, "schedule");
                var workSheetNames = ScheduleBL.GetWorksheetName();
                List<string> selectedDataPoints = null;
                if (Session[SESSION_KEYS_REPORTS.DW_SELECTED_DATA_POINTS] != null)
                {
                    selectedDataPoints = (List<string>)Session[SESSION_KEYS_REPORTS.DW_SELECTED_DATA_POINTS];
                }
                // Get the saved provider list from the session.
                if (Session[SESSION_KEYS_REPORTS.DW_PROVIDER_LIST] != null)
                {
                    providerList = (List<DataWizardProviders>)Session[SESSION_KEYS_REPORTS.DW_PROVIDER_LIST];
                }
                int i = 1;
                foreach (var workSheetName in workSheetNames)
                {
                    int displayLine = 1;
                    bool lineFlag = false;
                    wsName = workSheetName.Text;

                    // Get Field Names and Data Points
                    fieldNamesColumnTwo = ReportBL.GetColumnHeaders(wsName, selectedYear);
                    fieldTemplatesDataPoints = ReportBL.GetScheduleDataPoints(wsName, selectedYear, providerList);

                    if (fieldNamesColumnTwo.Count == 0 && fieldTemplatesDataPoints.Count == 0) continue;

                    // 1. Parent - Schedule Name - B-1
                    DataPointsViewModel parent = new DataPointsViewModel()
                    {
                        ID = workSheetName.Text.Replace(" ", "") + "_" + workSheetName.Value,
                        //This conditional decreases amount of changes required elsewhere if the worksheetName.Text seed value was changed
                        Text = ScheduleBL.ScheduleNameFromUISelector(workSheetName.Text),
                        parentID = string.Empty,
                        DisplayColumn = 0,
                        hasChild = true,
                        ScheduleName = wsName
                    };
                    listDataPoints.Add(parent);

                    // Assign boolean value to lineFlag variable based on SchedulesWithRepeatingLines
                    lineFlag = SchedulesWithRepeatingLines(wsName);

                    if (!lineFlag)
                    {
                        if (wsName == "D-1")
                        {
                            var d1FieldTemplate = fieldTemplatesDataPoints
                                .Where(x => x.DisplayLine == 0 && x.DisplayColumn == 1)
                                .FirstOrDefault();
                            if (d1FieldTemplate != null)
                            {
                                isChecked = false;
                                if (selectedDataPoints != null && selectedDataPoints.Contains(d1FieldTemplate.RangeAddress))
                                {
                                    isChecked = true;
                                }

                                DataPointsViewModel d1ViewModel = new DataPointsViewModel()
                                {
                                    ID = parent.ID.Replace(" ", "") + "_" + d1FieldTemplate.DisplayLine,
                                    parentID = parent.ID,
                                    Text = d1FieldTemplate.FieldName,
                                    RangeName = d1FieldTemplate.RangeAddress,
                                    isChecked = isChecked,
                                    hasChild = false,
                                    DisplayColumn = d1FieldTemplate.DisplayColumn,
                                    ScheduleName = wsName
                                };

                                listDataPoints.Add(d1ViewModel);
                            }
                        }
                        // 2. Child - Field Name 
                        if (fieldNamesColumnTwo.Count > 0)
                        {
                            foreach (var fieldName in fieldNamesColumnTwo)
                            {
                                DataPointsViewModel viewModel1 = new DataPointsViewModel();
                                int displayCol = fieldName.DisplayColumn;
                                i++;

                                viewModel1.ID = parent.ID.Replace(" ", "") + "_" + fieldName.DisplayColumn;
                                viewModel1.parentID = parent.ID;
                                viewModel1.Text = fieldName.HeaderText;//FieldName;
                                viewModel1.hasChild = true;
                                viewModel1.DisplayColumn = displayCol;
                                viewModel1.ScheduleName = wsName;
                                listDataPoints.Add(viewModel1);

                                // 3. GrandChild - Reference - B1_1_2
                                var dataPointsGroup = fieldTemplatesDataPoints.Where(x => x.DisplayColumn == displayCol).ToList();
                                foreach (var fieldTemplates in dataPointsGroup)
                                {
                                    isChecked = false;
                                    if (fieldTemplates.Reference != null)
                                    {
                                        if (selectedDataPoints != null && selectedDataPoints.Contains(fieldTemplates.RangeAddress))
                                        {
                                            isChecked = true;
                                        }
                                        i++;
                                        if (GetScheduleswithChartofAccount().Contains(wsName))
                                        {
                                            DataPointsViewModel viewModelCOA = new DataPointsViewModel()
                                            {
                                                ID = viewModel1.ID.Replace(" ", "") + "_" + fieldTemplates.DisplayLine,
                                                parentID = viewModel1.ID,
                                                Text = fieldTemplates.FieldName,
                                                RangeName = fieldTemplates.RangeAddress,
                                                isChecked = isChecked,
                                                hasChild = false,
                                                DisplayColumn = fieldTemplates.DisplayColumn,
                                                ScheduleName = wsName
                                            };
                                            listDataPoints.Add(viewModelCOA);
                                        }
                                        else
                                        {
                                            DataPointsViewModel viewModel = new DataPointsViewModel()
                                            {
                                                ID = viewModel1.ID.Replace(" ", "") + "_" + fieldTemplates.DisplayLine,
                                                parentID = viewModel1.ID,
                                                Text = fieldTemplates.FieldName + "-" + fieldTemplates.RangeAddress,
                                                RangeName = fieldTemplates.RangeAddress,
                                                isChecked = isChecked,
                                                hasChild = false,
                                                DisplayColumn = fieldTemplates.DisplayColumn,
                                                ScheduleName = wsName
                                            };
                                            listDataPoints.Add(viewModel);
                                        }
                                    }
                                }
                                displayCol++;
                            }
                        }
                    }
                    else
                    {
                        // 2. GrandChild - Reference - B1_1_2
                        foreach (var fieldTemplates in fieldTemplatesDataPoints)
                        {
                            isChecked = false;
                            if (selectedDataPoints != null && selectedDataPoints.Contains(fieldTemplates.RangeAddress))
                            {
                                isChecked = true;
                            }
                            i++;
                            DataPointsViewModel viewModel = new DataPointsViewModel()
                            {
                                ID = parent.ID.Replace(" ", "") + "_" + displayLine,
                                parentID = parent.ID,
                                Text = fieldTemplates.FieldName + "-" + fieldTemplates.RangeAddress,
                                RangeName = fieldTemplates.RangeAddress,
                                isChecked = isChecked,
                                hasChild = false,
                                DisplayColumn = fieldTemplates.DisplayColumn,
                                ScheduleName = wsName
                            };
                            listDataPoints.Add(viewModel);
                            displayLine++;
                        }
                    }
                    i++;
                }
                return Json(listDataPoints, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetSelectedDataPointsTreeData(string savedReportID, string costReportYear)
        {
            if (!int.TryParse(ClientMapping.GetInternalIdFromClientId(savedReportID, "savedsearch"), out int dataPointReportId))
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
       
            //string costReportTypeYear = string.Empty;
            //costReportTypeYear = ClientMapping.GetInternalIdFromClientId(costReportYear, "costReportYear");
           
            List<SelectListItem> workSheetNames_all = ScheduleBL.GetWorksheetName();

            List<DataPointReportFields> selectedFields = ReportBL.GetAllSelectedDataPoints(dataPointReportId);// 6069
            List<string> selectedworksheetNames = selectedFields.Select(x => x.WorksheetName).Distinct().OrderBy(x => x).ToList();
            List<SelectListItem> ordered_selectedworksheetNames = new List<SelectListItem>();
            List<DataWizardProviders> providerList = new List<DataWizardProviders>();
            if (Session[SESSION_KEYS_REPORTS.DW_PROVIDER_LIST] != null)
            {
                providerList = (List<DataWizardProviders>)Session[SESSION_KEYS_REPORTS.DW_PROVIDER_LIST];
            }
            bool lineFlag = false;

            foreach (var workSheetName in workSheetNames_all)
            {
                var matched_WorksheetName = selectedworksheetNames.Find(x => x == workSheetName.Text);
                {
                    if (!string.IsNullOrWhiteSpace(matched_WorksheetName))
                    {
                        ordered_selectedworksheetNames.Add(workSheetName);
                    }
                }
            }

            List<SelectedDataPointsTreeViewModel> listDataPoints = new List<SelectedDataPointsTreeViewModel>();

            //TODO: This should load the document template and field templates and CACHE THEM instead of re-pulling
            //      for EVERY field template. This would vastly improve the performance.
           
            int scheduleSequenceId = 1;
            foreach (var worksheetName in ordered_selectedworksheetNames)
            {
                SelectedDataPointsTreeViewModel parent = new SelectedDataPointsTreeViewModel() // WorksheetName
                {
                    sequenceId = scheduleSequenceId,
                    id = worksheetName.Text.Replace(" ", "") + "_" + worksheetName.Value,
                    parentId = string.Empty, 
                    //This conditional decreases amount of changes required elsewhere if the worksheetName.Text seed value was changed
                    text = ScheduleBL.ScheduleNameFromUISelector(worksheetName.Text),
                    hasChild = true,
                    ischecked = true,

                };

                listDataPoints.Add(parent);
                scheduleSequenceId++;

                var columnHeaders = ReportBL.GetColumnHeaders(worksheetName.Text, costReportYear);
                var fieldTemplatesDataPoints = ReportBL.GetScheduleDataPoints(worksheetName.Text, costReportYear, providerList);
                var worksheet_RangeAddress = selectedFields.Where(x => x.WorksheetName == worksheetName.Text).ToList();

                //************
                lineFlag = SchedulesWithRepeatingLines(worksheetName.Text);
                //************

                string grandChild_ParentId = string.Empty;
                if (lineFlag)
                {
                    int displayLine = 1;
                    foreach (var dataPoints in fieldTemplatesDataPoints)
                    {
                        var fields = selectedFields
                            .FirstOrDefault(x => x.RangeAddress == dataPoints.RangeAddress);

                        if (fields != null)
                        {
                            SelectedDataPointsTreeViewModel grandChild = new SelectedDataPointsTreeViewModel() // Line RangeNames
                            {
                                sequenceId = scheduleSequenceId, // Add sequnce ID for DisplayDColumn
                                id = worksheetName.Text.Replace(" ", "") + "_" + worksheetName.Value + "_" + displayLine,
                                parentId = parent.id,
                                text = dataPoints.FieldName,
                                hasChild = false,
                                ischecked = true,

                            };
                            listDataPoints.Add(grandChild);
                            scheduleSequenceId++;
                        }
                        displayLine++;
                    }
                }
                else
                {
                    foreach (var rangeAddress in worksheet_RangeAddress)
                    {
                        var fieldTemplate = ReportBL.GetFieldTemplate(costReportYear, rangeAddress.WorksheetName, rangeAddress.RangeAddress);
                        var match = columnHeaders.Find(x => x.DisplayColumn == fieldTemplate.DisplayColumn);

                        if (match != null && !lineFlag)
                        {
                            var dataPointsVM = listDataPoints
                                .Find(x => x.text == match.HeaderText && x.parentId == parent.id);


                            if (dataPointsVM == null)
                            {
                                grandChild_ParentId = string.Empty;
                                SelectedDataPointsTreeViewModel child = new SelectedDataPointsTreeViewModel() // Header Text
                                {
                                    sequenceId = scheduleSequenceId,
                                    id = worksheetName.Text.Replace(" ", "") + "_" + worksheetName.Value + "_" + fieldTemplate.DisplayColumn,
                                    parentId = parent.id,
                                    text = match.HeaderText,
                                    hasChild = true,
                                    ischecked = true,

                                };
                                grandChild_ParentId = child.id;
                                listDataPoints.Add(child);
                                scheduleSequenceId++;
                                //continue;
                            }

                            SelectedDataPointsTreeViewModel grandChild = new SelectedDataPointsTreeViewModel() // Line RangeNames
                            {
                                sequenceId = scheduleSequenceId,
                                id = worksheetName.Text.Replace(" ", "") + "_" + worksheetName.Value + "_" + fieldTemplate.DisplayColumn + "_" + fieldTemplate.DisplayLine,
                                parentId = grandChild_ParentId,
                                text = fieldTemplate.FieldName,
                                hasChild = false,
                                ischecked = true,

                            };
                            listDataPoints.Add(grandChild);
                            scheduleSequenceId++;
                        }

                    }
                }
            }
            return Json(listDataPoints, JsonRequestBehavior.AllowGet);
        }

        private static bool SchedulesWithRepeatingLines(string worksheetName)
        {
            bool lineFlag;
            switch (worksheetName)
            {
                case "OtherBal":
                case "A":
                case "A Pt2":
                case "C-1":
                case "C-2 Pt1":
                case "C-2 Pt2":
                case "C-3 Pt1":
                case "C-3 Pt2 Pg1":
                case "C-3 Pt2 Pg2":
                case "C-3 Pt2 Pg3":
                case "C-3 Pt3 Pg1":
                case "C-3 Pt3 Pg2":
                case "C-3 Pt3 Pg3":
                case "D-2":
                case "AT2":
                case "AT7":
                case "AT8":
                case "AT9 Pt2":
                case "AT9 Pt1 Pg1":
                case "AT9 Pt1 Pg2":
                case "AT9 Pt1 Pg3":
                case "AT11":
                case "AT11 Pt1":
                case "AT11 Pt2 Pg1":
                case "AT11 Pt2 Pg2":
                case "AT12 Pt3 Pg1":
                case "AT14 Pt3 Pg1":
                    lineFlag = true;
                    break;
                default:
                    lineFlag = false;
                    break;
            }

            return lineFlag;
        }

        private List<string> GetScheduleswithChartofAccount()
        {
            return new List<string>
            {
                "B-1","B-2 Pt1","B-2 Pt2","C Pt1","C Pt2",
                "C Pt3","D","E","AT1 Pt1","AT1 Pt2","AT1 Pt3",
                "AT2","AT6 Pt1","AT6 Pt2","AT6 Pt3","AT7"
            };
        }
            

        /// <summary>
        /// This is called when we load a saved report for editing.
        /// </summary>
        /// <param name="savedSearchId"></param>
        /// <param name="costReportYear"></param>
        /// <param name="costReportType"></param>
        /// <returns></returns>
        public ActionResult GetSelectedDataPoints(string savedReportID, string selectedYear)
        {
            // User is loading UI with selected values of a saved report. Need to create a NEW TEMPORARY REPORT (with the TempId GUID) that is 
            // a copy of the specified report so that any changes will not overwrite the original saved report (until/unless the user saves it again!).

            if (!int.TryParse(ClientMapping.GetInternalIdFromClientId(savedReportID, "savedsearch"), out int saveReportId))
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);

            //string costReportTypeCode = string.Empty;
            //string costReportTypeYear = string.Empty;

            //costReportTypeCode = ClientMapping.GetInternalIdFromClientId(costReportType, "costReportType");
            //costReportTypeYear = ClientMapping.GetInternalIdFromClientId(costReportYear, "costReportYear");
            //int.TryParse(costReportTypeYear, out int crYear);

            //Need to create a EDITING copy of the saved report
            DeleteEditingReport();
            Session[SESSION_KEYS_REPORTS.DW_NEW_COPIED_REPORT_ID] = null;
            int newReportId = CopySelectedReport(saveReportId);
            Session[SESSION_KEYS_REPORTS.DW_NEW_COPIED_REPORT_ID] = newReportId;
            // Note, if the user is loading a new saved report, we need to REMOVE any existing one.
            // SetNewReportGuid(tempId);

            // Now, load the view model with the data for the new editing report.
            Session[SESSION_KEYS_REPORTS.DW_SELECTED_DATA_POINTS] = null;
            List<string> selectedDataPoints = ReportBL.GetSelectedDataPoints(newReportId);
            Session[SESSION_KEYS_REPORTS.DW_SELECTED_DATA_POINTS] = selectedDataPoints;

            //Session["SelectedDataPointsList"] = null;
            //List<DataPointReportFields> selectedDataPointsList = ReportBL.GetSelectedDataPointsList(newReportId);
            //Session["SelectedDataPointsList"] = selectedDataPointsList;

            //Session[SESSION_KEYS_REPORTS.SELECTED_YEAR] = selectedYear;

            //DataWizard_TreeViewIndeterminateFlag(selectedYear);

            SelectedDataPointsViewModel selectedDataPointsViewModel = new SelectedDataPointsViewModel();
            selectedDataPointsViewModel.SelectedDataPoints = selectedDataPoints;

            return Json(new { Success = true, SelectedDataPointsViewModel = selectedDataPointsViewModel }, JsonRequestBehavior.AllowGet);
        }

        private int CopySelectedReport(int saveReportId)
        {
            var myUser = RetrieveSession.ARUMUser;
            Guid tempReportGuid = Guid.NewGuid();
            int newReportId = ReportBL.CopySelectedReport(saveReportId, tempReportGuid, myUser.FirstName, myUser.LastName);
            Session[SESSION_KEYS_REPORTS.DW_TEMP_REPORT_GUID] = tempReportGuid;
            return newReportId;
        }

        public ActionResult SaveDataPoints(DataPointReportsViewModel dataPointReportsViewModel)
        {
            MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
            if (myUser == null || !myUser.Roles.Any())
            {
                return RedirectToAction("Login", "Home");
            }

            List<DataPointsReportsViewModel> lstsavedSearches = new List<DataPointsReportsViewModel>();
            List<DataPointReports> dataPointsReportsData = new List<DataPointReports>();
            string tempReportId = Session[SESSION_KEYS_REPORTS.DW_TEMP_REPORT_GUID]?.ToString() ?? "";
            Guid.TryParse(tempReportId, out Guid reportTempGuid);

            bool isExistingReport = ReportBL.CheckReportExists(dataPointReportsViewModel.ReportName.Trim());
            if (isExistingReport)
            {
                ReportBL.DeleteMatchingReport(dataPointReportsViewModel.ReportName);
            }

            ReportBL.DeleteProviderDataPoints(reportTempGuid);
             var reportId = ReportBL.UpdateTempId(reportTempGuid, dataPointReportsViewModel.CostReportYear, dataPointReportsViewModel.CostReportType,
                     dataPointReportsViewModel.CostReportStatus, dataPointReportsViewModel.ReportName, $"{myUser.FirstName} {myUser.LastName}");
            Session[SESSION_KEYS_REPORTS.DW_TEMP_REPORT_GUID] = null;
            SaveProviderandDataPoints(dataPointReportsViewModel, reportId);

            dataPointsReportsData = ReportBL.GetSavedDataPointProviders();
            foreach (DataPointReports reportData in dataPointsReportsData)
            {
                DateTimeOffset? dateTimeOffset = reportData.CreatedOn;
                lstsavedSearches.Add(new DataPointsReportsViewModel
                {
                    ReportId = ClientMapping.MapInternalIdToClientId(reportData.ReportId.ToString(), "savedsearch"),
                    ReportName = reportData.ReportName,
                    CRYear = reportData.CRYear,
                    CRLength = reportData.CRLength,
                    CRType = reportData.CRType + (string.IsNullOrWhiteSpace(ReportBL.GetCostReportTypeName(reportData.CRType)) ? "" : " " + ReportBL.GetCostReportTypeName(reportData.CRType)),
                    CRStatus = reportData.CRStatus,
                    Creator = reportData.CreatedBy,
                    DateCreated = dateTimeOffset.Value.Date.ToShortDateString(),

                });
            }
            return Json(new { Success = true, DataPointsSavedSearchesViewModel = lstsavedSearches }, JsonRequestBehavior.AllowGet);
        }
        private void SaveProviderandDataPoints(DataPointReportsViewModel dataPointReportsViewModel, int repordId)
        {
            List<int> providerIds = new List<int>();
            foreach (var provider in dataPointReportsViewModel.ProviderData)
            {
                int.TryParse(ClientMapping.GetInternalIdFromClientId(provider, "provider"), out int providerId);
               
                DataPointReportProviders dataPointReportProviders = new DataPointReportProviders
                {
                    ReportId = repordId,
                    ProviderId = providerId
                };

                ReportBL.SaveDataPointReportProviders(dataPointReportProviders);
            }
            foreach (var rangeName in dataPointReportsViewModel.RangeName)
            {
                string rangeNameSelected = null;
                int? displayColumn = null;
                if (rangeName.Contains(":"))
                {
                    string[] value = rangeName.Split(':');
                    string worksheetName = string.Empty;
                    string itemTwo = string.Empty;
                    string displayColumnSelected = string.Empty;
                    if (value.Length == 3)
                    {
                        worksheetName = value[0];
                        itemTwo = value[1];
                        displayColumn = Convert.ToInt32(value[2]);
                        if (itemTwo.Length > 4)//"B-1:10018"
                        {
                            rangeNameSelected = itemTwo;
                        }
                        else // Column // "B-1:2"
                        {
                            displayColumn = Convert.ToInt32(itemTwo);
                        }
                        DataPointReportFields dataPointReportFields = new DataPointReportFields
                        {
                            ReportId = repordId,
                            RangeAddress = rangeNameSelected,
                            WorksheetName = worksheetName,
                            DisplayColumn = (int)displayColumn
                        };
                        ReportBL.SaveDataPointFields(dataPointReportFields);
                    }
                }
            }
        }

        public ActionResult DeleteSavedSearch(string id)
        {
            if (!int.TryParse(ClientMapping.GetInternalIdFromClientId(id, "savedsearch"), out int dataPointId))
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);

            List<DataPointReports> dataPointsReportData = ReportBL.DeleteDataPoints(dataPointId);
           
            List<DataPointsReportsViewModel> lstsavedSearches = new List<DataPointsReportsViewModel>();
            foreach (DataPointReports reportData in dataPointsReportData)
            {
                DateTimeOffset? dateTimeOffset = reportData.CreatedOn;
                lstsavedSearches.Add(new DataPointsReportsViewModel
                {
                    ReportId = ClientMapping.MapInternalIdToClientId(reportData.ReportId.ToString(), "savedsearch"),
                    ReportName = reportData.ReportName,
                    CRYear = reportData.CRYear,
                    CRType = reportData.CRType + (string.IsNullOrWhiteSpace(ReportBL.GetCostReportTypeName(reportData.CRType)) ? "" : " " + ReportBL.GetCostReportTypeName(reportData.CRType)),
                    CRStatus = reportData.CRStatus,
                    Creator = reportData.CreatedBy,
                    DateCreated = dateTimeOffset.Value.Date.ToShortDateString(),

                });
            }
            return Json(new { Success = true, DataPointsSavedSearchesViewModel = lstsavedSearches }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SaveDownloadReport(DataPointReportsViewModel dataPointReportsViewModel)
        {
            string tempReportId = Session[SESSION_KEYS_REPORTS.DW_TEMP_REPORT_GUID]?.ToString() ?? "";
            Guid.TryParse(tempReportId, out Guid reportTempGuid);
            int reportId = ReportBL.DeleteProviderDataPoints(reportTempGuid);
            SaveProviderandDataPoints(dataPointReportsViewModel, reportId);
            string ReportId = ClientMapping.MapInternalIdToClientId(reportId.ToString(), "savedsearch");
            return Json(new { Success = true, reportID = ReportId }, JsonRequestBehavior.AllowGet);
        }

        private string CreateStyle(IWorkbook workbook, string styleName, bool bold, int size, bool wrapText,
                string fontNane, ExcelVAlign vAlign, ExcelHAlign hAlign)
        {
            if (!workbook.Styles.Contains(styleName))
            {
                IStyle style = workbook.Styles.Add(styleName);
                style.BeginUpdate();
                style.Font.FontName = fontNane;
                style.Font.Size = size;
                style.Font.Bold = bold;
                style.VerticalAlignment = vAlign;
                style.HorizontalAlignment = hAlign;
                style.WrapText = wrapText;
                style.EndUpdate();
            }
            return styleName;
        }
        private IAllProviders GetAllProviderList(Guid arumToken, bool includeContacts, bool includeIdentifiers, bool includeAddress,
                             bool includeEffectiveDates, bool includeChowGroups, bool includeAffiliates, bool includeAttributes,
                             bool includeStatuses, bool includeChowChain, string callerFunction)
        {
            var parameters = new List<ARUMWebServiceApplication.WebParameter> {
                new ARUMWebServiceApplication.WebParameter { Key = "includeContacts", Value = includeContacts ? "true" : "false", DataType = "bool" },
                new ARUMWebServiceApplication.WebParameter { Key = "includeIdentifiers", Value = includeIdentifiers ? "true" : "false", DataType = "bool" },
                new ARUMWebServiceApplication.WebParameter { Key = "includeAddress", Value = includeAddress ? "true" : "false", DataType = "bool" },
                new ARUMWebServiceApplication.WebParameter { Key = "includeEffectiveDates", Value = includeEffectiveDates ? "true" : "false", DataType = "bool" },
                new ARUMWebServiceApplication.WebParameter { Key = "includeChowGroups", Value = includeChowGroups  ? "true" : "false", DataType = "bool" },
                new ARUMWebServiceApplication.WebParameter { Key = "includeAffiliates", Value = includeAffiliates  ? "true" : "false", DataType = "bool" },
                new ARUMWebServiceApplication.WebParameter { Key = "includeAttributes", Value = includeAttributes  ? "true" : "false", DataType = "bool" },
                new ARUMWebServiceApplication.WebParameter { Key = "includeStatuses", Value = includeStatuses  ? "true" : "false", DataType = "bool" },
                new ARUMWebServiceApplication.WebParameter { Key = "includeChowChain", Value = includeChowChain  ? "true" : "false", DataType = "bool" }
            };

            ARUMHandler.ClearCache();
            var returnValue = _applicationServiceProxy.GetAllActiveProvidersWithParameters(arumToken, parameters, "RateController/GetAllProviderList");

            if (!returnValue.Result.Successful)
            {
                throw new Exception($" Providers not found. {returnValue.Result.Message}");
            }

            return returnValue;
        }

        public static int GetPageNumber(string rangeName)
        {
            // Check if the rangeName is empty or doesn't end with an alphabetic character
            if (string.IsNullOrEmpty(rangeName) || !char.IsLetter(rangeName[rangeName.Length - 1]))
            {
                return 0;
            }

            // Extract suffix of up to 2 characters from the end of the rangeName
            string suffix = rangeName.Length >= 2 && char.IsLetter(rangeName[rangeName.Length - 2])
                            ? rangeName.Substring(rangeName.Length - 2)
                            : rangeName.Substring(rangeName.Length - 1);

            // Calculate the PageNumber based on the suffix length
            if (suffix.Length == 1)
            {
                return suffix[0] - 'A' + 1;
            }
            else if (suffix.Length == 2)
            {
                return suffix[0] - 'A' + 1;
            }
            else
            {
                return 0;
            }
        }

        public ActionResult GenerateDataWizardReport(string reportId, string reportName)
        {
            if (!int.TryParse(ClientMapping.GetInternalIdFromClientId(reportId, "savedsearch"), out int dataPointReportId))
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);

            List<int> selectedProviders = new List<int>();
            List<DataWizardProviders> providerList = new List<DataWizardProviders>();
            string fileName = $"{reportName}.xlsx";
            string fullPath;
            fullPath = Path.Combine(Path.GetTempPath(), fileName);

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                Syncfusion.XlsIO.IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Xlsx;

                //Instantiate a new workbook
                IWorkbook workbook = application.Workbooks.Create(1);

                var worksheetNames = ScheduleBL.GetWorksheetName();

                if (Session[SESSION_KEYS_REPORTS.DW_PROVIDER_LIST] != null)
                {
                    providerList = (List<DataWizardProviders>)Session[SESSION_KEYS_REPORTS.DW_PROVIDER_LIST];
                }
                List<CostReportStatus> costReportStatuses = ReportBL.GetCostReportStatuses();

                List<RateProvider> providersList = ReportBL.GetProvidersList();

                DataPointReports dataPointReports = ReportBL.GetDataPointReportsById(dataPointReportId);

                List<DataPointReportFields> selectedFields = ReportBL.GetAllSelectedDataPoints(dataPointReportId);

                selectedProviders = ReportBL.GetSelectedProviders(dataPointReportId);

                MSLC.ApplicationProxy.Interfaces.IAllProviders arum_Providers = _applicationServiceProxy.GetAllActiveProvidersFromListWithOptions(RetrieveSession.Classification.ARUMToken,
                   selectedProviders, false, true, false, true, false, false, false, "ratesetting.providers");

                //Retrieving data for selected providers and selected data points of Data Wizard Report 
                List<DataWizardReport> reportDataList = ReportBL.GenerateDataWizardReport(dataPointReportId);

                //Get Selected Distinct WorkSheetName
                List<string> distinctWorkSheetName = ReportBL.GetSelectedDistinctWorkSheetName(dataPointReportId);
                IAddressCodeFunctions addressCodeFunctions = CostReportBL.GetInstanceAddressCodeFunctions(RetrieveSession.Classification);

                foreach (var worksheetName in worksheetNames)
                {
                    if (!distinctWorkSheetName.Contains(worksheetName.Text))
                        continue;

                    int old_ProviderId_Arum = 0;
                    int old_ProviderCostReportId = 0;
                    string providerNPI = String.Empty;
                    string providerName = String.Empty;
                    string providerNumber = String.Empty;
                    string costReportType = String.Empty;
                    string costReportStatus = string.Empty;
                    string documentNumber = string.Empty;
                    DateTime costReportBeginDate = new DateTime();
                    DateTime costReportEndDate = new DateTime();
                    bool providerIdFalg = false;

                    ////Retrieving data for selected providers and selected data points of Data Wizard Report 
                    //var reportDataList = ReportBL.GenerateDataWizardReport(dataPointReportId, worksheetName.Text);

                    List<DataWizardReportViewModel> dataWizardReportDataList = new List<DataWizardReportViewModel>();
                    var filteredRecords = reportDataList
                        .Where(r => r.WorksheetName == worksheetName.Text)
                        .ToList();
                    if (filteredRecords.Count > 0)
                    {
                        //For Sch A order columns by DEN 
                        if (worksheetName.Text == "A")
                        {
                            filteredRecords = filteredRecords
                            .OrderBy(x => x.ProviderId)
                            .ThenBy(x => x.RangeName)
                            .ToList();
                        }
                        else if (worksheetName.Text == "C-1")
                        {
                            foreach (var reportData in filteredRecords)
                            {
                                reportData.PageNumber = GetPageNumber(reportData.RangeName);
                            }
                        }
                        var distinctColumns = filteredRecords
                            .Select(x => x.DisplayColumn)
                            .Distinct()
                            .ToList();
                        var columnHeaders = ReportBL.GetColumnHeaders(worksheetName.Text, dataPointReports.CRYear.ToString());

                        var fieldTemplateHeaders = ReportBL.GetScheduleDataPoints(worksheetName.Text, dataPointReports.CRYear.ToString(), providerList);

                        foreach (var reportData in filteredRecords)
                        {
                            int providerId_Arum = reportData.ProviderId;
                            if (providerId_Arum != old_ProviderId_Arum || !providerIdFalg)
                            {
                                var provider = arum_Providers.lstProviders.FirstOrDefault(x => x.ProviderID == providerId_Arum);
                                //Getting the providerNPI from ARUM
                                providerNPI = provider.Identifiers.FirstOrDefault(i => i.Type == "NPI") != null ?
                                    provider.Identifiers.FirstOrDefault(i => i.Type == "NPI").Value : string.Empty;
                                old_ProviderId_Arum = providerId_Arum;
                                providerIdFalg = true;

                                //Getting the ProviderName and ProviderNumber from database
                                var rateProvider = providersList
                                    .FirstOrDefault(x => x.ProviderId == providerId_Arum);
                                providerName = rateProvider.ProviderName;
                                providerNumber = rateProvider.ProviderNumber;
                            }

                            int providerCostReportId = reportData.ProviderCostReportId;
                            if(providerCostReportId != old_ProviderCostReportId)
                            {
                                var pr = ReportBL.GetProviderCostReport(reportData.ProviderCostReportId);
                                costReportType = pr.CostReportTypeCode;
                                costReportBeginDate = pr.PeriodBegin;
                                costReportEndDate = pr.PeriodEnd;
                                documentNumber = pr.Version;
                                costReportStatus = costReportStatuses
                               .Where(x => x.CostReportStatusId == pr.CostReportStatusId)
                               .Select(s => s.Status)
                               .FirstOrDefault();

                                old_ProviderCostReportId = providerCostReportId;
                            }
                            void AddReportData(string fieldName, string dataValue, int dataType, string rangeName)
                            {
                                dataWizardReportDataList.Add(new DataWizardReportViewModel
                                {
                                    ProviderCostReportId = reportData.ProviderCostReportId,
                                    ProviderNPI = providerNPI,
                                    ProviderName = providerName,
                                    ProviderNumber = providerNumber,
                                    CostReportYear = dataPointReports.CRYear,
                                    CostReportType = costReportType,
                                    CostReportStatus = costReportStatus,
                                    CostReportBeginDate = costReportBeginDate,
                                    CostReportEndDate = costReportEndDate,
                                    DocumentNumber = documentNumber,
                                    FieldName = fieldName,
                                    DataValue = dataValue,
                                    PageNumber = reportData.PageNumber,
                                    DisplayLine = reportData.DisplayLine,
                                    DisplayColumn = reportData.DisplayColumn,
                                    Line = reportData.Line,
                                    WorksheetName = reportData.WorksheetName,
                                    DataType = dataType,
                                    RangeName = rangeName
                                });
                            }
                            AddReportData(reportData.FieldName, reportData.DataValue, reportData.DataType, reportData.RangeName);
                            if (reportData.RangeName == "10110" && !string.IsNullOrWhiteSpace(reportData.DataValue))
                            {
                                string countyName = addressCodeFunctions.GetCountyNameByCountyCode(Convert.ToInt32(reportData.DataValue));
                                AddReportData("County Name", countyName, 1, "CountyName");
                            }
                        }

                        if (dataWizardReportDataList.Count > 0)
                        {
                            IWorksheet newWorksheet = null;
                            // dynCols: (displayLine, displayCol, headerText, dataType)
                            List<(int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType)> dynCols;
                            
                            switch (worksheetName.Text)
                            {
                                case "A-2 Pt2":
                                case "D-2":
                                case "E-1":
                                case "E-1 Pt1":
                                case "AT2":
                                case "AT9 Pt2":
                                case "AT9 Pt1 Pg2":
                                case "AT9 Pt1 Pg3":
                                case "AT11 Pt2 Pg2":
                                case "AT12 Pt3 Pg1":
                                case "AT14 Pt3 Pg1":
                                    var selectedColumns = selectedFields.Where(x => x.WorksheetName == worksheetName.Text)
                                        .Select(h => h.RangeAddress)
                                        .ToList();
                                    var colHeaders = fieldTemplateHeaders
                                        .Where(x => selectedColumns.Contains(x.RangeAddress))
                                        .OrderBy(x => x.DisplayLine).ThenBy(x => x.DisplayColumn).ToList();
                                    //Declare a worksheet object
                                    newWorksheet = workbook.Worksheets.Create(ScheduleBL.ScheduleNameFromUISelector(worksheetName.Text));

                                    dynCols = BuildDynamicColumnsForOtherSch(dataWizardReportDataList, colHeaders, worksheetName.Text, true);
                                    CreateExcelWorkSheetReport(workbook, newWorksheet, dataWizardReportDataList, dynCols, worksheetName.Text, true); 
                                    break;
                                case "A":
                                    //Declare a worksheet object
                                    newWorksheet = workbook.Worksheets.Create(worksheetName.Text);

                                    dynCols = BuildDynamicColumnsForSchA(dataWizardReportDataList, worksheetName.Text, dataPointReports.CRYear.ToString());
                                    CreateExcelWorkSheetReport(workbook, newWorksheet, dataWizardReportDataList, dynCols, worksheetName.Text);
                                    break;
                                case "C-1":
                                    int maxPageNumber = dataWizardReportDataList.Max(r => r.PageNumber);
                                    for (int page = 1; page <= maxPageNumber; page++)
                                    {
                                        //Declare a worksheet object
                                        newWorksheet = workbook.Worksheets.Create(worksheetName.Text + " Pg" + page);

                                        List<DataWizardReportViewModel> filteredList = dataWizardReportDataList
                                                                                        .Where(r => r.PageNumber == 0 || r.PageNumber == page)
                                                                                        .ToList();

                                        dynCols = BuildDynamicColumnsForSchC1(filteredList, fieldTemplateHeaders);
                                        CreateExcelWorkSheetReport(workbook, newWorksheet, filteredList, dynCols, worksheetName.Text);
                                    }
                                    break;
                                case "C-2 Pt1":
                                case "C-2 Pt2":
                                case "C-3 Pt1":
                                case "C-3 Pt2 Pg1":
                                case "C-3 Pt2 Pg2":
                                case "C-3 Pt2 Pg3":
                                case "C-3 Pt3 Pg1":
                                case "C-3 Pt3 Pg2":
                                case "C-3 Pt3 Pg3":
                                    var selectedCColumns = selectedFields.Where(x => x.WorksheetName == worksheetName.Text)
                                        .Select(h => h.RangeAddress)
                                        .ToList();
                                    var colCHeaders = fieldTemplateHeaders
                                        .Where(x => selectedCColumns.Contains(x.RangeAddress))
                                        .OrderBy(x => x.DisplayLine).ThenBy(x => x.DisplayColumn).ToList();
                                    //Declare a worksheet object
                                    newWorksheet = workbook.Worksheets.Create(ScheduleBL.ScheduleNameFromUISelector(worksheetName.Text));

                                    dynCols = BuildDynamicColumnsForC2_C3Sch(dataWizardReportDataList, colCHeaders, worksheetName.Text);
                                    CreateExcelWorkSheetReport(workbook, newWorksheet, dataWizardReportDataList, dynCols, "", true);
                                    break;
                                case "OtherBal":
                                case "AT11":
                                case "AT11 Pt1":
                                case "AT11 Pt2 Pg1":
                                    var selectedColumns1 = selectedFields.Where(x => x.WorksheetName == worksheetName.Text)
                                        .Select(h => h.RangeAddress)
                                        .ToList();
                                    var colHeaders1 = fieldTemplateHeaders
                                        .Where(x => selectedColumns1.Contains(x.RangeAddress))
                                        .OrderBy(x => x.DisplayLine).ThenBy(x => x.DisplayColumn).ToList();
                                    //Declare a worksheet object
                                    newWorksheet = workbook.Worksheets.Create(ScheduleBL.ScheduleNameFromUISelector(worksheetName.Text));
                                    dynCols = BuildDynamicColumnsForSchAt11(worksheetName.Text, dataWizardReportDataList, colHeaders1);
                                    CreateExcelWorkSheetReport(workbook, newWorksheet, dataWizardReportDataList, dynCols, worksheetName.Text);
                                    break;
                                default:
                                    var selColumns = selectedFields.Where(x => x.WorksheetName == worksheetName.Text)
                                        .Select(h => h.RangeAddress)
                                        .ToList();
                                    var orderedHeaders = fieldTemplateHeaders
                                        .Where(x => selColumns.Contains(x.RangeAddress))
                                        .OrderBy(x => x.DisplayLine).ThenBy(x => x.DisplayColumn).ToList();
                                    //Declare a worksheet object
                                    newWorksheet = workbook.Worksheets.Create(ScheduleBL.ScheduleNameFromUISelector(worksheetName.Text));
                                    dynCols = BuildDynamicColumnsForOtherSch(dataWizardReportDataList, orderedHeaders, worksheetName.Text);
                                    CreateExcelWorkSheetReport(workbook, newWorksheet, dataWizardReportDataList, dynCols, worksheetName.Text);
                                    break;
                            }
                        }
                    }
                }

                //Check if the worksheet exists
                bool isWorksheetAvaliable = workbook.Worksheets.Any(sheet => sheet.Name == "Sheet1");
                if (isWorksheetAvaliable && workbook.Worksheets.Count > 1)
                    workbook.Worksheets.Remove("Sheet1");

                //Save the workbook to stream
                MemoryStream outputStream = new MemoryStream();
                workbook.SaveAs(outputStream);
                outputStream.Position = 0;

                workbook.Close();

                //Dispose the ExcelEngine object
                MemoryStream report = outputStream;

                using (FileStream fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    report.WriteTo(fileStream);
                }

            }
            return Json(new { Success = true, fileName = HttpUtility.UrlEncode(fileName) }, JsonRequestBehavior.AllowGet);
        }

        public static string AddUniqueColumn(DataTable t, string baseName, Type type)
        {
            string name = baseName;
            int n = 1;
            while (t.Columns.Contains(name))
                name = baseName + "_" + n++;
            t.Columns.Add(name, type);
            return name;
        }

        public static void ExportWithDisplayHeaders(
            IWorksheet ws,
            DataTable dt,
            IList<string> displayHeaders,
            int startRow = 1,
            int startCol = 1,
            Action<IRange> headerStyler = null,
            Action<IRange> bodyStyler = null)
        {
            if (displayHeaders.Count != dt.Columns.Count)
                throw new ArgumentException("displayHeaders.Count must match dt.Columns.Count");

            int totalCols = dt.Columns.Count;
            if (totalCols == 0) return;
            // Write visible headers
            for (int c = 0; c < displayHeaders.Count; c++)
            {
                ws.Range[startRow, startCol + c].Text = displayHeaders[c];
            }

            // Bulk import data (no DT headers)
            if(dt.Rows.Count > 0)
                ws.ImportDataTable(dt, false, startRow + 1, startCol);

            
            //int totalRows = dt.Rows.Count + 1;

            var headerRange = ws.Range[startRow, startCol, startRow, startCol + totalCols - 1];

            if (headerStyler != null)
                headerStyler(headerRange);
            else
            {
                headerRange.CellStyle.Font.Bold = true;
                headerRange.WrapText = true;
                headerRange.VerticalAlignment = ExcelVAlign.VAlignTop;
                headerRange.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                headerRange.RowHeight = 60;
            }
            if (dt.Rows.Count > 0)
            {
                var bodyRange = ws.Range[startRow + 1, startCol, startRow + dt.Rows.Count, startCol + totalCols - 1];
                if (bodyStyler != null)
                    bodyStyler(bodyRange);
                else
                {
                    //bodyRange.VerticalAlignment = ExcelVAlign.VAlignTop;
                    bodyRange.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                }
            }
        }

        public static string ToExcelString(object value, int dataType)
        {
            if (value == null) return "";
            switch (dataType)
            {
                case 1: return value.ToString(); // String
                case 2: int iv; return int.TryParse(value.ToString(), out iv) ? iv.ToString() : "";
                case 3: decimal dv; return decimal.TryParse(value.ToString(), out dv) ? dv.ToString() : "";
                case 6: DateTime dt; return DateTime.TryParse(value.ToString(), out dt) ? dt.ToString("MM/dd/yyyy") : "";
                case 7:
                case 12:
                    bool bv;
                    //bool may show up as 1/0 or string
                    if (int.TryParse(value.ToString(), out int result))
                        bv = result == 1 ? true : false;
                    else
                        bool.TryParse(value.ToString(), out bv);
                    return bv ? "Y" : "N";
                default: return value.ToString();
            }
        }

           private static GroupKey GetTopGroupByDistinctLines(IEnumerable<DataWizardReportViewModel> reportData,
                                                           Func<DataWizardReportViewModel, bool> predicate)
        {
            var top = reportData
                .Where(predicate)
                .GroupBy(r => new
                {
                    r.ProviderCostReportId,
                    r.ProviderNumber,
                    CostReportType = r.CostReportType ?? string.Empty,
                    CostReportStatus = r.CostReportStatus ?? string.Empty
                })
                .Select(g => new GroupKey
                {
                    ProviderCostReportId = g.Key.ProviderCostReportId,
                    ProviderNumber = g.Key.ProviderNumber,
                    CostReportType = g.Key.CostReportType,
                    CostReportStatus = g.Key.CostReportStatus,
                    DistinctLineCount = g.Select(x => x.DisplayLine).Distinct().Count(),
                    DistinctColumnCount = g.Select(x => x.DisplayColumn).Distinct().Count()
                })
                .OrderByDescending(x => x.DistinctLineCount)
                .ThenByDescending(x => x.DistinctColumnCount)
                .FirstOrDefault();

            return top; 
        }

        // Returns all rows for the top group within a line range (section)
        public List<DataWizardReportViewModel> GetSectionRowsForTopGroup(List<DataWizardReportViewModel> reportData,
                                                               int startLine, int endLine)
        {
            var key = GetTopGroupByDistinctLines(reportData, x => x.DisplayLine >= startLine && x.DisplayLine <= endLine);
            if (key == null) return new List<DataWizardReportViewModel>();

            return reportData
                .Where(x => x.ProviderCostReportId == key.ProviderCostReportId
                         && x.ProviderNumber == key.ProviderNumber
                         && (x.CostReportStatus ?? string.Empty) == key.CostReportStatus
                         && (x.CostReportType ?? string.Empty) == key.CostReportType
                         && x.DisplayLine >= startLine && x.DisplayLine <= endLine)
                .OrderBy(x => x.DisplayLine)               
                .ToList();
        }

        public List<DataWizardReportViewModel> GetRowsAtLineForTopGroup(List<DataWizardReportViewModel> reportData, int line)
        {
            var key = GetTopGroupByDistinctLines(reportData, x => x.DisplayLine == line);
            if (key == null) return new List<DataWizardReportViewModel>();

            return reportData
                .Where(x => x.ProviderCostReportId == key.ProviderCostReportId
                         && x.ProviderNumber == key.ProviderNumber
                         && (x.CostReportStatus ?? string.Empty) == key.CostReportStatus
                         && (x.CostReportType ?? string.Empty) == key.CostReportType
                         && x.DisplayLine == line)
                .OrderBy(x => x.DisplayLine)
                .ThenBy(x => x.DisplayColumn)             
                .ToList();
        }


        static readonly Dictionary<string, SheetProfile> _profiles =
            new Dictionary<string, SheetProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["OtherBal"] = new SheetProfile
                {
                    SectionRanges = new List<(int, int)> { (2, 7), (10, 15), (18, 23), (25, 28), (31, 35) },
                    TotalLines = new List<int> { 8, 16, 24, 29, 36 }
                },
                ["AT7"] = new SheetProfile
                {
                    SectionRanges = new List<(int, int)> { (2, 5), (8, 11), (14, 17), (20, 23), (26, 29) },
                    TotalLines = new List<int> { 6, 12, 18, 24, 30, 31 }
                },
                ["AT11"] = new SheetProfile()
                {
                    SectionRanges = new List<(int, int)> { (4, 23) },
                    TotalLines = new List<int> { 24 }
                },
                ["AT11 Pt1"] = new SheetProfile()
                {
                    SectionRanges = new List<(int, int)> { (2, 79), (83, 135) },
                    TotalLines = new List<int> { 80, 136 }
                },
                ["AT11 Pt2 Pg1"] = new SheetProfile
                {
                    SectionRanges = new List<(int, int)> { (2, 105), (109, 134) },
                    TotalLines = new List<int> {106, 135 }
                }
                
            };

        private static void AppendRows(IEnumerable<DataWizardReportViewModel> rows,
                                       List<(int DisplayLine, int DisplayColumn, string Line, string FieldName, string RangeName, int DataType)> dest)
        {
            foreach (var r in rows)
                dest.Add((r.DisplayLine, r.DisplayColumn, r.Line, r.FieldName, r.RangeName, r.DataType));
        }

        public List<(int DisplayLine, int DisplayColumn, string Line, string FName, string RName, int DataType)> BuildDynamicColumnsForSheet(string sheetName,
           List<DataWizardReportViewModel> reportData)
        {
            if (!_profiles.TryGetValue(sheetName, out var profile))
                throw new ArgumentException("Unknown sheet name: " + sheetName);

            var dynCols = new List<(int, int, string, string, string, int)>();

            // Sections
            foreach (var range in profile.SectionRanges)
            {
                var rows = GetSectionRowsForTopGroup(reportData, range.Start, range.End);
                AppendRows(rows, dynCols);
            }

            // Totals
            foreach (var line in profile.TotalLines)
            {
                var rows = GetRowsAtLineForTopGroup(reportData, line);
                AppendRows(rows, dynCols);
            }

            return dynCols
                .OrderBy(x => x.Item1)
                .ThenBy(x => x.Item2)
                .ToList();
        }

        public List<(int DisplayLine, int DisplayColumn, string Line, string FName, string RName, int DataType)> BuildDynamicColumnsForSchAt11(string sheetName,
        List<DataWizardReportViewModel> reportData, List<FieldTemplate> columnHeaders)
        {
            if (!_profiles.TryGetValue(sheetName, out var profile))
                throw new ArgumentException("Unknown sheet name: " + sheetName);

            var dynCols = new List<(int, int, string, string, string, int)>();

            if (sheetName == "AT11 Pt1")
            {
                 var att11TextBoxCols = columnHeaders
                    .Where(x => !char.IsLetter(x.RangeName.First()) && !char.IsLetter(x.RangeName.Last()))
                    .OrderBy(h => h.DisplayLine)
                    .ThenBy(h => h.DisplayColumn)
                    .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                    .ToList();
                foreach (var h in att11TextBoxCols)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                }
            }
            else if(sheetName == "AT11")
            {
                var att11TextBoxCols = columnHeaders
                    .Where(x => x.DisplayColumn == 1 && x.DisplayLine == 0)
                    .OrderBy(h => h.DisplayLine)
                    .ThenBy(h => h.DisplayColumn)
                    .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                    .ToList();
                foreach (var h in att11TextBoxCols)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                }

                var att11Row2 = columnHeaders
                    .Where(x => x.DisplayLine == 2)
                    .OrderBy(h => h.DisplayLine)
                    .ThenBy(h => h.DisplayColumn)
                    .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                    .ToList();
                foreach (var h in att11Row2)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                }

            }
                foreach (var range in profile.SectionRanges)
                {
                    var headers = columnHeaders
                        .Where(x => x.DisplayLine == range.Start)
                        .OrderBy(h => h.DisplayLine)
                        .ThenBy(h => h.DisplayColumn)
                        .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                        .ToList();

                    //Columns that user can define description for
                    for (int dLine = range.Start; dLine <= range.End; dLine++)
                    {
                        foreach (var h in headers)
                        {
                            dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(dLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                        }
                    }
                }

            // Totals
            foreach (var line in profile.TotalLines)
            {
                var headers = columnHeaders
                .Where(x => x.DisplayLine == line)
                .OrderBy(h => h.DisplayLine)
                .ThenBy(h => h.DisplayColumn)
                .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                .ToList();
                foreach (var h in headers)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                }
            }
 
            return dynCols
                .OrderBy(x => x.Item1)
                .ThenBy(x => x.Item2)
                .ToList();
        }

        public List<(int DisplayLine, int DisplayColumn, string Line, string FName, string Rname, int DataType)> BuildDynamicColumnsForSchA(
            List<DataWizardReportViewModel> reportData, string worksheetName, string costReportYear)
        {
            var dynCols = new List<(int, int, string, string, string, int)>();

            //Get FieldTemplate records based on worksheet name and CR year
            List<FieldTemplate> filedTempateList = ReportBL.GetFieldTemplateList(worksheetName, costReportYear);
            // Add CountyName as it does not belong to the FieldTemplates
            FieldTemplate countyName_FT = new FieldTemplate
            {
                DisplayLine = 0,
                DisplayColumn = 0,
                Line = string.Empty,
                FieldName = "County Name",
                RangeName = "CountyName",
                DataType = DataType.VarChar
            };
            filedTempateList.Add(countyName_FT);

            var listRangeNames = reportData
                .Select(r => r.RangeName)
                .ToHashSet();

            var resultList = filedTempateList
                .Where(x => listRangeNames.Contains(x.RangeName))
                .ToList();

            dynCols = new List<(int, int, string, string, string, int)>();

            var relatedRanges = new Dictionary<string, string>
            {
                { "LockedOn", "SubmitOn" },
                { "10030", "10280" },
                { "10110", "CountyName" }
            };
            var addedRangeNames = new HashSet<string>();
            foreach (var data in resultList.ToList())
            {
                if (addedRangeNames.Add(data.RangeName))
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))
                                       (data.DisplayLine, data.DisplayColumn, data.Line, data.FieldName, data.RangeName, data.DataType));
                }
                   
                if (relatedRanges.TryGetValue(data.RangeName, out var relatedRange))
                {
                    var temp = resultList.FirstOrDefault(x => x.RangeName == relatedRange);
                    if (temp != null && addedRangeNames.Add(relatedRange))
                    {
                        dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))
                                 (temp.DisplayLine, temp.DisplayColumn, temp.Line, temp.FieldName, temp.RangeName, temp.DataType));
                    }
                }
            }    
            return dynCols;
        }


        public List<(int DisplayLine, int DisplayColumn, string Line, string FName, string RName, int DataType)> BuildDynamicColumnsForSchC1(
            List<DataWizardReportViewModel> reportData, List<FieldTemplate> columnHeaders)
        {
            var dynCols = new List<(int, int, string, string, string, int)>();
            var result = reportData
                .Where(r => r.DisplayLine > 0)
               .GroupBy(r => new { r.ProviderCostReportId, r.ProviderNumber, r.CostReportType, r.CostReportStatus })
               .Select(g => new
               {
                   ProviderCostReportId = g.Key.ProviderCostReportId,
                   ProviderNumber = g.Key.ProviderNumber,
                   CostReportType = g.Key.CostReportType,
                   CostReportStatus = g.Key.CostReportStatus,
                   DistinctDisplayLineCount = g.Select(x => x.DisplayLine).Distinct().Count()
               })
               .OrderByDescending(x => x.DistinctDisplayLineCount)
               .FirstOrDefault();

            var resultWithMaxDisplayColumn = reportData
                .Where(x => x.DisplayLine == 0)
                .GroupBy(r => new { r.ProviderCostReportId, r.ProviderNumber, r.CostReportType, r.CostReportStatus })
                  .Select(g => new
                  {
                      ProviderCostReportId = g.Key.ProviderCostReportId,
                      ProviderNumber = g.Key.ProviderNumber,
                      CostReportType = g.Key.CostReportType,
                      CostReportStatus = g.Key.CostReportStatus,
                      DistinctDisplayColumnCount = g.Select(x => x.DisplayColumn).Count()
                  })
           .OrderByDescending(x => x.DistinctDisplayColumnCount)
           .FirstOrDefault();

            dynCols = new List<(int, int, string, string, string, int)>();

            if (resultWithMaxDisplayColumn != null)
            {
                var c1Data = columnHeaders
                   .Where(x => x.DisplayLine == 0)
                   .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                   .ToList();
                // NFOFI-5731: this is hardcoded as 100 for the value in HTML, business has asked that it gets added to the report as well
                var baseYearAllowance = new
                {
                    DisplayLine = (int?)0,
                    DisplayColumn = (int?)1,
                    Line = "1",
                    FieldName = "Base Percentage Allowance",
                    RangeName = "BASE",
                    DataType = DataType.Integer
                };

                c1Data.Add(baseYearAllowance);

                c1Data = c1Data.OrderBy(h => h.Line).ToList();

                foreach (var c1 in c1Data)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(c1.DisplayLine, c1.DisplayColumn, c1.Line, c1.FieldName, c1.RangeName, c1.DataType));
                }
            }

            if (result != null)
            {
                var c1GridData = columnHeaders
                   .Where(x => x.DisplayLine > 0)
                   .OrderBy(h => h.DisplayLine)
                   .ThenBy(h => h.DisplayColumn)
                   .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                   .ToList();

                for (int dLine = 1; dLine <= result.DistinctDisplayLineCount; dLine++)
                {
                    foreach (var data in c1GridData)
                    {
                        dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(dLine, data.DisplayColumn, data.Line, data.FieldName, data.RangeName, data.DataType));
                     }
                }
            }
            return dynCols;
        }

        public List<(int DisplayLine, int DisplayColumn, string Line, string FName, string RName, int DataType)> BuildDynamicColumnsForC2_C3Sch(
            List<DataWizardReportViewModel> reportData, List<FieldTemplate> columnHeaders, string worksheetName)
        {
            var dynCols = new List<(int, int, string, string, string, int)>();
            int recordWithMaxDisplayLineNo = reportData.Where(x => x.DisplayLine > 0 && x.DataValue != "")
                    .GroupBy(r => new { r.ProviderCostReportId, r.ProviderNumber, r.CostReportType, r.CostReportStatus })
                    .Select(g => g.Select(x => x.DisplayLine).Distinct().Count())
                    .DefaultIfEmpty(0).Max();

            var baseCols = columnHeaders
                    .Where(x => char.IsLetter(x.RangeName.Last()))
                    .OrderBy(h => h.DisplayLine)
                    .ThenBy(h => h.DisplayColumn)
                    .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                    .ToList();

            var baseTotalCols = columnHeaders
                .Where(x => !char.IsLetter(x.RangeName.Last()))
                .OrderBy(h => h.DisplayLine)
                .ThenBy(h => h.DisplayColumn)
                .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                .ToList();

            if ((worksheetName == "C-3 Pt3 Pg1" || worksheetName == "C-3 Pt3 Pg2"))
            {
                foreach (var h in baseTotalCols)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                }
                foreach (var h in baseCols)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                }
            }
            else
            {
                foreach (var h in baseTotalCols)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                }
                for (int dLine = 0; dLine < recordWithMaxDisplayLineNo; dLine++)
                {
                    foreach (var h in baseCols)
                    {
                        int line = dLine + 1;
                        dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(line, h.DisplayColumn, line.ToString(), h.FieldName, h.RangeName, h.DataType));
                    }
                }
            }

            return dynCols;
        }

        public List<(int DisplayLine, int DisplayColumn, string Line, string FName, string RName, int DataType)> BuildDynamicColumnsForOtherSch(
            List<DataWizardReportViewModel> reportData, List<FieldTemplate> columnHeaders, string worksheetName, bool flag = false)
        {
            var dynCols = new List<(int, int, string, string, string, int)>();

            int recordWithMaxDisplayLineNo = !flag
                ? reportData.Max(r => r.DisplayLine)
                : reportData.GroupBy(r => new { r.ProviderCostReportId, r.ProviderNumber, r.CostReportType, r.CostReportStatus })
                    .Select(g => g.Select(x => x.DisplayLine).Distinct().Count())
                    .DefaultIfEmpty(0).Max();

            if (!flag)
            {
                // NFOFI-5757 and NFOFI-5730 have columns that need to be shown with letters at end of range name
                string[] sheetsWithLettersInRangeName = { "E-1 Pt2", "AT7" };
                var baseCols = columnHeaders
                   .Where(x => !char.IsLetter(x.RangeName.Last()) || (sheetsWithLettersInRangeName.Contains(worksheetName)))
                   .OrderBy(h => h.DisplayLine)
                   .ThenBy(h => h.DisplayColumn)
                   .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                   .ToList();

                dynCols = new List<(int, int, string, string, string, int)>();
                foreach (var h in baseCols)
                {
                    dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                }
            }
            else
            {
                var baseCols = columnHeaders
                    .Where(x => char.IsLetter(x.RangeName.Last()))
                    .OrderBy(h => h.DisplayLine)
                    .ThenBy(h => h.DisplayColumn)
                    .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                    .ToList();

                var baseTotalCols = columnHeaders
                    .Where(x => !char.IsLetter(x.RangeName.Last()))
                    .OrderBy(h => h.DisplayLine)
                    .ThenBy(h => h.DisplayColumn)
                    .Select(h => new { h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType })
                    .ToList();

                dynCols = new List<(int, int, string, string, string, int)>();
                if (worksheetName == "E-1" || worksheetName == "E-1 Pt1")
                {
                    int endColLineNum = 22;
                    var endCols = baseTotalCols.Where(x => x.Line == endColLineNum.ToString()).ToList();
                    baseTotalCols.RemoveAll(x => x.Line == endColLineNum.ToString());

                    //Columns with Hard Coded Description first (Except for last cols for line 22)
                    foreach (var h in baseTotalCols)
                    {
                        dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                    }
                    //Columns that user can define description for
                    for (int dLine = 8; dLine < endColLineNum; dLine++)
                    {
                        foreach (var h in baseCols)
                        {
                            dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(dLine, h.DisplayColumn, dLine.ToString(), h.FieldName, h.RangeName, h.DataType));
                        }
                    }
                    //Columns with hard coded description for line 22 go last
                    foreach (var endCol in endCols)
                    {
                        dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(endCol.DisplayLine, endCol.DisplayColumn, endCol.Line, endCol.FieldName, endCol.RangeName, endCol.DataType));
                    }
                }
                else
                {
                    for (int dLine = 1; dLine <= recordWithMaxDisplayLineNo; dLine++)
                    {
                        foreach (var h in baseCols)
                        {
                            dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(dLine, h.DisplayColumn, dLine.ToString(), h.FieldName, h.RangeName, h.DataType));
                        }
                    }
                    foreach (var h in baseTotalCols)
                    {
                        dynCols.Add(((int displayLine, int displayCol, string line, string headerText, string rangeName, int dataType))(h.DisplayLine, h.DisplayColumn, h.Line, h.FieldName, h.RangeName, h.DataType));
                    }
                }
            }
            return dynCols;
        }
        public void CreateExcelWorkSheetReport(
        IWorkbook workbook,
        IWorksheet ws,
            List<DataWizardReportViewModel> reportData,
            List<(int displayLine, int displayCol, string line, string headerText, string rangename, int dataType)> dynCols,
            string wsName = null,
            bool flag = false)
        {
            workbook.CalculationOptions.CalculationMode = ExcelCalculationMode.Manual;
            List<int> colsHoldingMedicaidId = new List<int>();

            List<string> rangeNames_Remove = new List<string>
            {
                "LockedOn","SubmitOn","CountyName"
            };

            // Unique keys (one row per ProviderNumber + CostReportType)
            var uniqueKeys = reportData
                    .Where(x => x.WorksheetName != "C-1" || x.PageNumber != 0)
                    .GroupBy(x => new { x.ProviderCostReportId, x.ProviderNumber, x.CostReportType, x.CostReportStatus })
                    .Select(g => g.Key)
                    .ToList();

            // Build DataTable 
            var dt = new DataTable();
            var displayHeaders = new List<string>();

            // Fixed columns
            dt.Columns.Add("Provider Name", typeof(string)); displayHeaders.Add("Provider Name - 10010");
            dt.Columns.Add("Provider Number", typeof(string)); displayHeaders.Add("Provider Number - 10020");
            dt.Columns.Add("Provider NPI", typeof(string)); displayHeaders.Add("Provider NPI - 10830");
            dt.Columns.Add("Cost Report Year", typeof(string)); displayHeaders.Add("Cost Report Year");
            dt.Columns.Add("Cost Report Type", typeof(string)); displayHeaders.Add("Cost Report Type - 10000");
            dt.Columns.Add("Cost Report Status", typeof(string)); displayHeaders.Add("Cost Report Status");
            dt.Columns.Add("Cost Report Document Number", typeof(string)); displayHeaders.Add("Cost Report Document Number");
            dt.Columns.Add("Cost Report Begin Date", typeof(string)); displayHeaders.Add("Cost Report Begin Date - 10370");
            dt.Columns.Add("Cost Report End Date", typeof(string)); displayHeaders.Add("Cost Report End Date - 10380");

            bool headerlineFlag = SchedulesWithNoHeaderLineNumbers(ws.Name);
            if (SimplifiedType == "NF" && ws.Name == "Att8")
                headerlineFlag = true;

            // Dynamic columns: show duplicate header text; ensure unique internal names
            foreach (var dc in dynCols)
            {
                var display = string.Empty;
                if (wsName == "C-1")
                {
                    if (dc.displayLine == 0)
                    {
                        display = dc.headerText;
                        if (dc.line != "0") //NFOFI-5731: Remove "(Line 0)" from headers
                            display += " (Line " + dc.line + ")";
                    }
                    else
                        display = dc.headerText;
                }
                else if (wsName == "A")
                {
                    if (rangeNames_Remove.Contains(dc.rangename))
                    {
                        display = !headerlineFlag ? dc.headerText : (dc.headerText + " (Line " + dc.line + ")");
                    }
                    else
                    {
                        display = !headerlineFlag ? dc.headerText + " - " + dc.rangename : (dc.headerText + " - " + dc.rangename + " (Line " + dc.line + ")");
                    }
                }
                else if (GetScheduleswithChartofAccount().Contains(wsName)) {
                        display = !headerlineFlag ? dc.headerText : (dc.headerText + " (Line " + dc.line + ")");
                }
                else display = !headerlineFlag ? dc.headerText + " - " + dc.rangename : (dc.headerText + " - " + dc.rangename + " (Line " + dc.line + ")");

                AddUniqueColumn(dt, display, typeof(string));
                displayHeaders.Add(display);
            }

            if (wsName != "")
            {
                var lookup = reportData
                .GroupBy(r => new { r.ProviderNumber, r.ProviderCostReportId, r.CostReportType, r.CostReportStatus, r.DisplayLine, r.DisplayColumn, r.Line, r.FieldName })
                .ToDictionary(g => g.Key, g => g.First());

                // Fill DataTable rows
                foreach (var u in uniqueKeys)
                {
                    // any one row for fixed columns:
                    var groupedByKey = reportData
                        .Where(x => x.ProviderCostReportId == u.ProviderCostReportId
                            && x.ProviderNumber == u.ProviderNumber
                            && x.CostReportType == u.CostReportType
                            && x.CostReportStatus == u.CostReportStatus)
                        .ToList();
                    var first = groupedByKey.First(x => x.ProviderCostReportId == u.ProviderCostReportId
                            && x.ProviderNumber == u.ProviderNumber
                            && x.CostReportType == u.CostReportType
                            && x.CostReportStatus == u.CostReportStatus);

                    var dr = dt.NewRow();
                    dr[0] = first.ProviderName ?? "";
                    dr[1] = first.ProviderNumber == null ? "" : first.ProviderNumber.ToString();
                    dr[2] = first.ProviderNPI == null ? "" : first.ProviderNPI.ToString();
                    dr[3] = first.CostReportYear.ToString() == null ? "" : first.CostReportYear.ToString();
                    dr[4] = first.CostReportType == null ? "" : first.CostReportType.ToString();
                    dr[5] = first.CostReportStatus == null ? "" : first.CostReportStatus.ToString();
                    dr[6] = first.DocumentNumber == null ? "" : first.DocumentNumber.ToString();
                    dr[7] = first.CostReportBeginDate.HasValue ? first.CostReportBeginDate.Value.ToString("MM/dd/yyyy") : "";
                    dr[8] = first.CostReportEndDate.HasValue ? first.CostReportEndDate.Value.ToString("MM/dd/yyyy") : "";

                    int i = 0;
                    if (wsName == "A")
                    {
                        foreach (var col in dynCols)
                        {    
                            if(col.rangename == "10020")
                            {// Set Column Format for Medicaid Number with leading zeros
                                colsHoldingMedicaidId.Add(1);
                                colsHoldingMedicaidId.Add(9 + i);
                            }
                            var match = groupedByKey.FirstOrDefault(g => string.Equals(g.RangeName, col.rangename, StringComparison.OrdinalIgnoreCase));

                            dr[9 + i] = (match?.DataValue != null) ? ToExcelString(match.DataValue, col.dataType) : "";

                            i++;
                        }
                    }
                    else if (wsName == "OtherBal" || wsName == "AT7" || wsName == "AT11" || wsName == "AT11 Pt1" || wsName == "AT11 Pt2 Pg1")
                    {
                        // dynamic cells
                        for (int j = 0; j < dynCols.Count; j++)
                        {
                            var item = dynCols[j];
                            var k = new { u.ProviderNumber, u.ProviderCostReportId, u.CostReportType, u.CostReportStatus, DisplayLine = item.displayLine, DisplayColumn = item.displayCol, Line = item.line, FieldName = item.headerText };

                            string cellText = "";
                            if (lookup.ContainsKey(k))
                            {
                                var rec = lookup[k];
                                if (rec != null && rec.DataValue != null)
                                {
                                    cellText = ToExcelString(rec.DataValue, item.dataType);
                                    if (item.dataType == (int)DataType.MedicaidId)
                                        colsHoldingMedicaidId.Add(j + 9);
                                }
                            }
                            dr[9 + j] = cellText;
                        }
                    }
                    else
                    {
                        // dynamic cells
                        for (int j = 0; j < dynCols.Count; j++)
                        {
                            var item = dynCols[j];
                            var k = new { u.ProviderNumber, u.ProviderCostReportId, u.CostReportType, u.CostReportStatus, DisplayLine = item.displayLine, DisplayColumn = item.displayCol, Line = item.line, FieldName = item.headerText };

                            string cellText = "";
                            if (item.line == "1" && wsName == "C-1") //NOFIF-5731: C-1 needs Base Allowance added to report (this is hardcoded in HTML so we need to hardcode here for now)
                                cellText = "100";
                            else if (lookup.ContainsKey(k))
                            {
                                var rec = lookup[k];
                                if (rec != null && rec.DataValue != null)
                                {
                                    cellText = ToExcelString(rec.DataValue, item.dataType);
                                    if (item.dataType == (int)DataType.MedicaidId)
                                        colsHoldingMedicaidId.Add(j + 9);
                                }
                            }
                            dr[9 + j] = cellText;
                        }
                    }
                    dt.Rows.Add(dr);
                }
            }
            else
            {
                var lookup = reportData
                .GroupBy(r => new { r.ProviderNumber, r.ProviderCostReportId, r.CostReportType, r.CostReportStatus, r.DisplayLine, r.DisplayColumn })
                .ToDictionary(g => g.Key, g => g.First());

                var groupedByKey = reportData
                    .GroupBy(r => new { r.ProviderCostReportId, r.ProviderNumber, r.CostReportType, r.CostReportStatus })
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Fill DataTable rows
                foreach (var key in uniqueKeys)
                {
                    var rows = groupedByKey[key];
                    var first = rows[0];

                    var dr = dt.NewRow();
                    dr[0] = first.ProviderName ?? "";
                    dr[1] = first.ProviderNumber == null ? "" : first.ProviderNumber.ToString();
                    dr[2] = first.ProviderNPI == null ? "" : first.ProviderNPI.ToString();
                    dr[3] = first.CostReportYear.ToString() == null ? "" : first.CostReportYear.ToString();
                    dr[4] = first.CostReportType == null ? "" : first.CostReportType.ToString();
                    dr[5] = first.CostReportStatus == null ? "" : first.CostReportStatus.ToString();
                    dr[6] = first.DocumentNumber == null ? "" : first.DocumentNumber.ToString();
                    dr[7] = first.CostReportBeginDate.HasValue ? first.CostReportBeginDate.Value.ToString("MM/dd/yyyy") : "";
                    dr[8] = first.CostReportEndDate.HasValue ? first.CostReportEndDate.Value.ToString("MM/dd/yyyy") : "";

                    // dynamic cells
                    for (int i = 0; i < dynCols.Count; i++)
                    {
                        var item = dynCols[i];
                        var k = new { key.ProviderNumber, key.ProviderCostReportId, key.CostReportType, key.CostReportStatus, DisplayLine = item.displayLine, DisplayColumn = item.displayCol };

                        string cellText = "";
                        
                         if (lookup.ContainsKey(k))
                        {
                            var rec = lookup[k];
                            //Schedule AT9 Pt1 Pg1: Is facility owned or rented:Fair Rental Value Survey needs to display 0 = Rented or 1 = Owned.  
                            if (rec.DisplayLine == 0 && rec.DisplayColumn == 3 && rec.WorksheetName == "AT9 Pt1 Pg1")
                            {
                                if (rec.DataValue == "0")
                                    cellText = "Rented";
                                else if(rec.DataValue == "1")
                                    cellText = "Owned";
                            }
                            else if (rec != null && rec.DataValue != null)
                            {
                                cellText = ToExcelString(rec.DataValue, item.dataType);
                                if (item.dataType == (int)DataType.MedicaidId)
                                    colsHoldingMedicaidId.Add(i + 9);
                            }

                        }
                        dr[9 + i] = cellText;
                    }

                    dt.Rows.Add(dr);
                }
            }

            // Write headers + bulk import (no DuplicateNameException, headers may repeat)
            ExportWithDisplayHeaders(ws, dt, displayHeaders, 1, 1);

            // Fast formatting 
            int totalCols = dt.Columns.Count;
            for (int c = 1; c <= totalCols; c++)
            {
                ws.Columns[c - 1].ColumnWidth = 25;
            }

            // Number formats for fixed columns
            ws.Columns[0].NumberFormat = "Text";
            ws.Columns[1].NumberFormat = "0000000";
            ws.Columns[2].NumberFormat = "000000000";
            ws.Columns[3].NumberFormat = @"###0_);(###0)";
            //ws.Columns[4].NumberFormat = "@";
            ws.Columns[5].NumberFormat = "Text";
            ws.Columns[6].NumberFormat = "0";
            ws.Columns[7].NumberFormat = @"mm\/dd\/yyyy";
            ws.Columns[8].NumberFormat = @"mm\/dd\/yyyy";

            foreach (int index in colsHoldingMedicaidId.Distinct())
                ws.Columns[index].NumberFormat = "000000#";

            ws.PageSetup.Orientation = ExcelPageOrientation.Landscape;
        }

        private static bool SchedulesWithNoHeaderLineNumbers(string worksheetName)
        {
            bool headerlineFlag;
            switch (worksheetName)
            {
                case "Other Balance":
                case "A":
                case "C-2 Pt1":
                case "C-2 Pt2":
                case "C-3 Pt1":
                case "C-3 Pt2 Pg1":
                case "C-3 Pt2 Pg2":
                case "C-3 Pt2 Pg3":
                case "C-3 Pt3 Pg1":
                case "C-3 Pt3 Pg2":
                case "C-3 Pt3 Pg3":
                case "D-2":
                case "Att4":
                case "Att8":
                case "Att9 Pt1 Pg1":
                case "Att11":
                case "Att11 Pt1":
                case "Att11 Pt2 Pg1":
                case "Att11 Pt2 Pg2":
                    headerlineFlag = false;
                    break;
                default:
                    headerlineFlag = true;
                    break;
            }

            return headerlineFlag;
        }

        private static bool SelectByProviderEffectiveDate(bool? activeState, List<MSLC.ApplicationProxy.Interfaces.Models.EffectiveDate> dates)
        {
            // If "All" was selected, return all.
            if (!activeState.HasValue) return true;

            // If there are no dates and we are looking for inactive - this is it.
            if (dates.Count == 0 && !activeState.Value) return true;

            // If active, then return those that are within the effective dates.
            bool active = dates.Any(d => d.DateEffective < DateTime.Now && (d.EndDate == null || d.EndDate > DateTime.Now));
            return active == activeState.Value;
        }
        #endregion

        #region Helper Functions

        private List<SelectListItem> GetProviderIdentifiersSelectList()
        {
            List<SelectListItem> providerIdentifiers;
            using (var providerProxy = MSLC.Portal.Utility.Proxies.ARUMProvider.GetARUMProviderServiceProxy(RetrieveSession.Classification))
            {
                providerIdentifiers = providerProxy.GetAllCodes(RetrieveSession.Classification.ARUMToken, "ProviderIdentifier", "StatLogUploadDownloadReport")
                    ?.Codes.OrderByDescending(c => c.Order)
                    .Select(c => new SelectListItem { Selected = false, Text = c.Name, Value = c.Value, Disabled = !c.IsActive })
                    .ToList();
            }
            return providerIdentifiers;
        }
        
        private MemoryStream CreateExcelFile(DataTable dataTable, string worksheetName)
        {
            ExcelEngine xlEngine = new ExcelEngine();

            Syncfusion.XlsIO.IApplication xlApp = xlEngine.Excel;
            IWorkbook xlWorkbook = xlApp.Workbooks.Create(1);
            xlWorkbook.Version = ExcelVersion.Excel2016;
            IWorksheet worksheet = xlWorkbook.Worksheets[0];

            //Field Descriptions
            worksheet.ImportDataTable(dataTable, true, 1, 1);


            #region Buisness requested styles

            IStyle style = xlWorkbook.Styles.Add("MSLC Styling");
            style.Font.Bold = true;
            style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            style.Color = Syncfusion.Drawing.Color.FromArgb(122, 193, 66);
            // style.FillPattern = ExcelPattern.DarkUpwardDiagonal;
            worksheet.Rows[0].CellStyle = style;
            if(worksheet.Rows.Length > 1) // if it has any data, free the first row
                worksheet.Rows[1].FreezePanes();
            worksheet.AutoFilters.FilterRange = worksheet.UsedRange;

            #endregion
            
            worksheet.Name = worksheetName;
            worksheet.UsedRange.ColumnWidth = 255d;
            worksheet.UsedRange.RowHeight = 13.2d;
            worksheet.UsedRange.AutofitRows();
            worksheet.UsedRange.AutofitColumns();


            // Set up Report Payload object
            var memoryStream = new MemoryStream();

            // Save workbook to 
            xlWorkbook.SaveAs(memoryStream);

            //close the new workbook.
            xlWorkbook.Close();

            //Dispose the ExcelEngine object
            xlEngine.Dispose();

            return memoryStream;
        }

        private void DeleteEditingReport()
        {
            string tempReportGuid = Session[SESSION_KEYS_REPORTS.DW_TEMP_REPORT_GUID]?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(tempReportGuid))
            {
                // We need to clear out old EDITING report
                Guid.TryParse(tempReportGuid, out Guid reportTempGuid);
                ReportBL.DeleteEditingReport(reportTempGuid);
            }
            Session[SESSION_KEYS_REPORTS.DW_TEMP_REPORT_GUID] = null;
        }

        private Guid SaveDataPointReports(int costReportYear, string costReportLength, string costReportTypeCode, string costReportStatus, string reportName)
        {
            MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
            
            Guid tempReportId = Guid.NewGuid();
            DataPointReports dataPointDataReports = new DataPointReports()
            {
                CRYear = costReportYear,
                CRLength = costReportLength,
                CRType = costReportTypeCode,
                CRStatus = costReportStatus,
                ReportName = reportName,
                TempId = tempReportId,
                CreatedOn = DateTime.Now,
                CreatedBy = $"{myUser.FirstName} {myUser.LastName}",
                LastUpdatedOn = DateTime.Now,
                LastUpdateBy = $"{myUser.FirstName} {myUser.LastName}"
            };

            ReportBL.SaveDataPointsReport(dataPointDataReports);
            Session[SESSION_KEYS_REPORTS.DW_TEMP_REPORT_GUID] = tempReportId;
            return tempReportId;
        }

        #endregion
    }

    // (ProviderNumber + CostReportType) in a section
    public sealed class GroupKey
    {
        public int ProviderCostReportId { get; set; }
        public string ProviderNumber { get; set; }
        public string CostReportType { get; set; }
        public string CostReportStatus { get; set; }
        public int DistinctLineCount { get; set; }
        public int DistinctColumnCount { get; set; }
    }

    // Profile describing how a sheet is partitioned
    public sealed class SheetProfile
    {
        public List<(int Start, int End)> SectionRanges { get; set; }
        public List<int> TotalLines { get; set; }
    }

   }

