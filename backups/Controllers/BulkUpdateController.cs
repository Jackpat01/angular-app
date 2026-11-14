// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using MSLC.ArumProxy;
using MSLC.FileProxy;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.ProviderProxy;
using MSLC.StatLog.BL.AuditType;
using MSLC.StatLog.BL.CostReportHistory;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.BL.Report;
using MSLC.StatLog.Configuration;
using MSLC.StatLog.MVC.ViewModels;
using Newtonsoft.Json;
using Serilog;
using Syncfusion.Drawing;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using static MSLC.StatLog.UI.Models.ClientMappingStatLog;
using static MSLC.StatLog.UI.Models.StatLogScopes;

namespace MSLC.StatLog.Controllers
{
    public class BulkUpdateController : LoggedInBaseController
    {
        private const string FILE_DOWNLOAD_COOKIE_NAME = "fileDownload";
        private readonly ArumServiceProxy _arumUserProxy;
        private readonly FileServiceProxy _fileServiceProxy;
        private readonly ARUMProviderProxy _providerProxy;

        public BulkUpdateController()
        {
            _arumUserProxy = ARUMUser.GetArumServiceProxy(RetrieveSession.Classification);
            _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
            _providerProxy = ARUMProvider.GetARUMProviderServiceProxy(RetrieveSession.Classification);
        }

        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("DueDateUpdate:Access")]
        public ActionResult Index()
        {
            try
            {
                var primaryIdentifier = ProviderBL.GetPrimaryIdentifierDisplayName(RetrieveSession.Classification);
                BulkUpdateViewModel.View myViewModel = new BulkUpdateViewModel.View
                {
                    EndDate = new DateTime(DateTime.Now.Year, 12, 31),
                    StatLogTypeList = AuditTypeBL.GetAllActiveAuditTypes(RetrieveSession.Classification),
                    PrimaryIdentifier = primaryIdentifier,
                    ProviderList = new List<BulkUpdateProviderModel>(),
                    NewDueDate = new DateTime(DateTime.Now.Year, 12, 31)
                };
                //Create Provider List
                var ProviderList = ProviderBL.GetAllARUMProviderInfoByARUMUserID(RetrieveSession.Classification, RetrieveSession.ARUMUser, null).OrderBy(p => p.Name).ToList();
                foreach (var provider in ProviderList)
                {
                    myViewModel.ProviderList.Add(
                        new BulkUpdateProviderModel
                        {
                            Number = provider.Number,
                            Name = provider.Name,
                            ARUMProviderID = ToClientID(provider.ARUMProviderID, PROVIDER_ID_STATLOG)
                        });
                }
                return View(myViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return RedirectToAction("Login", "Home");
            }
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("DueDateUpdate:Access")]
        public JsonResult GenerateMassDueDateReport(string StatLogType, DateTime StatLogEndDate, string sProviderList, DateTime NewDueDate)
        {
            var myUser = RetrieveSession.ARUMUser;
            try
            {
                List<int?> ProviderIDs = new List<int?>();
                var IDArray = sProviderList.Split(';');
                //Int32.TryParse(myViewModel.SelectedEventID, out EventID);
                foreach (var ID in IDArray)
                {
                    if (string.IsNullOrEmpty(ID))
                        continue;
                    var interpretedID = ToInternalID(ID, PROVIDER_ID_STATLOG);
                    if (interpretedID != 0)
                    {
                        //ADD LOGGING/EXCEPTION BEHAVIOR
                        if (!myUser.Providers.Where(p => p.ProviderID == interpretedID).Any())
                            return Json(new
                            {
                                Success = false,
                                Message = "Error Generating Report"
                            }, JsonRequestBehavior.AllowGet);
                        ProviderIDs.Add(interpretedID);
                    }
                }

                DataTable dataTable = ReportBL.GenerateMassDueDateReport(RetrieveSession.Classification, RetrieveSession.ARUMUser, StatLogType, StatLogEndDate, ProviderIDs, NewDueDate);

                var fileGuid = Guid.NewGuid();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}"] = CreateExcelFile(dataTable, "Upload Reviews").ToArray();
                Session[$"{myUser.LastName}_{myUser.FirstName}:{fileGuid}:fileName"] = "MassDueDateReport-" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".xlsx";

                var sessionModel = new BulkUpdateViewModel.Save
                {
                    EndDate = StatLogEndDate,
                    SelectedStatLogTypeID = StatLogType,
                    ProviderIDs = sProviderList,
                    NewDueDate = NewDueDate,
                };

                Session[$"{myUser.LastName}_{myUser.FirstName}:UpdateModel"] = Newtonsoft.Json.JsonConvert.SerializeObject(sessionModel);

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

        [HttpGet]
        [ARUMFeatureActionFilterAttribute("DueDateUpdate:Access")]
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

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("DueDateUpdate:Access")]
        [ValidateAntiForgeryToken]
        public ActionResult BulkUpdate(BulkUpdateViewModel.Save myViewModel)
        {
            if (!myViewModel.ReportDownloaded)
            {
                TempData["Message"] = "Error updating Due Dates. Please try again later.";
                return RedirectToAction("Index", "BulkUpdate");
            }
            var myUser = RetrieveSession.ARUMUser;

            //Compare session viewModel to input viewModel to check for unreported model changes
            var sessionModel = JsonConvert.DeserializeObject<BulkUpdateViewModel.Save>((string)Session[$"{myUser.LastName}_{myUser.FirstName}:UpdateModel"]);
            if (sessionModel.SelectedStatLogTypeID !=  myViewModel.SelectedStatLogTypeID ||
                sessionModel.EndDate != myViewModel.EndDate ||
                sessionModel.ProviderIDs != myViewModel.ProviderIDs ||
                sessionModel.NewDueDate != myViewModel.NewDueDate)
            {
                Log.Warning("BulkUpdate model generated for report and BulkUpdate model submitted by user do not match");
                TempData["Message"] = "Error updating Due Dates. Please try again later.";
                return RedirectToAction("Index", "BulkUpdate");
            }

            List<int?> ProviderIDs = new List<int?>();
            var IDArray = myViewModel.ProviderIDs.Split(';');
            //Int32.TryParse(myViewModel.SelectedEventID, out EventID);
            foreach (var ID in IDArray)
            {
                if (string.IsNullOrEmpty(ID))
                    continue;
                var interpretedID = ToInternalID(ID, PROVIDER_ID_STATLOG);
                if (interpretedID != 0)
                {
                    //ADD LOGGING/EXCEPTION BEHAVIOR
                    if (!myUser.Providers.Where(p => p.ProviderID == interpretedID).Any())
                        return RedirectToAction("Index", "BulkUpdate");
                    ProviderIDs.Add(interpretedID);
                }
            }
            //EventIDs = EventBL.GetAllDescendantEventsFromList(RetrieveSession.Classification, EventIDs);
            CostReportHistoryBL.BulkUpdateHistoryDueDateByStatLogIDAndEventID(RetrieveSession.Classification, RetrieveSession.ARUMUser.Email, myViewModel.EndDate, ProviderIDs, myViewModel.SelectedStatLogTypeID, myViewModel.NewDueDate);
            TempData["Message"] = "Due Dates successfully updated.";
            return RedirectToAction("Index", "BulkUpdate");
        }

        [HttpGet]
        [ARUMFeatureActionFilterAttribute("DueDateUpdate:Access")]
        public JsonResult AdjustDueDate(string sUncheckedDueDate)
        {
            bool pushStatlogDueDate = StatlogARUMConfiguration.AdjustForWeekendAndHoliday(RetrieveSession.Classification);
            if (!pushStatlogDueDate)
            {
                return Json(new { checkedDueDate = sUncheckedDueDate }, JsonRequestBehavior.AllowGet);
            }
            DateTime uncheckedDueDate = DateTime.Parse(sUncheckedDueDate);
            DateTime checkedDueDate = DueDateConfiguration.AdjustForWeekendAndHoliday(uncheckedDueDate, pushStatlogDueDate);

            return Json(new { checkedDueDate = checkedDueDate.ToString("MM/dd/yyyy"), PushStatlogDueDate = pushStatlogDueDate }, JsonRequestBehavior.AllowGet);
        }

        private MemoryStream CreateExcelFile(DataTable dataTable, string worksheetName)
        {
            ExcelEngine xlEngine = new ExcelEngine();

            IApplication xlApp = xlEngine.Excel;
            IWorkbook xlWorkbook = xlApp.Workbooks.Create(1);
            xlWorkbook.Version = ExcelVersion.Excel2016;
            IWorksheet worksheet = xlWorkbook.Worksheets[0];

            //Field Descriptions
            worksheet.ImportDataTable(dataTable, true, 1, 1);

            #region Warning Column Style

            IStyle warningStyle = xlWorkbook.Styles.Add("Warning Styling");
            warningStyle.Font.RGBColor = Color.Red;
            worksheet.Columns[8].CellStyle = warningStyle;

            #endregion

            #region Business requested styles

            IStyle style = xlWorkbook.Styles.Add("MSLC Styling");
            style.Font.Bold = true;
            style.Font.RGBColor = Color.Black;
            style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            style.Color = Color.FromArgb(122, 193, 66);
            // style.FillPattern = ExcelPattern.DarkUpwardDiagonal;
            worksheet.Rows[0].CellStyle = style;
            if (worksheet.Rows.Length > 1) // if it has any data, free the first row
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
    }
}