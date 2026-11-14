// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ganss.Xss;
using MSLC.ArumProxy.Models;
using MSLC.FileProxy;
using MSLC.FileProxy.Interfaces;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.File;
using MSLC.Portal.Utility.Proxies;
using MSLC.Portal.Utility.String;
using MSLC.StatLog.BL.AuditType;
using MSLC.StatLog.BL.CostReportHistory;
using MSLC.StatLog.Helpers;
using MSLC.StatLog.MVC.MVC;
using MSLC.StatLog.MVC.ViewModels;
using MSLC.StatLog.UI.Models;

namespace MSLC.StatLog.Controllers
{
    public class TransferController : LoggedInBaseController
    {
        /// <summary>
        /// Retrieves the files from the file store
        /// </summary>
        /// <returns>Returns zipped file</returns>
        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult RequestFiles(DownloadFilesViewModel.Download viewModel)
        {
            int costReportID = ClientMappingStatLog.ToInternalID(viewModel.sCostReportID, StatLogScopes.COST_REPORT_ID_STATLOG);
            var selectedFiles = viewModel.Files.Where(f => f.Selected)
                .Select(f => ClientMappingStatLog.ToInternalIDAsGuid(f.FileID, StatLogScopes.FILE_ID_STATLOG))
                .ToList();

            StatLogMapping.Mapping map = StatLogMapping.MapByCostReportID(costReportID, "Statlog:View");
            if (map.Error)
                return Json(new
                {
                    Success = false,
                    Message = map.ErrorMessage
                }, JsonRequestBehavior.AllowGet);

            ARUMProxy_User myContact = RetrieveSession.ARUMUser;
            bool isPHISafe = ProviderUserHelper.IsProviderUserPhiSafe(map.Provider.ARUMProviderID, RetrieveSession.ARUMUser);
            
            var costReportHistory = CostReportHistoryBL.GetHistoryByCostReportID(RetrieveSession.Classification, map.StatLog.CRID,
                    myContact.UserID, "Transfer", "DownloadFiles", (int) map.StatLog.ARUMProviderID);

            var selectedFileIDs = costReportHistory
                .Where(history => history.Viewable(new[] { RetrieveSession.CurrentRole })
                                  && history.Downloadable(new[] { RetrieveSession.CurrentRole }, isPHISafe)
                                  && history.FileFoundInARUM
                                  && history.IsActive
                                  && selectedFiles.Contains(history.FileID))
                .Select(history => (Guid) history.FileID).ToList();
            
            var sourcefiles = new List<SourceFile>();
            FileServiceProxy fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
            foreach (Guid selectedFileID in selectedFileIDs)
            {
                ISingleFileResult myDownloadedFile = fileServiceProxy.GetFileByID(RetrieveSession.ARUMUser.UserID,
                    selectedFileID, "Transfer - Request Files", "RequestFiles", "Transfer");

                if (myDownloadedFile == null ||
                    string.IsNullOrEmpty(myDownloadedFile.FileName) ||
                    myDownloadedFile.myFile == null ||
                    myDownloadedFile.myFile.Length <= 0)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Error: File cannot be retrieved at this time, please contact support."
                    }, JsonRequestBehavior.AllowGet);
                }

                sourcefiles.Add(new SourceFile
                {
                    Name = myDownloadedFile.FileName.Trim(),
                    Extension = myDownloadedFile.Extension,
                    FileBytes = myDownloadedFile.myFile
                });
            }
            
            string auditTypeDesc = AuditTypeBL.GetNameByAuditType(RetrieveSession.Classification, map.StatLog.AuditType) ?? "";
            auditTypeDesc = auditTypeDesc.RemoveInvalidChars().Replace(".", "").Replace(" ", "_");

            var fileGuid = Guid.NewGuid();
            TempData[$"{myContact.LastName}_{myContact.FirstName}:{fileGuid}:fileContents"] = Zip.Create(sourcefiles).filebytes;
            TempData[$"{myContact.LastName}_{myContact.FirstName}:{fileGuid}:contentType"] = "application/zip";
            TempData[$"{myContact.LastName}_{myContact.FirstName}:{fileGuid}:fileDownloadName"] = $"{map.StatLog.EndDate:MM/dd/yyyy}_{auditTypeDesc}.zip";

            return Json(new
            {
                Success = true,
                id = fileGuid
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Shows the files that can be downloaded
        /// </summary>
        /// <param name="sCostReportID">Cost Report ID</param>
        /// <returns>Download Files Page</returns>
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult DownloadFiles(string sCostReportID)
        {
            int costReportID = ClientMappingStatLog.ToInternalID(sCostReportID, StatLogScopes.COST_REPORT_ID_STATLOG);
            
            StatLogMapping.Mapping map = StatLogMapping.MapByCostReportID(costReportID, "Statlog:View");
            if (map.Error)
                throw new Exception(map.ErrorMessage);

            ARUMProxy_User myContact = RetrieveSession.ARUMUser;
            bool isPHISafe = ProviderUserHelper.IsProviderUserPhiSafe(map.Provider.ARUMProviderID, RetrieveSession.ARUMUser);

            string providerName = map.Provider.ConcatProviderNameAndNumber;
            
            var costReportHistory = CostReportHistoryBL.GetHistoryByCostReportID(RetrieveSession.Classification, map.StatLog.CRID,
                    myContact.UserID, "Transfer", "DownloadFiles", (int) map.StatLog.ARUMProviderID);

            var sanitizer = new HtmlSanitizer();

            var fileTable = costReportHistory
                .Where(history => history.Viewable(new[] { RetrieveSession.CurrentRole })
                            && history.Downloadable(new[] { RetrieveSession.CurrentRole }, isPHISafe)
                            && history.FileFoundInARUM
                            && history.IsActive)
                .Select(history => new DownloadFilesViewModel.FileInformation
                {
                    EventDate = history.EventDate,
                    EventDateString = history.EventDate.ToString(),
                    EventName = history.ParentHistory != null ? history.ParentHistory.EventName : history.EventName,
                    IsPHI = history.IsPHI,
                    IsReviewOkay = history.IsReviewOkay,
                    Notes = sanitizer.Sanitize(history.Notes),
                    FileID = ClientMappingStatLog.ToClientID(history.FileID?.ToString(), StatLogScopes.FILE_ID_STATLOG),
                    FileName = history.FileName,
                    FileModified = history.FileModified,
                    FileSize = history.FileSize,
                    FileFoundInARUM = history.FileFoundInARUM,
                    HasFileUpload = history.HasFileUpload,
                    Selected = false
                }).ToList();

            var viewModel = new DownloadFilesViewModel.View
            {
                sCostReportID = sCostReportID,
                ProviderName = providerName,
                Files = fileTable
            };
            
            return View(viewModel);
        }

        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult DownloadFile(Guid id)
        {
            ARUMProxy_User myUser = RetrieveSession.ARUMUser;
            var fileContents = (byte[]) TempData[$"{myUser.LastName}_{myUser.FirstName}:{id}:fileContents"];
            var contentType = (string) TempData[$"{myUser.LastName}_{myUser.FirstName}:{id}:contentType"];
            var fileDownloadName = (string) TempData[$"{myUser.LastName}_{myUser.FirstName}:{id}:fileDownloadName"];
            
            if (fileContents == null || contentType == null || fileDownloadName == null)
                return new EmptyResult();

            return File(fileContents, contentType, fileDownloadName);
        }
    }
}