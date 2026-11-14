using Ionic.Zip;
using MSLC.ApplicationProxy;
using MSLC.ArumProxy;
using MSLC.ArumProxy.Interfaces;
using MSLC.ArumProxy.Models;
using MSLC.FileProxy;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.StatLog.BL.CostReportHistory;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.Controllers;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using WebPortal_OH_FI.Models;
using WebPortal_OH_FI.Web;

namespace WebPortal_OH_FI.Controllers
{
    public class DownloadController : LoggedInBaseController
    {
        #region "Properties"

        private const string FILE_SCOPE = "DownloadFileID";
        private const string PROVIDER_SCOPE = "DownloadFileID";
        private readonly ArumServiceProxy _arumUserProxy;
        private readonly FileServiceProxy _arumFileProxy;
        private readonly ARUMProxy_User _user;

        #endregion
        
        public DownloadController()
        {
            _arumUserProxy = ARUMUser.GetArumServiceProxy(RetrieveSession.Classification);
            _arumFileProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
            _user = RetrieveSession.ARUMUser;
        }
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Index Page</returns>
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Portal:Download")]
        public ActionResult Index()
        {
            if (!UploadDownloadConfiguration.EnableDownload || !_user.Roles.Any())
                return RedirectToAction("Login", "Home");
            // The Download Controller currently has it's own special way of through it's own special session variable.
            // Pulls the special DownloadProviderID from the session and sets it as the selected provider id, than clears it's special provider id.
            int specialProviderID = OHFISession.ProviderID;
            RetrieveSession.SelectedProviderID = specialProviderID;
            OHFISession.ProviderID = 0;
            
            string primaryIdentifier = ProviderBL.GetPrimaryIdentifier(RetrieveSession.Classification);

            var providers = ProviderBL.GetAllARUMProviderInfoByARUMUserID(RetrieveSession.Classification, _user, "Portal:Download")
                .Select(p => new DownloadDropDownProvider
                {
                    Name = p.Name,
                    Number = p.Number,
                    ARUMProviderID = ClientMapping.MapInternalIdToClientId(p.ARUMProviderID.ToString(), PROVIDER_SCOPE),
                }).ToList();

            var myViewModel = new DownloadViewModel
            {
                FileIDs = null,
                sProviderID = specialProviderID < 1 ? string.Empty : 
                    ClientMapping.MapInternalIdToClientId(specialProviderID.ToString(), PROVIDER_SCOPE),
                Providers = providers,
                Files = RetrieveFiles(),
                UserName = null,
                PrimaryIdentifier = !string.IsNullOrEmpty(primaryIdentifier) ? primaryIdentifier : "MCAID",
            };
            return View(myViewModel);
        }
        
        /// <summary>
        /// Indexes the specified my view model.
        /// </summary>
        /// <param name="myViewModel">My view model.</param>
        /// <param name="Command">The command.</param>
        /// <returns></returns>
        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Portal:Download")]
        public ActionResult Index(DownloadViewModel myViewModel, string Command)
        {
            if (!UploadDownloadConfiguration.EnableDownload)
                return RedirectToAction("Login", "Home");
            string providerIDString = ClientMapping.GetInternalIdFromClientId(myViewModel.sProviderID, PROVIDER_SCOPE);
            int.TryParse(providerIDString, out int providerID);
            try
            {
                if (myViewModel == null) 
                    return Index();

                switch (Command)
                {
                    case "":
                    case null:
                        if (providerID < 1)
                        {
                            OHFISession.ProviderID = 0;
                            TempData["Message"] = "Please select a Provider.";
                            break;
                        }

                        OHFISession.ProviderID = providerID;
                        break;
                    case "DownloadSelectedFiles":
                        if (!myViewModel.FileIDs.Any()) 
                            break;

                        var fileIDs = myViewModel.FileIDs
                            .Split('&')
                            .Select(internalID =>
                            {
                                var fileIDString = ClientMapping.GetInternalIdFromClientId(internalID, FILE_SCOPE);
                                return Guid.TryParse(fileIDString, out Guid fileID) ? fileID : Guid.Empty;
                            })
                            .Where(id => id != Guid.Empty)
                            .Distinct();
                        
                        var provider = ProviderBL.GetProviderInfoByARUMUserAndProviderID(RetrieveSession.Classification,
                            _user, providerID, "Portal:Download");

                        //ADD LOGGING/EXCEPTION BEHAVIOR
                        if (provider == null)
                        {
                            TempData["Message"] = "Selected Provider Not Found, Please contact support";
                            return RedirectToAction("Index");
                        }
                        
                        using (ZipFile zip = new ZipFile())
                        {
                            int looper = 0;
                            foreach (Guid fileID in fileIDs)
                            {
                                looper++;

                                var file = GetFileByID(_user, fileID, false, looper, out var FileName);
                                if (file == null)
                                {
                                    TempData["Message"] = "Selected File has no content, Please contact support";
                                    return RedirectToAction("Index");
                                }

                                zip.AddEntry(FileName, file);
                            }

                            var myMemoryStreamForZip = new MemoryStream();
                            zip.Save(myMemoryStreamForZip);
                            myMemoryStreamForZip.Position = 0;

                            string zipFileName = $"ProviderFiles{provider.Name}_{DateTime.Now:yyyy_MM_dd_hh_mm_tt}.zip";
                            return File(myMemoryStreamForZip, @"application/zip", zipFileName);
                        }
                }
                return Index();
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
                return RedirectToAction("Login", "Home");
            }
        }
        
        
        private FileInformationViewModel[] RetrieveFiles()
        {
            int providerID = RetrieveSession.SelectedProviderID;
            int userID = RetrieveSession.ARUMUser.UserID;
            if (providerID < 1)
            {
                return new FileInformationViewModel[] { };
            }

            #region Retrieve valid parent file types for current user
            
            ARUMApplicationServiceProxy applicationProxy = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);

            var response = applicationProxy.GetAllRoleFileTypesByRoleID(RetrieveSession.Classification.ARUMToken, 0, "Download/RetrieveFiles");
            if (!response.Result.Successful)
            {
                TempData["Message"] = response.Result.Message;
                return new FileInformationViewModel[] { };
            }

            IARUMRole currentRole = RetrieveSession.CurrentRole;
            if (currentRole == null)
            {
                TempData["Message"] = "Missing Provider Role. Contact support.";
                return new FileInformationViewModel[] { };
            }
            var validParentFileTypes = response.RoleFileType
                .Where(r => r.RoleName.Equals(currentRole.RoleName))
                .Select(ft => ft.FileTypeName);

            #endregion

            #region Retrieve all files for provider ID

            var myFileResults = _arumFileProxy.GetFileListByApplicationIDAndUserIDAndProviderID(
                userID, providerID, "Download", "Index", "Download - Index");

            if (!myFileResults.Results.Successful)
            {
                TempData["Message"] = myFileResults.Results.Message;
                return new FileInformationViewModel[] { };
            }

            #endregion

            // Retrieve all fileIDs that have been uploaded via StatLog.
            var myStatLogFiles = CostReportHistoryBL.GetAllProviderStatLogFiles(RetrieveSession.Classification, providerID);
                
            return myFileResults.myFiles
                // Filter out any files uploaded via statlog.
                .Where(f => !myStatLogFiles.Contains(f.FileID))
                // Filter out any files that the current role does not have access too.
                .Where(f => validParentFileTypes.Contains(f.ParentFileTypeName))
                .OrderByDescending(c => c.CreateDate)
                .ThenBy(c => c.ParentFileTypeName)
                .ThenBy(c => c.ChildFileTypeName)
                .ThenBy(c => c.PeriodText)
                .Select(f => new FileInformationViewModel
                {
                    FileID = ClientMapping.MapInternalIdToClientId(f.FileID.ToString(), FILE_SCOPE),
                    ParentFileTypeName = f.ParentFileTypeName,
                    ChildFileTypeName = f.ChildFileTypeName,
                    PeriodText = f.PeriodText,
                    FileName = f.FileName,
                    SourceType = f.SourceType,
                    CreateDate = f.CreateDate,
                    SizeInString = f.SizeInString,
                }).ToArray();
        }

        private byte[] GetFileByID(ARUMProxy_User user, Guid ID, bool IsSingle, int Looper, out string FileName)
        {
            FileName = string.Empty;
            var arumfileResults = _arumFileProxy.GetFileByID(user.UserID, ID, "Download Index", "Get File By ID", "Download - Index - Get File By ID");
            if (arumfileResults != null)
            {
                if (arumfileResults.myFile != null && arumfileResults.myFile.Length > 0)
                {
                    if (IsSingle)
                    {
                        FileName = string.Concat(arumfileResults.FileName, ".", arumfileResults.Extension);
                    }
                    else
                    {
                        FileName = string.Format("{1}_{0}.{2}", arumfileResults.FileName, Looper,
                            arumfileResults.Extension);
                        //string.Concat(arumfileResults.FileName, ".", arumfileResults.Extension);
                    }

                    //var cd = new System.Net.Mime.ContentDisposition
                    //{
                    //    // for example foo.bak
                    //    FileName = FileName,

                    //    // always prompt the user for downloading, set to true if you want 
                    //    // the browser to try to show the file inline
                    //    Inline = true,
                    //};
                    return arumfileResults.myFile;
                }
            }
            return null;
        }
        /// <summary>
        /// Downloads the file result.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <returns>The File to send back to the client.</returns>
        [HttpGet]
        [SelectedProviderFeatureFilter("Portal:Download")]
        public ActionResult DownloadFileResult(string sFileID)
        {
            try
            {
                string fileIDString = ClientMapping.GetInternalIdFromClientId(sFileID, FILE_SCOPE);
                
                if (Guid.TryParse(fileIDString, out Guid fileID))
                {
                    //get file.. 
                    var file = GetFileByID(_user, fileID, true, 0, out var FileName);
                    if (file == null)
                    {
                        TempData["Message"] = "Selected File has no content, Please contact support";
                        return RedirectToAction("Index");
                    }

                    Response.Clear();
                    Response.ClearContent();
                    Response.ClearHeaders();
                    Response.AppendHeader("Content-Disposition", $"attachment; filename={FileName ?? string.Empty}");
                    return File(file, "application/force-download");
                }
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
            }
            return null;
        }

        [ARUMFeatureActionFilterAttribute("")] //TODO: Add attribute to limit which roles can hit this endpoint.
        public ActionResult GetFile(string key, string filename)
        {
            try
            {
                var myUser = _user;
                if (myUser == null)
                {
                    throw new Exception("Unable to retrieve the user data.");
                }

                var sessionKey = $"{myUser.LastName}_{myUser.FirstName}:{key}";
                var s = (MemoryStream)Session[sessionKey];
                Session[sessionKey] = null;

                return File(s, "text/plain", filename);
            }
            catch (Exception)
            {
                return new EmptyResult();
            }
        }
    }
}