using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using MSLC.ArumProxy.Interfaces;
using MSLC.BulkProcessing.BL;
using MSLC.BulkProcessing.DAL.BulkProcessing;
using MSLC.BulkProcessing.DAL.BulkProcessing.Context;
using MSLC.BulkProcessing.DTO;
using MSLC.BulkProcessing.EXT;
using MSLC.FileProxy;
using MSLC.FileProxy.Interfaces;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.StatLog.Configuration;
using MSLC.StatLog.Controllers;
using Newtonsoft.Json.Linq;
using Omu.ValueInjecter;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using WebPortal_OH_FI.App_Start.List;
using WebPortal_OH_FI.Common.Extensions;
using WebPortal_OH_FI.Models.ViewModels;
using WebPortal_OH_FI.Web;
using static WebPortal_OH_FI.App_Start.List.MDSReportOptions;
using static WebPortal_OH_FI.Models.ViewModels.ViewStatusViewModel;

namespace WebPortal_OH_FI.Controllers
{
    public class BulkProcessingController : LoggedInBaseController
    {
        private static string SimplifiedType =>
            Common.Extensions.ClassificationDTOExtensions.GetSimplifiedType(RetrieveSession.Classification);

        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("BulkProcessing:View")]
        public ActionResult Index()
        {
            return View(GetAllJobs("Bulk Upload"));
        }

        [ARUMFeatureActionFilterAttribute("BulkProcessing:View")]
        public ActionResult ViewStatus(Guid jobID, string jobName)
        {
            return View(GetJobViewStatus(jobID, jobName));
        }

        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public ActionResult MDSReportJobs()
        {
            return View(GetAllMDSReportJobs());
        }

        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Reports:MDSResidentListing")]
        public ActionResult MDSResidentListingJobs()
        {
            return View(GetAllMDSResidentListingJobs());
        }

        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public ActionResult ViewMDSStatus(Guid jobID, string jobName)
        {
            var jobIDString = ClientMapping.GetInternalIdFromClientId(jobID.ToString(), "BulkProcessingJobID");
            return View(GetMDSJobViewStatus(Guid.Parse(jobIDString), jobName));
        }

        [ARUMFeatureActionFilterAttribute("Reports:MDSResidentListing")]
        public ActionResult ViewMDSResidentListingStatus(Guid jobID, string jobName)
        {
            var jobIDString = ClientMapping.GetInternalIdFromClientId(jobID.ToString(), "BulkProcessingJobID");
            return View(GetMDSResidentListingJobViewStatus(Guid.Parse(jobIDString), jobName));
        }

        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Reports:SectionQ")]
        public ActionResult SectionQReportJobs()
        {
            List<SectionQViewModel> SectionQJobs = new List<SectionQViewModel>();

            List<JobEXT> bulkJobs = JobBL.GetJobDashboardInformation(ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification), "Generate Section Q Report")?.OrderByDescending(j => j.Job.CreateDate).ToList();

            foreach (var job in bulkJobs)
            {
                var reportItem = job.Items.FirstOrDefault();

                var model = new SectionQViewModel
                {
                    ID = ClientMapping.MapInternalIdToClientId(job.Job.ID.ToString(), "BulkProcessingJobID"),
                    JobName = job.Job.Name,
                    ReportType = reportItem.Attributes.FirstOrDefault(a => a.Key == "ReportType")?.Value ?? "N/A",
                    ReportVersion = reportItem.Attributes.FirstOrDefault(a => a.Key == "ReportVersion")?.Value ?? "N/A",
                    CLSProvGrouping = reportItem.Attributes.FirstOrDefault(a => a.Key == "CLSProvGrouping")?.Value ?? "N/A",
                    ReportEndDateString = reportItem.Attributes.FirstOrDefault(a => a.Key == "EndingDate")?.Value,
                    ReportEndDate = reportItem.Attributes.FirstOrDefault(a => a.Key == "EndingDate") != null ? DateTime.Parse(reportItem.Attributes.FirstOrDefault(a => a.Key == "EndingDate").Value) : (DateTime?)null,
                    CreatedBy = job.Job.CreatedBy,
                    CreateDate = job.Job.CreateDate,
                    Downloadable = reportItem.Item.FileGuid != null && reportItem.Item.FileGuid != Guid.Empty,
                    Error = reportItem.LastStatus?.StatusHistory.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ?? true
                };

                SectionQJobs.Add(model);
            }

            return View(SectionQJobs);
        }

        [ARUMFeatureActionFilterAttribute("Reports:SectionQ")]
        public ActionResult ViewSectionQStatus(Guid jobID, string jobName)
        {
            var jobIDString = ClientMapping.GetInternalIdFromClientId(jobID.ToString(), "BulkProcessingJobID");
            return View(GetJobViewStatus(Guid.Parse(jobIDString), jobName));
        }

        [ARUMFeatureActionFilterAttribute("Reports:SectionQ")]
        public ActionResult DownloadSectionQ(string jobIDString)
        {
            var mappedJobID = ClientMapping.GetInternalIdFromClientId(jobIDString, "BulkProcessingJobID");
            if (mappedJobID == null || !Guid.TryParse(mappedJobID, out Guid jobID))
            {
                TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                return RedirectToAction("SectionQReportJobs");
            }
            var arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);
            using (var db = new BulkProcessing_Context(arumConnectionString))
            {
                var fileID = db.Item
                    .FirstOrDefault(item => item.JobID.Equals(jobID) &&
                                            item.Type.Equals("Request"))?.FileGuid;
                if (fileID == null)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("SectionQReportJobs");
                }

                var _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
                var response = _fileServiceProxy.GetFileByID(RetrieveSession.ARUMUser.UserID, fileID.Value,
                    "BulkProcessing", "DownloadSectionQ",
                    "BulkProcessing/DownloadSectionQ");

                if (!response.Results.Successful)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("SectionQReportJobs");
                }

                var filename = $"SectionQ_{SimplifiedType} Report - {DateTime.Now:yyyy-dd-M--HH-mm-ss}.zip";

                return File(response.myFile, @"application/zip", filename);
            }
        }

        [ARUMFeatureActionFilterAttribute("Reports:MDS")]
        public ActionResult DownloadMDSReport(string jobIDString)
        {
            var mappedJobID = ClientMapping.GetInternalIdFromClientId(jobIDString, "BulkProcessingJobID");
            if (mappedJobID == null || !Guid.TryParse(mappedJobID, out Guid jobID))
            {
                TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                return RedirectToAction("MDSReportJobs");
            }
            var arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);
            using (var db = new BulkProcessing_Context(arumConnectionString))
            {
                var item = db.Item
                    .Where(i => i.JobID.Equals(jobID) && i.Type.Equals("Request"))
                    .Select(i => new
                    {
                        Item = i,
                        Attributes = i.Attributes
                    }).ToList()
                    .Select(model => new ItemWithAttributesEXT
                    {
                        Item = new ItemDTO{
                            ID = model.Item.ID,
                            ParentID = model.Item.ParentID,
                            JobID = model.Item.JobID,
                            FileGuid = model.Item.FileGuid,
                            Name = model.Item.Name,
                            Type = model.Item.Type
                        },
                        Attributes = model.Attributes.Select(ia => new ItemAttributeDTO{
                            ID = ia.ID,
                            ItemID = ia.ItemID,
                            Key = ia.Key,
                            Value = ia.Value
                        }).ToList()
                    }).FirstOrDefault();

                if (item == null)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("MDSReportJobs");
                }

                //Verify user has access to report option before downloading
                var reportOptions = MDSReportOptions.GetReportOptionsForUserRoles(RetrieveSession.Classification, RetrieveSession.ARUMUser.Roles);
                if (reportOptions.FirstOrDefault(r => item.Attributes.FirstOrDefault(a => a.Key == "ReportType")?.Value == $"{r.Version} - {r.Name}"
                    && item.Attributes.FirstOrDefault(a => a.Key == "ReportPeriod")?.Value == r.Period) == null)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("MDSReportJobs");
                }

                var fileID = item?.Item.FileGuid;

                if (fileID == null)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("MDSReportJobs");
                }

                var _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
                var response = _fileServiceProxy.GetFileByID(RetrieveSession.ARUMUser.UserID, fileID.Value,
                    "BulkProcessing", "DownloadMDSReport",
                    "BulkProcessing/DownloadMDSReport");

                if (!response.Results.Successful)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("MDSReportJobs");
                }
                
                var filename = $"{item.Attributes.FirstOrDefault(a => a.Key == "ReportType")?.Value}_{item.Attributes.FirstOrDefault(a => a.Key == "ReportPeriod")?.Value} Report - {item.Attributes.FirstOrDefault(a => a.Key == "EndingDate")?.Value}.zip";

                return File(response.myFile, @"application/zip", filename);
            }
        }

        [ARUMFeatureActionFilterAttribute("Reports:MDSResidentListing")]
        public ActionResult DownloadMDSResidentListingReport(string jobIDString)
        {
            var mappedJobID = ClientMapping.GetInternalIdFromClientId(jobIDString, "BulkProcessingJobID");
            if (mappedJobID == null || !Guid.TryParse(mappedJobID, out Guid jobID))
            {
                TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                return RedirectToAction("MDSResidentListingJobs");
            }
            var arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);
            using (var db = new BulkProcessing_Context(arumConnectionString))
            {
                var item = db.Item
                    .Where(i => i.JobID.Equals(jobID) && i.Type.Equals("Request"))
                    .Select(i => new
                    {
                        Item = i,
                        Attributes = i.Attributes
                    })
                    .Select(model => new ItemWithAttributesEXT
                    {
                        Item = new ItemDTO
                        {
                            ID = model.Item.ID,
                            ParentID = model.Item.ParentID,
                            JobID = model.Item.JobID,
                            FileGuid = model.Item.FileGuid,
                            Name = model.Item.Name,
                            Type = model.Item.Type
                        },
                        Attributes = model.Attributes.Select(ia => new ItemAttributeDTO
                        {
                            ID = ia.ID,
                            ItemID = ia.ItemID,
                            Key = ia.Key,
                            Value = ia.Value
                        }).ToList()
                    }).FirstOrDefault();

                if (item == null)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("MDSResidentListingJobs");
                }

                var fileID = item?.Item.FileGuid;

                if (fileID == null)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("MDSResidentListingJobs");
                }

                var _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
                var response = _fileServiceProxy.GetFileByID(RetrieveSession.ARUMUser.UserID, fileID.Value,
                    "BulkProcessing", "DownloadMDSResidentListingReport",
                    "BulkProcessing/DownloadMDSResidentListingReport");

                if (!response.Results.Successful)
                {
                    TempData["Message"] = "Unable to get file from selected job. Please try again later or contact support.";
                    return RedirectToAction("MDSResidentListingJobs");
                }

                var filename = $"{item.Attributes.FirstOrDefault(a => a.Key == "ReportType")?.Value} Report.zip";

                return File(response.myFile, @"application/zip", filename);
            }
        }

        #region Helper Functions
        private List<BulkProcessingViewModel> GetAllJobs(string jobName)
        {
            return JobBL.GetAllJobs(ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification))
                .Select(j => new BulkProcessingViewModel
                {
                    ID = j.ID,
                    JobName = j.Name,
                    CreateDate = j.CreateDate,
                    CreatedBy = j.CreatedBy,

                })
                .Where(j => j.JobName.Equals(jobName))
                .OrderByDescending(j => j.CreateDate).ToList();
        }

        private List<MDSReportViewModel> GetAllMDSReportJobs()
        {
            List<MDSReportViewModel> MDSJobs = new List<MDSReportViewModel>();

            List<JobEXT> bulkJobs = JobBL.GetJobDashboardInformation(ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification), "Generate MDS Report")?.OrderByDescending(j => j.Job.CreateDate).ToList();
            var reportOptions = MDSReportOptions.GetReportOptionsForUserRoles(RetrieveSession.Classification, RetrieveSession.ARUMUser.Roles);

            foreach (var job in bulkJobs)
            {
                var reportItem = job.Items.FirstOrDefault();

                if (reportOptions.FirstOrDefault(r => reportItem.Attributes.FirstOrDefault(a => a.Key == "ReportType")?.Value == $"{r.Version} - {r.Name}"
                    && reportItem.Attributes.FirstOrDefault(a => a.Key == "ReportPeriod")?.Value == r.Period) == null)
                        continue;

                var model = new MDSReportViewModel
                {
                    ID = ClientMapping.MapInternalIdToClientId(job.Job.ID.ToString(), "BulkProcessingJobID"),
                    JobName = job.Job.Name,
                    ReportName = reportItem.Attributes.FirstOrDefault(a => a.Key == "ReportType")?.Value,
                    ReportingPeriod = reportItem.Attributes.FirstOrDefault(a => a.Key == "ReportPeriod")?.Value,
                    ReportExtension = reportItem.Attributes.FirstOrDefault(a => a.Key == "ReportExtension")?.Value,
                    ReportEndDateString = reportItem.Attributes.FirstOrDefault(a => a.Key == "EndingDate")?.Value,
                    ReportEndDate = reportItem.Attributes.FirstOrDefault(a => a.Key == "EndingDate") != null ? DateTime.Parse(reportItem.Attributes.FirstOrDefault(a => a.Key == "EndingDate").Value) : (DateTime?)null,
                    CreatedBy = job.Job.CreatedBy,
                    CreateDate = job.Job.CreateDate,
                    Downloadable = reportItem.Item.FileGuid != null && reportItem.Item.FileGuid != Guid.Empty,
                    Error = reportItem.LastStatus?.StatusHistory.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ?? true
                };

                MDSJobs.Add(model);
            }

            return MDSJobs;
        }

        private List<MDSResidentListingViewModel> GetAllMDSResidentListingJobs()
        {
            List<MDSResidentListingViewModel> MDSJobs = new List<MDSResidentListingViewModel>();

            List<JobEXT> bulkJobs = JobBL.GetJobDashboardInformation(ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification), "Generate MDS Resident Listing Report")?.OrderByDescending(j => j.Job.CreateDate).ToList();

            foreach (var job in bulkJobs)
            {
                var reportItem = job.Items.FirstOrDefault();

                var model = new MDSResidentListingViewModel
                {
                    ID = ClientMapping.MapInternalIdToClientId(job.Job.ID.ToString(), "BulkProcessingJobID"),
                    JobName = job.Job.Name,
                    ReportName = reportItem.Attributes.FirstOrDefault(a => a.Key == "ReportType")?.Value,
                    CreatedBy = job.Job.CreatedBy,
                    CreateDate = job.Job.CreateDate,
                    Downloadable = reportItem.Item.FileGuid != null && reportItem.Item.FileGuid != Guid.Empty,
                    Error = reportItem.LastStatus?.StatusHistory.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ?? true
                };

                MDSJobs.Add(model);
            }

            return MDSJobs;
        }

        private static StatusHistoryEXT CreateLastStatus(StatusHistory lastStatus, List<ICollection<Message>> statusMessages)
        {
            var statusHistory = lastStatus == null ? null : new StatusHistoryDTO().InjectFrom(lastStatus) as StatusHistoryDTO;
            var messages = statusMessages?.FirstOrDefault()?.Select(m => new MessageDTO().InjectFrom(m) as MessageDTO).ToList();
            return new StatusHistoryEXT
            {
                StatusHistory = statusHistory,
                Messages = messages
            };
        }

        private ViewStatusViewModel GetJobViewStatus(Guid jobID, string jobName)
        {
            string arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);

            return new ViewStatusViewModel
            {
                JobName = jobName,
                Items = ItemBL.GetItemsByJobID(arumConnectionString, jobID)
                    .Select(j => new ViewStatusViewModel.ItemViewModel
                    {
                        ID = j.Item.ID,
                        ParentID = j.Item.ParentID,
                        Status = j.LastStatus?.StatusHistory?.Status,
                        FileName = j.Item.Name,
                        CreateDate = j.Item.CreateDate,
                        CreatedBy = j.Item.CreatedBy,
                        FirstMessage = j.LastStatus?.Messages?.FirstOrDefault()?.Text ?? ""
                    }).ToList()
            };
        }

        private ViewStatusViewModel GetMDSJobViewStatus(Guid jobID, string jobName)
        {
            string arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);
            var reportOptions = MDSReportOptions.GetReportOptionsForUserRoles(RetrieveSession.Classification, RetrieveSession.ARUMUser.Roles);

            return new ViewStatusViewModel
            {
                JobName = jobName,
                Items = ItemBL.GetItemsByJobID(arumConnectionString, jobID).Where(i => reportOptions.Any(r => i.Attributes.FirstOrDefault(a => a.Key == "ReportType")?.Value == $"{r.Version} - {r.Name}"
                && i.Attributes.FirstOrDefault(a => a.Key == "ReportPeriod")?.Value == r.Period))
                    .Select(j => new ViewStatusViewModel.ItemViewModel
                    {
                        ID = j.Item.ID,
                        ParentID = j.Item.ParentID,
                        Status = j.LastStatus?.StatusHistory?.Status,
                        FileName = j.Item.Name,
                        CreateDate = j.Item.CreateDate,
                        CreatedBy = j.Item.CreatedBy,
                        FirstMessage = j.LastStatus?.Messages?.FirstOrDefault()?.Text ?? ""
                    }).ToList()
            };
        }

        private ViewResidentStatusViewModel GetMDSResidentListingJobViewStatus(Guid jobID, string jobName)
        {
            string arumConnectionString = ARUMConfiguration.GetARUMConnectionString(RetrieveSession.Classification);
            var item = ItemBL.GetItemsByJobID(arumConnectionString, jobID).FirstOrDefault();
            var providerAttribute = item.Attributes.FirstOrDefault(a => a.Key == "ProviderIDs")?.Value;
            var providerIDs = providerAttribute.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var _applicationServiceProxy = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);
            var providerList = _applicationServiceProxy.GetAllActiveProvidersFromListWithParameters(RetrieveSession.Classification.ARUMToken, providerIDs.Select(i => Int32.Parse(i)).ToList(), new List<ARUMWebServiceApplication.WebParameter>
            {
                new ARUMWebServiceApplication.WebParameter
                {
                    Key = "includeIdentifiers",
                    Value = "true",
                    DataType = "bool"
                }
            }, "Statlog.ProviderBL.GetAllARUMProviderInfoByARUMUserID")
            .lstProviders;

            var viewmodel = new ViewResidentStatusViewModel
            {
                JobName = jobName
            };

            viewmodel.Items = providerList.Select(p => new ViewResidentStatusViewModel.ItemViewModel
            {
                ID = item.Item.ID,
                ParentID = item.Item.ParentID,
                Status = item.LastStatus?.StatusHistory?.Status,
                ProviderName = p.Name,
                ProviderMedicareID = p.Identifiers.FirstOrDefault(i => i.Type.ToUpper() == "MEDICAREID")?.Value ?? "",
                CreateDate = item.Item.CreateDate,
                CreatedBy = item.Item.CreatedBy,
                FirstMessage = item.LastStatus?.Messages?.FirstOrDefault()?.Text ?? ""
            }).ToList();

            return viewmodel;
        }
        #endregion
    }
}