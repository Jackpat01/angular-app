using System;
using MSLC.ArumProxy;
using MSLC.FileProxy;
using MSLC.StatLog.Controllers;
using System.Web.Mvc;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using WebPortal_OH_FI.Common;
using WebPortal_OH_FI.Web;
using Serilog;
using MSLC.ArumProxy.Models;
using System.Linq;
using WebPortal_OH_FI.Models;

namespace WebPortal_OH_FI.Controllers
{
    public class HelpController : LoggedInBaseController
    {
        #region "Properties"

        private const string FILE_SCOPE = "HelpResourcesID";
        private ArumServiceProxy _arumUserProxy;
        private FileServiceProxy _arumFileProxy;
        private readonly ARUMProxy_User _user;
        #endregion

        public HelpController()
        {
            _arumUserProxy = ARUMUser.GetArumServiceProxy(RetrieveSession.Classification);
            _arumFileProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
            _user = RetrieveSession.ARUMUser;
        }
        // GET: Help
        [ClearProviderID]
        public ActionResult Index()
        {
            if (!PortalConfiguration.EnableResource)
            {
                return RedirectToAction("Index", "Secure");
            }

            // I am disabling this endpoint because we don't have the time to focus on a disabled feature right now
            // We need to make sure the files on this endpoint are properly restricted
            // We need to add a proper feature filter tag
            // We need to use clientMapping to secure any ID's sent over
            // - Adam Holbert 1/25/2023 
            //throw new NotImplementedException(
            //    "This endpoint needs to be re-written to follow the OHFI security updates");
            var viewModel = new HelpViewModel.View();
            if (_user == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var myFileResults = _arumFileProxy.GetAllowAllFileListByApplicationID(_user.UserID, "Help", "Index", "help - Index");
            if (myFileResults.Results.Successful && (myFileResults.myFiles != null && myFileResults.myFiles.Any()))
            {
                viewModel.Files = myFileResults.myFiles.Select(f => new FileInformationViewModel
                {
                    FileID = ClientMapping.MapInternalIdToClientId(f.FileID.ToString(), FILE_SCOPE),
                    ParentFileTypeName = f.ParentFileTypeName,
                    ChildFileTypeName = f.ChildFileTypeName,
                    PeriodText = f.PeriodText,
                    FileName = f.FileName,
                    Extension = f.Extension.ToLower(),
                    SourceType = f.SourceType,
                    CreateDate = f.CreateDate,
                    SizeInString = f.SizeInString,
                }).OrderByDescending(c => c.CreateDate).ToArray();
            }
            else
            {
                TempData["Message"] = "No help resources available at this time.";
            }

            return View(viewModel);
        }

        /// <summary>
        /// Downloads the file result.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <returns>The File to send back to the client.</returns>
        [HttpGet]
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
            TempData["Message"] = "File not found, Please contact support";
            return RedirectToAction("Index");
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
    }
}