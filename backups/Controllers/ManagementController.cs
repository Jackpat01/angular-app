using CsvHelper.Configuration;
using MSLC.ApplicationProxy;
using MSLC.ApplicationProxy.Interfaces;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.ADO;
using MSLC.Portal.Utility.Proxies;
using MSLC.ProviderProxy;
using MSLC.RateSetting.Data.Models;
using MSLC.StatLog.Controllers;
using Serilog;
using Syncfusion.Pdf;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using WebPortal_OH_FI.BL;
using WebPortal_OH_FI.BL.Helpers;
using WebPortal_OH_FI.BL.MappedObjects;
using WebPortal_OH_FI.Common;
using WebPortal_OH_FI.Common.Extensions;
using WebPortal_OH_FI.Models;
using WebPortal_OH_FI.Models.CostReport;
using WebPortal_OH_FI.ProjectSpecific.Data;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.crdata;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;
using WebPortal_OH_FI.Web;

namespace WebPortal_OH_FI.Controllers
{
    public static class SESSION_KEYS_MGMT
    {
        public const string IMPORT_PROGRESS = "ImportProgress";
    }

    public class ManagementController : LoggedInBaseController
    {
        private ImportProgressViewModel ImportHistoricalProgress = new ImportProgressViewModel();
        private readonly ARUMProviderProxy _providerServiceProxy;
        protected char[] Letters = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

        private static string[] C1AdminMainFeilds = { "27190", "27200", "27010", "27020" };

        private static string SimplifiedType =>
            Common.Extensions.ClassificationDTOExtensions.GetSimplifiedType(RetrieveSession.Classification);

        private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<ManagementController>();
        private static Serilog.ILogger _staticLogger = Serilog.Log.Logger.ForContext<ManagementController>();

        public ManagementController()
        {
            _providerServiceProxy = MSLC.Portal.Utility.Proxies.ARUMProvider.GetARUMProviderServiceProxy(RetrieveSession.Classification);
        }

        // GET: Management
        public ActionResult Index()
        {
            return View();
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult Version()
        {
            try
            {
                string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new Exception("Invalid request.");
                }

                string[] token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new Exception("Invalid request.");
                }

                string main = token[0];
                string worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new Exception("Invalid request.");
                }

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                int providerId = 0;
                if (int.TryParse(selection.ProviderId, out int c))
                {
                    providerId = c;
                }

                //Getting the last cleared Provider Cost Report Id
                //Convert selection PeriodCode to previous year
                int previousYear = 0;
                int? clearedProviderCostReportId = null;
                if (int.TryParse(selection.PeriodCode.Substring(2, 4), out int y))
                {
                    previousYear = y - 1;
                    clearedProviderCostReportId = ManagementBL.GetClearedProviderCostReportId(providerId, $"CY{previousYear}");
                }
                var canCreateFromCopy = false;
                //Disable the "Copy Cost Report" for the audit cost report types '4.90', '4.91', '4.92'
                if (clearedProviderCostReportId != null && !selection.CostReportTypeId.StartsWith("4.9"))
                {
                    if (CostReportPermsissionsBL.CheckPermissionCostReportCreate(selection.CostReportTypeId))
                        canCreateFromCopy = true;
                }
               
                ManagementViewModel model = new ManagementViewModel
                {
                    // map to client id for sending to client.
                    ProviderCostReportId = ClientMapping.MapInternalIdToClientId(selection.ProviderCostReportId, "costreport"),
                    ProviderId = ClientMapping.MapInternalIdToClientId(selection.ProviderId, "chow"),
                    PeriodId = selection.PeriodCode,
                    CostReportTypeId = selection.CostReportTypeId,
                    VersionName = selection.VersionName,
                    ProviderName = selection.FacilityText,
                    CanEditCostReport = selection.CanEditCostReport,
                    CanCreateFromCopy = canCreateFromCopy,
                    CanCreateCostReport = selection.CanCreateCostReport
                };

                return PartialView(model);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw new Exception("Unable to Copy Cost Report Data.");
            }
        }

        public JsonResult CopyCostReportData(string costReportOption)
        {
            try
            {
                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCreateNewFacilityVersion(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for Copy Cost Report."
                    }, JsonRequestBehavior.AllowGet);
                }

                string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new Exception("Invalid request.");
                }

                string[] token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new Exception("Invalid request.");
                }

                string main = token[0];
                string worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new Exception("Invalid request.");
                }

                CostReportModel cr = new CostReportModel
                {
                    ProviderCostReportId = selection.ProviderCostReportId,
                    ProviderId = selection.ProviderId != "0" && !string.IsNullOrWhiteSpace(selection.ProviderId) ? (int?)int.Parse(selection.ProviderId) : null,
                    PeriodId = selection.PeriodCode,
                    CostReportTypeId = selection.CostReportTypeId,
                    VersionName = selection.VersionName,
                    ProviderName = selection.FacilityText
                };

                ARUMHandler.ClearCache();
                MSLC.ProviderProxy.Interfaces.IArumDetailedProvider arum_provider = _providerServiceProxy.GetProviderInfoByARUMProviderID(RetrieveSession.Classification.ARUMToken, cr.ProviderId.Value, "Rate/Provider");

                if (arum_provider != null)
                {
                    cr.ProviderName = arum_provider.Provider.Name;

                    foreach (MSLC.ProviderProxy.Interfaces.Models.ProviderIdentifier identifier in arum_provider.Identifiers)
                    {
                        switch (identifier.Type.ToUpper())
                        {
                            case "FEDTAXID":
                            case "FEDERAL TAX ID NUMBER":
                                cr.FedId = identifier.Value.FormatUSTaxId();
                                break;
                            case "MEDICAREID":
                                cr.CCN = identifier.Value;
                                break;
                            case "MCAID":
                                cr.MedicaidId = identifier.Value;
                                break;
                            case "NPI":
                                cr.NPI = identifier.Value;
                                break;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(cr.MedicaidId)) cr.MedicaidId = arum_provider.Provider.MCAID;

                    cr.ODHId = CostReportBL.GetODHFromAffiliate(_providerServiceProxy, arum_provider);

                    MSLC.ProviderProxy.Interfaces.Models.Address address = arum_provider.Addresses
                        .FirstOrDefault(x => (x.Type == "FA" || x.Type == null || x.Type == "") && x.IsActive);
                    if (address != null)
                    {
                        cr.Address1 = address.Address1 ?? string.Empty;
                        cr.Address2 = address.Address2 ?? string.Empty;
                        cr.City = address.City ?? string.Empty;
                        cr.ZipCode = address.ZipCode ?? string.Empty;
                    }

                    // Get the expected period end of the specified period.
                    DateTime? periodEnd = RedDateBL.GetPeriodEndDate(selection.PeriodCode);

                    if (periodEnd.HasValue)
                    {
                        cr.County = arum_provider.EffectiveDates
                            .OrderByDescending(x => x.EffectiveDate)
                            .ThenByDescending(x => x.CreateDate)
                            .FirstOrDefault(x => x.EffectiveDate <= periodEnd.Value)?.County ?? string.Empty;
                    }
                    else
                    {
                        cr.County = arum_provider.EffectiveDates
                            .OrderByDescending(x => x.EffectiveDate)
                            .ThenByDescending(x => x.CreateDate)
                            .FirstOrDefault()?.County ?? string.Empty;
                    }
                }
                else
                {
                    return Json(new { Success = false, Message = $"Provider data not found for {selection.FacilityText}"});
                }

                ManagementBL.CopyCostReport(cr, costReportOption);

                return Json(new { Success = true });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = e.Message });
            }
        }

        #region Notes and Review
        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult Note()
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            ManagementViewModel model = new ManagementViewModel
            {
                // map to client id for sending to client.
                ProviderCostReportId = ClientMapping.MapInternalIdToClientId(selection.ProviderCostReportId, "costreport"),
                ProviderId = ClientMapping.MapInternalIdToClientId(selection.ProviderId, "chow"),
                PeriodId = selection.PeriodCode,
                CostReportTypeId = selection.CostReportTypeId,
                VersionName = selection.VersionName,
                ProviderName = selection.FacilityText,
                CanEditCostReport = selection.CanEditCostReport
            };

            return PartialView(model);
        }

        // Get All notes by ProviderCostReportId
        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetNotes()
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int i))
            {
                providerCostReportId = i;
            }

            List<Note> notes = ManagementBL.GetNotes(providerCostReportId);
            List<NoteViewModel> list = new List<NoteViewModel>();

            foreach (Note note in notes)
            {
                list.Add(new NoteViewModel
                {
                    NoteId = ClientMapping.MapInternalIdToClientId(note.NoteId.ToString(), "notes"),
                    ProviderCostReportId = ClientMapping.MapInternalIdToClientId(note.ProviderCostReportId.ToString(), "costreport"),
                    Notes = note.Notes,
                    UpdatedBy = note.UpdatedBy,
                    UpdatedDate = note.UpdatedDate
                });
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetNote(string noteId)
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int i))
            {
                providerCostReportId = i;
            }

            Note note = ManagementBL.GetNote(providerCostReportId,
                int.Parse(ClientMapping.GetInternalIdFromClientId(noteId, "notes")));

            NoteViewModel vm = new NoteViewModel
            {
                NoteId = ClientMapping.MapInternalIdToClientId(note.NoteId.ToString(), "notes"),
                ProviderCostReportId = ClientMapping.MapInternalIdToClientId(note.ProviderCostReportId.ToString(), "costreport"),
                Notes = HttpUtility.HtmlDecode(note.Notes),
                UpdatedBy = note.UpdatedBy,
                UpdatedDate = note.UpdatedDate
            };

            return Json(vm, JsonRequestBehavior.AllowGet);
        }

        // Add Note
        public JsonResult AddNote(NoteViewModel note)
        {
            var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
            {
                return Json(new
                {
                    Success = false,
                    Message = "Invalid request for Add Notes."
                }, JsonRequestBehavior.AllowGet);
            }
            note.Notes = note.Notes ?? "";
            Note item = new Note
            {
                NoteId = 0,
                Notes = note.Notes
            };

            if (!string.IsNullOrWhiteSpace(note.NoteId)) item.NoteId = int.Parse(ClientMapping.GetInternalIdFromClientId(note.NoteId, "notes"));

            var myUser = RetrieveSession.ARUMUser;
            item.CreatedBy = myUser.FirstName + " " + myUser.LastName;
            note.UpdatedBy = item.UpdatedBy = item.CreatedBy;

            if (int.TryParse(selection.ProviderCostReportId, out int i))
            {
                item.ProviderCostReportId = i;
            }

            ManagementBL.SaveNote(item);

            note.UpdatedDate = item.UpdatedDate;

            note.NoteId = ClientMapping.MapInternalIdToClientId(item.NoteId.ToString(), "notes");

            return Json(note, JsonRequestBehavior.AllowGet);
        }

        // Delete Note by
        public JsonResult DeleteNote(string id)
        {
            var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
            {
                return Json(new
                {
                    Success = false,
                    Message = "Invalid request for Delete Notes."
                }, JsonRequestBehavior.AllowGet);
            }
            ManagementBL.DeleteNote(int.Parse(ClientMapping.GetInternalIdFromClientId(id, "notes")));
            return Json(new { Success = true, Message = @"" }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Audit Report
        //TODO: Change strings for feature filter attributes for audit endpoints
        // waiting for new groups to be made for this

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult AuditReport()
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            ManagementViewModel model = new ManagementViewModel
            {
                // map to client id for sending to client.
                ProviderCostReportId = ClientMapping.MapInternalIdToClientId(selection.ProviderCostReportId, "costreport"),
                ProviderId = ClientMapping.MapInternalIdToClientId(selection.ProviderId, "chow"),
                PeriodId = selection.PeriodCode,
                CostReportTypeId = selection.CostReportTypeId,
                VersionName = selection.VersionName,
                ProviderName = selection.FacilityText,
                CanEditCostReport = selection.CanEditCostReport
            };

            return PartialView(model);
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult AuditAdjustmentHistoryReport()
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            ManagementViewModel model = new ManagementViewModel
            {
                // map to client id for sending to client.
                ProviderCostReportId = ClientMapping.MapInternalIdToClientId(selection.ProviderCostReportId, "costreport"),
                ProviderId = ClientMapping.MapInternalIdToClientId(selection.ProviderId, "chow"),
                PeriodId = selection.PeriodCode,
                CostReportTypeId = selection.CostReportTypeId,
                VersionName = selection.VersionName,
                ProviderName = selection.FacilityText,
                CanEditCostReport = selection.CanEditCostReport
            };

            return PartialView(model);
        }

        #endregion

        /// <summary>
        /// View the applet view for creating a new facility version by copying data
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult CreateNewFacility()
        {
            try
            {
                var selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new Exception("Invalid request.");
                }

                var token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new Exception("Invalid request.");
                }

                var main = token[0];
                var worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new Exception("Invalid request.");
                }

                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }
                int providerId = 0;
                if (int.TryParse(selection.ProviderId, out int c))
                {
                    providerId = c;
                }
                //Getting the Cleared or On-Hold Provider Cost Report Id
                var clearedOnHoldProviderCostReportId = ManagementBL.GetSubmittedProviderCostReportId(providerId, selection.PeriodCode);
                var canCreateNewFacility = false;
                ProviderCostReport lastSubmittedCostReport = null;
                if (clearedOnHoldProviderCostReportId != null)
                {
                    if(selection.CostReportTypeId.StartsWith("4.9"))
                    {
                        lastSubmittedCostReport =  CostReportAuditBL.GetCopiedCostReport(selection.PeriodCode, selection.CostReportTypeId, providerId);
                    }
                    if (CostReportPermsissionsBL.CheckPermissionCostReportCreate(selection.CostReportTypeId))
                        canCreateNewFacility = true;
                }
                
                var model = new ManagementViewModel
                {
                    // map to client id for sending to client.
                    ProviderCostReportId = ClientMapping.MapInternalIdToClientId(selection.ProviderCostReportId, "costreport"),
                    ProviderId = ClientMapping.MapInternalIdToClientId(selection.ProviderId, "chow"),
                    PeriodId = selection.PeriodCode,
                    CostReportTypeId = selection.CostReportTypeId,
                    VersionName = selection.VersionName,
                    ProviderName = selection.FacilityText,
                    CanEditCostReport = selection.CanEditCostReport,
                    CanCreateNewFacility = canCreateNewFacility,
                    CanCreateCostReport = selection.CanCreateCostReport,
                    LastSubmittedCostReportTypeCode = lastSubmittedCostReport?.CostReportTypeCode,
                    LastSubmittedDocumentNumber = lastSubmittedCostReport?.Version
                };

                return PartialView(model);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw new ArgumentException("Unable to View Copy Cost Report Data.");
            }
        }

        /// <summary>
        /// Perform the action of creating a new facility version by copying cost report data.
        /// </summary>
        /// <returns></returns>
        public JsonResult CreateNewFacilityVersion()
        {
            var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            if (!CostReportPermsissionsBL.CheckPermissionCreateNewFacilityVersion(selection.CostReportTypeId))
            {
                return Json(new
                {
                    Success = false,
                    Message = "Invalid request for Create New Facility Version."
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new ArgumentException("Invalid request.");
                }

                var token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new ArgumentException("Invalid request.");
                }

                var main = token[0];
                var worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new Exception("Invalid request.");
                }

                var cr = new CostReportModel
                {
                    ProviderCostReportId = selection.ProviderCostReportId,
                    ProviderId = selection.ProviderId != "0" && !string.IsNullOrWhiteSpace(selection.ProviderId) ? (int?)int.Parse(selection.ProviderId) : null,
                    PeriodId = selection.PeriodCode,
                    CostReportTypeId = selection.CostReportTypeId,
                    VersionName = selection.VersionName,
                    ProviderName = selection.FacilityText
                };

                ARUMHandler.ClearCache();
                MSLC.ProviderProxy.Interfaces.IArumDetailedProvider arum_provider = _providerServiceProxy.GetProviderInfoByARUMProviderID(RetrieveSession.Classification.ARUMToken, cr.ProviderId.Value, "Rate/Provider");

                if (arum_provider != null)
                {
                    cr.ProviderName = arum_provider.Provider.Name;

                    foreach (MSLC.ProviderProxy.Interfaces.Models.ProviderIdentifier identifier in arum_provider.Identifiers)
                    {
                        switch (identifier.Type.ToUpper())
                        {
                            case "FEDTAXID":
                            case "FEDERAL TAX ID NUMBER":
                                cr.FedId = identifier.Value.FormatUSTaxId();
                                break;
                            case "MEDICAREID":
                                cr.CCN = identifier.Value;
                                break;
                            case "MCAID":
                                cr.MedicaidId = identifier.Value;
                                break;
                            case "NPI":
                                cr.NPI = identifier.Value;
                                break;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(cr.MedicaidId)) cr.MedicaidId = arum_provider.Provider.MCAID;

                    cr.ODHId = CostReportBL.GetODHFromAffiliate(_providerServiceProxy, arum_provider);

                    MSLC.ProviderProxy.Interfaces.Models.Address address = arum_provider.Addresses
                        .FirstOrDefault(x => (x.Type == "FA" || x.Type == null || x.Type == "") && x.IsActive);
                    if (address != null)
                    {
                        cr.Address1 = address.Address1 ?? string.Empty;
                        cr.Address2 = address.Address2 ?? string.Empty;
                        cr.City = address.City ?? string.Empty;
                        cr.ZipCode = address.ZipCode ?? string.Empty;
                    }

                    // Get the expected period end of the specified period.
                    DateTime? periodEnd = RedDateBL.GetPeriodEndDate(selection.PeriodCode);

                    if (periodEnd.HasValue)
                    {
                        cr.County = arum_provider.EffectiveDates
                            .OrderByDescending(x => x.EffectiveDate)
                            .ThenByDescending(x => x.CreateDate)
                            .FirstOrDefault(x => x.EffectiveDate <= periodEnd.Value)?.County ?? string.Empty;
                    }
                    else
                    {
                        cr.County = arum_provider.EffectiveDates
                            .OrderByDescending(x => x.EffectiveDate)
                            .ThenByDescending(x => x.CreateDate)
                            .FirstOrDefault()?.County ?? string.Empty;
                    }
                }
                else
                {
                    return Json(new { Success = false, Message = $"Provider data not found for {selection.FacilityText}" });
                }

                ManagementBL.CreateNewFacilityVersion(cr);

                return Json(new { Success = true });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = e.Message });
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult CheckSubmittedVersion()
        {
            try
            {
                var selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new Exception("Invalid request.");
                }

                var token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new Exception("Invalid request.");
                }

                var main = token[0];
                var worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new Exception("Invalid request.");
                }

                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                var cr = new CostReportModel
                {
                    ProviderCostReportId = selection.ProviderCostReportId,
                    ProviderId = selection.ProviderId != "0" && !string.IsNullOrWhiteSpace(selection.ProviderId) ? (int?)int.Parse(selection.ProviderId) : null,
                    PeriodId = selection.PeriodCode,
                    CostReportTypeId = selection.CostReportTypeId,
                    ProviderName = selection.FacilityText
                };

                // TODO: - There already exists a version of the Cost Report for the same Provider, Reporting Period, Cost Report Type AND 
                //The associated Cost Report statlog for the same Provider / Reporting Period has one of the following statuses as the most recent child event: 
                //"R" (Resubmission Due - Errors)
                //"X" (Invalid Cost Report)
                //"S" (Supervisor Resubmission Request)

                bool costReportIsSubmitted = providerCostReportId != 0 && CostReportHistoryBL.CheckSubmissionStatus(providerCostReportId) != CostReportStatusEnum.Open;

                return Json(new { Success = true, Submitted = costReportIsSubmitted });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = e.Message });
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GenerateTransmitNumber()
        {
            try
            {
                return Json(new { Success = true, Data = CostReportBL.GetTransmitNumber() });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = e.Message });
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult ExportExcel()
        {
            try
            {
                var selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new Exception("Invalid request.");
                }

                var token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new Exception("Invalid request.");
                }

                var main = token[0];
                var worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new Exception("Invalid request.");
                }

                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                var model = new ManagementViewModel
                {
                    // map to client id for sending to client.
                    ProviderCostReportId = ClientMapping.MapInternalIdToClientId(selection.ProviderCostReportId, "costreport"),
                    ProviderId = ClientMapping.MapInternalIdToClientId(selection.ProviderId, "chow"),
                    PeriodId = selection.PeriodCode,
                    CostReportTypeId = selection.CostReportTypeId,
                    VersionName = selection.VersionName,
                    ProviderName = selection.FacilityText
                };

                return PartialView(model);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw new Exception("Unable to Copy Cost Report Data.");
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult ExportToExcel()
        {
            string fullPath = "";
            try
            {
                var selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new ArgumentException("Invalid request.");
                }

                var token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new ArgumentException("Invalid request.");
                }

                var main = token[0];
                var worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new ArgumentException("Invalid request.");
                }

                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                // Get the document template effective year for the Medicaid document of this cost report
                string costReportExtractTemplateLocation = GetCostReportTemplate(CostReportBL.GetCostReportTemplateEffectiveDate(providerCostReportId), false);

                if (string.IsNullOrEmpty(costReportExtractTemplateLocation)) 
                {
                    throw new ArgumentException("Cost Report Extract Template Location not configured in Global Application Settings.");
                }

                string fileName = $"{selection.FacilityText} ({selection.PeriodCode}).xlsx".ToSafeFileName();

                //Save the file to the path of the current user's temporary folder
                fullPath = Path.Combine(Path.GetTempPath(), fileName);


                string stringNoEditSummaryText = ARUMConfigurationOHFI.CostReportHistoricalImportAcceptanceDate(RetrieveSession.Classification);
                if (!DateTime.TryParse(stringNoEditSummaryText, out DateTime dateNoEditSummaryText)) dateNoEditSummaryText = new DateTime(1900, 1, 1);

                ManagementBL.GenerateCostReportExtractExcel(providerCostReportId, selection, fileName, costReportExtractTemplateLocation,
                    ARUMConfigurationOHFI.CostReportEditSummaryQuestionText(RetrieveSession.Classification),
                    dateNoEditSummaryText);

                return Json(new { Success = true, fileName = HttpUtility.UrlEncode(fileName) });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = $"{e.Message}\n{fullPath}" });
            }
        }


        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult Download(string fileName)
        {
            //Get the file path of the current user's temporary folder
            string fullPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileName));
            byte[] fileByteArray = System.IO.File.ReadAllBytes(fullPath);
            System.IO.File.Delete(fullPath);
            //Get  the file extension to determine download MIME Type
            string extn = Path.GetExtension(fileName);
            string fileMIMEType = "";
            if (extn.Equals(".xlsx")) { fileMIMEType = "application/vnd.ms-excel"; }
            else if (extn.Equals(".txt")) { fileMIMEType = "text/plain"; }
            else if (extn.Equals(".zip")) { fileMIMEType = "application/zip"; }
            else if (extn.Equals(".csv")) { fileMIMEType = "text/csv"; }

            // Inherit Request Origin Header to allow Barracuda proxy.
            string originHeader = Request.Headers.Get("Access-Control-Allow-Origin");
            if (!string.IsNullOrWhiteSpace(originHeader))
            {
                Response.Headers.Add("Access-Control-Allow-Origin", originHeader);
            }

            return File(fileByteArray, fileMIMEType, fileName);
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult ExportText()
        {
            try
            {
                var selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new Exception("Invalid request.");
                }

                var token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new Exception("Invalid request.");
                }

                var main = token[0];
                var worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new Exception("Invalid request.");
                }

                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                var model = new ManagementViewModel
                {
                    // map to client id for sending to client.
                    ProviderCostReportId = ClientMapping.MapInternalIdToClientId(selection.ProviderCostReportId, "costreport"),
                    ProviderId = ClientMapping.MapInternalIdToClientId(selection.ProviderId, "chow"),
                    PeriodId = selection.PeriodCode,
                    CostReportTypeId = selection.CostReportTypeId,
                    VersionName = selection.VersionName,
                    ProviderName = selection.FacilityText
                };

                return PartialView(model);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw new Exception("Unable to Copy Cost Report Data to text file.");
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult ExportToText()
        {
            try
            {
                var selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new Exception("Invalid request.");
                }

                var token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new Exception("Invalid request.");
                }

                var main = token[0];
                var worksheetName = token[1];

                if (worksheetName != "version")
                {
                    throw new Exception("Invalid request.");
                }

                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                var crm = new CostReportModel
                {
                    ProviderCostReportId = selection.ProviderCostReportId,
                    ProviderId = selection.ProviderId != "0" && !string.IsNullOrWhiteSpace(selection.ProviderId) ? (int?)int.Parse(selection.ProviderId) : null,
                    PeriodId = selection.PeriodCode,
                    CostReportTypeId = selection.CostReportTypeId,
                    VersionName = selection.VersionName,
                    ProviderName = selection.FacilityText
                };

                var fileName = $"{crm.ProviderName} ({crm.PeriodId}).txt".ToSafeFileName();

                var db = new ProjectSpecificDbContext(WebPortal_OH_FI.Common.Functions.DatabaseConnection);
                var cr = db.ProviderCostReports.FirstOrDefault(x => x.ProviderCostReportId == providerCostReportId);


                //Save the file to server temp folder
                string fullPath = Path.Combine(Path.GetTempPath(), fileName);

                //string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                //string sFile = System.IO.Path.Combine(sCurrentDirectory, @"Documents\");
                //string sFilePath = Path.GetFullPath(sFile);

                var results = Database.LoadDataTableSQLWithConnectionString(
                Functions.DatabaseConnection,
                "GetSubmitReport",
                new SqlParameter("@ProviderCostReportId", cr.ProviderCostReportId));

                try
                {
                    CostReportTextFileHelper txtHelper = new CostReportTextFileHelper();
                    MemoryStream ms = txtHelper.PopulateCostReportExportFile(results);
                    FileStream file = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                    ms.WriteTo(file);
                    file.Close();
                    ms.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message.ToString(), e);
                    throw;
                }

                return Json(new { Success = true, fileName = HttpUtility.UrlEncode(fileName) });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = e.Message });
            }
        }

        [ARUMFeatureActionFilter("CostReport:Edit")]
        public JsonResult UpdateProviderTracking()
        {
            List<string> results = new List<string>();

            try
            {
                var myUser = RetrieveSession.ARUMUser;

                #region Get Provider Info
                ARUMApplicationServiceProxy _arumApplicationClient = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);

                IAllProviders providers = _arumApplicationClient.GetAllActiveProvidersWithOptions(RetrieveSession.Classification.ARUMToken,
                    false, true, false, true, false, false, false,
                    "Management/UpdateProviderTracking");
                #endregion

                using (ProjectSpecificDbContext db = new ProjectSpecificDbContext(Functions.DatabaseConnection))
                {
                    foreach (MSLC.ApplicationProxy.Interfaces.Models.Provider prov in providers.lstProviders)
                    {
                        MSLC.ApplicationProxy.Interfaces.Models.EffectiveDate effDate = RateProviderBL.GetProviderEffectiveDateObject(SimplifiedType, prov.EffectiveDates);
                        if (effDate == null)
                        {
                            results.Add($"{prov.ProviderID} {prov.MCAID} {prov.Name}: No effective dates");
                            continue;
                        }

                        if (effDate.County == null)
                        {
                            results.Add($"{prov.ProviderID} {prov.MCAID} {prov.Name}: No county Id");
                            continue;
                        }

                        RateBL.AddRateProvider(prov.ProviderID, prov.Name, prov.MCAID, effDate.DateEffective, $"{myUser.FirstName} {myUser.LastName}");

                        // add or update the provider in tracking.
                        CostReportTrackingBL.CheckCostReportTrackingProvider(db, 
                            prov.ProviderID, int.Parse(effDate.County), true, prov.Name, prov.MCAID, 
                            prov.Identifiers.FirstOrDefault(x => x.Type == "NPI")?.Value);

                        results.Add($"{prov.ProviderID} {prov.MCAID} {prov.Name}: SUCCESS");
                    }

                    db.SaveChanges();
                }

                return Json(new
                {
                    Success = true,
                    Data = results
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new
                {
                    Success = false,
                    Data = results
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        public JsonResult ImportHistoricalProgressCheck()
        {
            return Json(new
            {
                Success = true,
                Data = (ImportProgressViewModel)Session[SESSION_KEYS_MGMT.IMPORT_PROGRESS]
            }, JsonRequestBehavior.AllowGet);
        }

        [ARUMFeatureActionFilter("RateSetting:View")]
        public JsonResult ImportCertifiedBeds(string type, string filepath)
        {
            try
            {
                CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true //false
                };

                #region Get Provider Info
                ARUMApplicationServiceProxy _arumApplicationClient = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);

                IAllProviders providers = _arumApplicationClient.GetAllActiveProvidersWithOptions(RetrieveSession.Classification.ARUMToken,
                    false, false, false, false, false, false, false,
                    "Management/ImportHistorical");
                #endregion

                // Get the ARUM provider information into a form the business layer can use.
                List<RateProvider> rateProviders = new List<RateProvider>();
                foreach (var provider in providers.lstProviders)
                {
                    if (!provider.IsActive) continue;
                    rateProviders.Add(new RateProvider
                    {
                        ProviderId = provider.ProviderID,
                        ProviderName = provider.Name,
                        ProviderNumber = provider.MCAID
                    });
                }

                List<ICertifiedBeds> certifiedBeds = new List<ICertifiedBeds>();

                //Open existing Excel file
                using (ExcelEngine excelEngine = new ExcelEngine())
                using (MemoryStream ExcelMemStream = new MemoryStream())
                using (FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    file.CopyTo(ExcelMemStream);

                    Syncfusion.XlsIO.IApplication app = excelEngine.Excel;
                    app.DefaultVersion = ExcelVersion.Xlsx;

                    ExcelMemStream.Position = 0;
                    IWorkbook workbook = app.Workbooks.Open(ExcelMemStream, ExcelOpenType.Automatic, ExcelParseOptions.Default);

                    int length = 0;
                    switch (SimplifiedType)
                    {
                        case "NF":
                            length = 9;
                            break;

                        case "ICF":
                            length = 6;
                            break;
                    }

                    for (int i = 0; i < workbook.Worksheets.Count; i++)
                    {
                        IWorksheet sheet = workbook.Worksheets[i];

                        int counter = 2;
                        string[] values = GetRowValuesFromSheet(sheet, counter, length);

                        while (!string.IsNullOrWhiteSpace(string.Join(" ", values)))
                        {
                            ICertifiedBeds importRecord = null;
                            DateTime effectiveDate = DateTime.Parse(values[2]);
                            effectiveDate = new DateTime(effectiveDate.Year, effectiveDate.Month, effectiveDate.Day);
                            switch (SimplifiedType)
                            {
                                case "NF":
                                    importRecord = new CertifiedBedsNF
                                    {
                                        VendorProviderId = int.Parse(values[0]),
                                        ProviderNumber = values[1],
                                        EffectiveDate = effectiveDate,
                                        Status = values[3],
                                        BedCountNF = int.Parse(values[4]),
                                        BedCountDual = int.Parse(values[5]),
                                        BedCountTotal = int.Parse(values[6]),
                                        Comments = values[7]
                                    };
                                    break;

                                case "ICF":
                                    importRecord = new CertifiedBedsICF
                                    {
                                        VendorProviderId = int.Parse(values[0]),
                                        ProviderNumber = values[1],
                                        EffectiveDate = effectiveDate,
                                        Status = values[3],
                                        BedCountTotal = int.Parse(values[4]),
                                        Comments = values[5]
                                    };
                                    break;
                            }

                            certifiedBeds.Add(importRecord);

                            values = GetRowValuesFromSheet(sheet, ++counter, length);
                        }
                    }
                }
                RateBL.ImportCertifiedBeds(rateProviders, certifiedBeds);

                ARUMHandler.ClearCache();
                rateProviders.Clear();
                certifiedBeds.Clear();
                return Json(new { Success = true });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                if (!(e is ThreadAbortException))
                {
                    var exMessage = e.InnerException != null ? $"{e.Message} Inner Exception: {e.InnerException.Message}" : e.Message;
                    Log.Error(e, exMessage);
                }
                ARUMHandler.ClearCache();
                return Json(new { Success = false, Message = "Error occurred importing the file. Please check template for provider type." });
            }
        }

        public JsonResult ImportAdminLicenses(string filepath)
        {
            List<LicenseAdmin> licenses = new List<LicenseAdmin>();

            try
            {
                CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true //false
                };

                //Open existing Excel file
                using (ExcelEngine excelEngine = new ExcelEngine())
                using (MemoryStream ExcelMemStream = new MemoryStream())
                using (FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    file.CopyTo(ExcelMemStream);

                    Syncfusion.XlsIO.IApplication app = excelEngine.Excel;
                    app.DefaultVersion = ExcelVersion.Xlsx;

                    ExcelMemStream.Position = 0;
                    IWorkbook workbook = app.Workbooks.Open(ExcelMemStream, ExcelOpenType.Automatic, ExcelParseOptions.Default);

                    int length = 13;
                    for (int i = 0; i < workbook.Worksheets.Count; i++)
                    {
                        IWorksheet sheet = workbook.Worksheets[i];

                        int counter = 2;
                        string[] values = GetRowValuesFromSheet(sheet, counter, length);

                        while (!string.IsNullOrWhiteSpace(string.Join(" ", values)))
                        {
                            bool importFlag = true;
                            string license = values[0];
                            string ssn = values[1];
                            string name = values[2];

                            Common.StringUtlities.ParseProperName(name, out string firstName, out string lastName, out string suffix);
                            if (!string.IsNullOrWhiteSpace(suffix)) lastName += " " + suffix;

                            if (ssn == "*********") ssn = "999999999";

                            if (importFlag)
                            {
                                LicenseAdmin importRecord = new LicenseAdmin
                                {
                                    AdminLicenseNumber = license,
                                    AdminUserNumber = ssn,
                                    AdminFirstName = firstName,
                                    AdminLastName = lastName,
                                };

                                licenses.Add(importRecord);
                            }

                            values = GetRowValuesFromSheet(sheet, ++counter, length);
                        }
                    }
                }
                RateBL.ImportAdminLicenses(licenses);

                ARUMHandler.ClearCache();
                return Json(new { Success = true, Data = licenses });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                if (!(e is ThreadAbortException))
                {
                    var exMessage = e.InnerException != null ? $"{e.Message} Inner Exception: {e.InnerException.Message}" : e.Message;
                    Log.Error(e, exMessage);
                }
                ARUMHandler.ClearCache();
                return Json(new { Success = false, Message = "Error occurred importing the file. Please check template for provider type." });
            }
            finally
            {
                licenses.Clear();
            }
        }

        public JsonResult ImportLicensedBeds(string type, string filepath)
        {
            try
            {
                CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true //false
                };

                #region Get Provider Info
                ARUMApplicationServiceProxy _arumApplicationClient = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);

                IAllProviders providers = _arumApplicationClient.GetAllActiveProvidersWithOptions(RetrieveSession.Classification.ARUMToken,
                    false, false, false, false, false, false, false,
                    "Management/ImportHistorical");
                #endregion

                // Get the ARUM provider information into a form the business layer can use.
                List<RateProvider> rateProviders = new List<RateProvider>();
                foreach (var provider in providers.lstProviders)
                {
                    if (!provider.IsActive) continue;
                    rateProviders.Add(new RateProvider
                    {
                        ProviderId = provider.ProviderID,
                        ProviderName = provider.Name,
                        ProviderNumber = provider.MCAID
                    });
                }

                List<LicensedBedModel> licensedBeds = new List<LicensedBedModel>();

                //Open existing Excel file
                using (ExcelEngine excelEngine = new ExcelEngine())
                using (MemoryStream ExcelMemStream = new MemoryStream())
                using (FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    file.CopyTo(ExcelMemStream);

                    Syncfusion.XlsIO.IApplication app = excelEngine.Excel;
                    app.DefaultVersion = ExcelVersion.Xlsx;

                    ExcelMemStream.Position = 0;
                    IWorkbook workbook = app.Workbooks.Open(ExcelMemStream, ExcelOpenType.Automatic, ExcelParseOptions.Default);

                    int length = 13;
                    for (int i = 0; i < workbook.Worksheets.Count; i++)
                    {
                        IWorksheet sheet = workbook.Worksheets[i];

                        int counter = 2;
                        string[] values = GetRowValuesFromSheet(sheet, counter, length);

                        while (!string.IsNullOrWhiteSpace(string.Join(" ", values)))
                        {
                            bool importFlag = true;
                            if (!DateTime.TryParse(values[1], out DateTime effDate)) importFlag = false;

                            int regHospital = 0,
                                countOther = 0,
                                countNf = 0,
                                totalLicBeds = 0,
                                totalProjectBeds = 0,
                                countIcf = 0,
                                residentialCare = 0,
                                outOfService = 0,
                                onHold = 0;

                            switch (type)
                            {
                                case "ICF":
                                    if (!int.TryParse(values[3], out countNf)) countNf = 0;
                                    if (!int.TryParse(values[4], out regHospital)) regHospital = 0;
                                    if (!int.TryParse(values[5], out countOther)) countOther = 0;
                                    if (!int.TryParse(values[6], out countIcf)) countIcf = 0;
                                    if (!int.TryParse(values[7], out totalLicBeds)) totalLicBeds = 0;
                                    if (!int.TryParse(values[8], out totalProjectBeds)) totalProjectBeds = 0;
                                    if (!int.TryParse(values[9], out residentialCare)) residentialCare = 0;
                                    if (!int.TryParse(values[10], out outOfService)) outOfService = 0;
                                    if (!int.TryParse(values[11], out onHold)) onHold = 0;
                                    break;

                                //case "NF":
                                default:
                                    if (!int.TryParse(values[3], out countNf)) countNf = 0;
                                    if (!int.TryParse(values[4], out regHospital)) regHospital = 0;
                                    if (!int.TryParse(values[5], out countOther)) countOther = 0;
                                    if (!int.TryParse(values[6], out totalProjectBeds)) totalProjectBeds = 0;
                                    if (!int.TryParse(values[7], out countIcf)) countIcf = 0;
                                    if (!int.TryParse(values[8], out totalLicBeds)) totalLicBeds = 0;
                                    if (!int.TryParse(values[9], out residentialCare)) residentialCare = 0;
                                    if (!int.TryParse(values[10], out outOfService)) outOfService = 0;
                                    if (!int.TryParse(values[11], out onHold)) onHold = 0;
                                    break;

                            }
                            if (importFlag)
                            {
                                LicensedBedModel importRecord = new LicensedBedModel
                                {
                                    ProviderNumber = values[0],
                                    EffectiveDate = effDate,
                                    Status = values[2],
                                    CountNF = countNf,
                                    CountHospital = regHospital,
                                    CountOther = countOther,
                                    CountICF = countIcf,
                                    CountLicensedTotal = totalLicBeds,
                                    CountProjectTotal = totalProjectBeds,
                                    CountResidential = residentialCare,
                                    CountOutOfService = outOfService,
                                    CountOnHold = onHold,
                                    Comments = !string.IsNullOrWhiteSpace(values[12]) ? values[12] : null
                                };

                                licensedBeds.Add(importRecord);
                            }

                            values = GetRowValuesFromSheet(sheet, ++counter, length);
                        }
                    }
                }
                RateBL.ImportLicensedBeds(rateProviders, licensedBeds);

                ARUMHandler.ClearCache();
                licensedBeds.Clear();
                return Json(new { Success = true });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                if (!(e is ThreadAbortException))
                {
                    var exMessage = e.InnerException != null ? $"{e.Message} Inner Exception: {e.InnerException.Message}" : e.Message;
                    Log.Error(e, exMessage);
                }
                ARUMHandler.ClearCache();
                return Json(new { Success = false, Message = "Error occurred importing the file. Please check template for provider type." });
            }
        }

        [ARUMFeatureActionFilter("RateSetting:View")]
        public JsonResult ImportBedTransfers(string type, string filepath)
        {
            try
            {
                CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true //false
                };

                #region Get Provider Info
                ARUMApplicationServiceProxy _arumApplicationClient = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);

                IAllProviders providers = _arumApplicationClient.GetAllActiveProvidersWithOptions(RetrieveSession.Classification.ARUMToken,
                    false, false, false, false, false, false, false,
                    "Management/ImportHistorical");
                #endregion

                // Get the ARUM provider information into a form the business layer can use.
                List<RateProvider> rateProviders = new List<RateProvider>();
                foreach (var provider in providers.lstProviders)
                {
                    if (!provider.IsActive) continue;
                    rateProviders.Add(new RateProvider
                    {
                        ProviderId = provider.ProviderID,
                        ProviderName = provider.Name,
                        ProviderNumber = provider.MCAID
                    });
                }

                List<CertifiedBedTransfers> bedTransfers = new List<CertifiedBedTransfers>();

                //Open existing Excel file
                using (ExcelEngine excelEngine = new ExcelEngine())
                using (MemoryStream ExcelMemStream = new MemoryStream())
                using (FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    file.CopyTo(ExcelMemStream);

                    Syncfusion.XlsIO.IApplication app = excelEngine.Excel;
                    app.DefaultVersion = ExcelVersion.Xlsx;

                    ExcelMemStream.Position = 0;
                    IWorkbook workbook = app.Workbooks.Open(ExcelMemStream, ExcelOpenType.Automatic, ExcelParseOptions.Default);

                    int length = 0;
                    switch (SimplifiedType)
                    {
                        case "NF":
                            length = 6;
                            break;

                        case "ICF":
                            length = 6;
                            break;
                    }

                    for (int i = 0; i < workbook.Worksheets.Count; i++)
                    {
                        IWorksheet sheet = workbook.Worksheets[i];

                        int counter = 2;
                        string[] values = GetRowValuesFromSheet(sheet, counter, length);

                        while (!string.IsNullOrWhiteSpace(string.Join(" ", values)))
                        {
                            CertifiedBedTransfers importRecord = null;
                            string providerNumber = string.Empty;

                            switch (SimplifiedType)
                            {
                                case "NF":
                                case "ICF":
                                    providerNumber = values[1];

                                    importRecord = new CertifiedBedTransfers
                                    {
                                        ProviderId = rateProviders.FirstOrDefault(x => x.ProviderNumber == values[0] && x.IsEnabled)?.ProviderId ?? 0,
                                        ProviderFromNumber = providerNumber,
                                        EffectiveDate = DateTime.Parse(values[2]),
                                        CertifiedBedsTransferred = int.Parse(values[3]),
                                        ProviderFromName = rateProviders.FirstOrDefault(x => x.ProviderNumber == providerNumber)?.ProviderName ?? string.Empty,
                                        Comments = values[5]
                                    };
                                    break;
                            }

                            bedTransfers.Add(importRecord);

                            values = GetRowValuesFromSheet(sheet, ++counter, length);
                        }
                    }
                }
                RateBL.ImportCertifiedBedTransfers(bedTransfers);

                ARUMHandler.ClearCache();
                rateProviders.Clear();
                bedTransfers.Clear();
                return Json(new { Success = true });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                if (!(e is ThreadAbortException))
                {
                    var exMessage = e.InnerException != null ? $"{e.Message} Inner Exception: {e.InnerException.Message}" : e.Message;
                    Log.Error(e, exMessage);
                }
                ARUMHandler.ClearCache();
                return Json(new { Success = false, Message = "Error occurred importing the file. Please check template for provider type." });
            }
        }

        /// <summary>
        /// Generate the "HistImport" stored procedure for the current provider type
        /// for the specified document template year. This is required to process the
        /// historical import process. 
        /// NOTE: This should be done DURING DEVELOPMENT and the result files deployed
        /// with the rest of the website.
        /// </summary>
        /// <param name="year">The document template year represented as a string</param>
        /// <returns></returns>
        [ARUMFeatureActionFilter("CostReport:Edit")]
        public JsonResult GenerateHistoricalImportProcedures(string year)
        {
            try
            {
                string documentType = string.Empty;
                string facilityType = string.Empty;
                string type = string.Empty;
                SqlConnectionStringBuilder strBuilder = new SqlConnectionStringBuilder(Functions.DatabaseConnection);

                // Setup values based on original connection string information.
                if (strBuilder["Database"].ToString().ToUpper().Contains("ICF"))
                {
                    documentType = "I";
                    facilityType = "2";
                    type = "ICF";
                }
                else if (strBuilder["Database"].ToString().ToUpper().Contains("NF") ||
                    strBuilder["Database"].ToString().ToUpper().Contains("OHFI"))
                {
                    documentType = "N";
                    facilityType = "1";
                    type = "NF";
                }
                else
                {
                    // If we reach here, then this is not a supported database for this process.
                    return Json(new
                    {
                        Success = true,
                        Message = "No Import Available."
                    });
                }

                List<string> sqlPackage = new List<string>
                {
                    $"IF NOT EXISTS ( SELECT * FROM sysobjects WHERE id = object_id(N'[dbo].[HistImport{type}]') and OBJECTPROPERTY(id, N'IsProcedure') = 1 )",
                    "BEGIN",
                    $"exec('CREATE PROCEDURE [dbo].[HistImport{type}] AS BEGIN SET NOCOUNT ON; END')",
                    "END",
                    "EXEC('",
                    $"ALTER PROCEDURE [dbo].[HistImport{type}] ",
                    //"    ( @DocumentNumber\tVARCHAR(20) = NULL",
                    //"    )",
                    "AS",
                    "BEGIN",
                    $"    DECLARE @FacilityType int =          {facilityType};",
                    $"    DECLARE @DocumentType nvarchar(5) = ''{documentType}'';",
                    "",
                    //"    IF (NOT EXISTS(SELECT 1 FROM [CR_FACIL_TYPE] WHERE [DOC_NUM] = @DocumentNumber AND [FACIL_TYPE] = @FacilityType)) RETURN;",
                    //"",
                };

                if (type == "NF" && (year == "2020" || year == "2021"))
                {
                    sqlPackage.AddRange(GenerateHistoricalImportNfAtt12());
                    sqlPackage.Add("");
                }

                // Get the working DB and update connection string to match.
                string workingDb = OHFISession.ImportHistoricalWorkingDb;
                strBuilder["Database"] = workingDb;

                // Reminder - this is the working DB.
                using (SqlConnection conn = new SqlConnection(strBuilder.ConnectionString))
                {
                    conn.Open();

                    // Get the *possible* tables and columns we need to use for import
                    List<DenRecord> dens = GetDataElementNumberDataForTemplate(conn, documentType, year);

                    string tableCurrent = string.Empty;

                    sqlPackage.Add("    -- Provider MEDICAID number from PROV_XREF");
                    sqlPackage.Add("    INSERT INTO [DocumentData] ( [DOC_NUM], [DocumentType], [DocumentTemplateId], [FieldTemplateId], [RangeName], [DataValue] )");
                    sqlPackage.Add("    SELECT base.[DOC_NUM], ft.[DocumentType], ft.[DocumentTemplateId], ft.[FieldTemplateId], ft.[RangeName], xref.[SRC_PROV_NUM]");
                    sqlPackage.Add("    FROM [DOCUMENT] base");
                    sqlPackage.Add("    LEFT JOIN [CR_FACIL_TYPE] cft on base.[DOC_NUM] = cft.[DOC_NUM]");
                    sqlPackage.Add("    LEFT JOIN [PROV_XREF] xref on xref.[PROV_ID] = base.[PROV_ID]");
                    sqlPackage.Add("    OUTER APPLY (");
                    sqlPackage.Add("        SELECT TOP 1 [DocumentTemplateId], [DocumentType], [FieldTemplateId], [RangeName] FROM [FieldTemplate]");
                    sqlPackage.Add("        WHERE [DEN] = ''10020'' AND [DocumentType] = @DocumentType");
                    sqlPackage.Add("    ) ft");
                    sqlPackage.Add("    WHERE cft.[FACIL_TYPE] = @FacilityType AND ft.[RangeName] IS NOT NULL AND base.[PROV_ID] IS NOT NULL"); // AND base.[DOC_NUM] = @DocumentNumber");
                    sqlPackage.Add("");

                    // Loop through them, validate each table and column, and then generate the SQL for it.
                    foreach (DenRecord den in dens)
                    {
                        // If this is a "new table", we need to break because we have SQL "IF" statements 
                        // to handle if table were not actually imported (were not provided by the client).
                        if (tableCurrent != den.TABLE_NAME)
                        {
                            // If this is actually the first table, there is nothing to end here!
                            if (!string.IsNullOrWhiteSpace(tableCurrent))
                            {
                                sqlPackage.Add("");
                            }

                            // Setup for this table. 
                            tableCurrent = den.TABLE_NAME;
                            //// This SQL "IF" statement will prevent tables that are "known" by the DATA_ELEM_NUMBER records
                            //// but not actually provided by the client to be skipped safely without crashing the process.
                        }
                        switch (den.DATA_ELEM_NUM) 
                        {
                            case "G64750":
                            case "G63450":
                            case "G63370":
                            case "G63410":
                            case "G63330":
                            case "G63490":
                                // These are Attachement 11 items for COVID. Because of the rush in implementating these
                                // by the state, they do not follow the same process as the other DENs, so they have to 
                                // be handled separately. These totals cannot be allowed to pickup the last line value
                                // from the table; they should be blank.
                                break;

                            case "63310":
                            case "63320":
                            case "63350":
                            case "63360":
                            case "63390":
                            case "63400":
                            case "63430":
                            case "63440":
                                // These are Attachement 11 items for COVID. Because of the rush in implementating these
                                // by the state, they do not follow the same process as the other DENs, so they have to 
                                // be handled separately.
                                sqlPackage.Add("    INSERT INTO [DocumentData] ( [DOC_NUM], [DocumentType], [DocumentTemplateId], [FieldTemplateId], [RangeName], [DataValue] )");
                                sqlPackage.Add($"    SELECT base.[DOC_NUM], ft.[DocumentType], ft.[DocumentTemplateId], ft.[FieldTemplateId], ft.[RangeName], base.[{den.COL_NAME}]");
                                sqlPackage.Add($"    FROM [{den.TABLE_NAME}] base");
                                sqlPackage.Add("    LEFT JOIN [CR_FACIL_TYPE] cft on base.[DOC_NUM] = cft.[DOC_NUM]");
                                sqlPackage.Add("    OUTER APPLY (");
                                sqlPackage.Add("        SELECT TOP 1 [DocumentTemplateId], [DocumentType], [FieldTemplateId], at.[RangeName] ");
                                sqlPackage.Add("        FROM [FieldTemplate] f ");
                                sqlPackage.Add("        LEFT JOIN @Attachment12CrossWalk at on at.[RangeName] = f.[RangeName] ");
                                sqlPackage.Add($"        WHERE [DEN] = ''{den.DATA_ELEM_NUM}'' AND [DocumentType] = @DocumentType");
                                sqlPackage.Add("        AND at.[CHART_OF_ACCT_NUM] = base.[CHART_OF_ACCT_NUM] ");
                                sqlPackage.Add("    ) ft");
                                sqlPackage.Add($"    WHERE cft.[FACIL_TYPE] = @FacilityType AND ft.[RangeName] IS NOT NULL AND base.[{den.COL_NAME}] IS NOT NULL"); // AND base.[DOC_NUM] = @DocumentNumber");
                                if (!string.IsNullOrWhiteSpace(den.SUBKEY_1_NAME)) sqlPackage.Add($"        AND TRIM(base.[{den.SUBKEY_1_NAME}]) = ''{den.SUBKEY_1}''");
                                if (!string.IsNullOrWhiteSpace(den.SUBKEY_2_NAME)) sqlPackage.Add($"        AND TRIM(base.[{den.SUBKEY_2_NAME}]) = ''{den.SUBKEY_2}''");
                                if (den.COL_NAME.Contains("CHART_OF_ACCT_NUM")) sqlPackage.Add($"        AND base.[{den.COL_NAME}] <> ''0''");
                                break;

                            case "T41705":
                            case "T41715":
                                sqlPackage.Add("    INSERT INTO [DocumentData] ( [DOC_NUM], [DocumentType], [DocumentTemplateId], [FieldTemplateId], [RangeName], [DataValue] )");
                                sqlPackage.Add($"    SELECT base.[DOC_NUM], ft.[DocumentType], ft.[DocumentTemplateId], ft.[FieldTemplateId], ft.[RangeName], ");
                                sqlPackage.Add("        CASE ");
                                sqlPackage.Add($"            WHEN base.[{den.COL_NAME}] IS NULL THEN ''''");
                                sqlPackage.Add($"            WHEN base.[{den.COL_NAME}] = ''0'' THEN ''0''");
                                sqlPackage.Add($"            WHEN SUBSTRING(base.[{den.COL_NAME}], 1, 1) = ''-'' THEN SUBSTRING(base.[{den.COL_NAME}], 2, LEN(base.[{den.COL_NAME}]) - 1)");
                                sqlPackage.Add($"            WHEN SUBSTRING(base.[{den.COL_NAME}], 1, 1) <> ''-'' THEN ''-'' + base.[{den.COL_NAME}]");
                                sqlPackage.Add("        END ");
                                sqlPackage.Add($"    FROM [{den.TABLE_NAME}] base");
                                sqlPackage.Add("    LEFT JOIN [CR_FACIL_TYPE] cft on base.[DOC_NUM] = cft.[DOC_NUM]");
                                sqlPackage.Add("    OUTER APPLY (");
                                sqlPackage.Add("        SELECT TOP 1 [DocumentTemplateId], [DocumentType], [FieldTemplateId], [RangeName] FROM [FieldTemplate]");
                                sqlPackage.Add($"        WHERE [DEN] = ''{den.DATA_ELEM_NUM}'' AND [DocumentType] = @DocumentType");
                                if (!string.IsNullOrWhiteSpace(den.SUFFIX_NAME1))
                                {
                                    if (!string.IsNullOrWhiteSpace(den.SUFFIX_NAME2)) sqlPackage.Add($"        AND [Suffix] = base.[{den.SUFFIX_NAME1}] + base.[{den.SUFFIX_NAME2}]");
                                    else sqlPackage.Add($"        AND [Suffix] = base.[{den.SUFFIX_NAME1}]");
                                }
                                sqlPackage.Add("    ) ft");
                                sqlPackage.Add($"    WHERE cft.[FACIL_TYPE] = @FacilityType AND ft.[RangeName] IS NOT NULL AND base.[{den.COL_NAME}] IS NOT NULL"); // AND base.[DOC_NUM] = @DocumentNumber");
                                if (!string.IsNullOrWhiteSpace(den.SUBKEY_1_NAME)) sqlPackage.Add($"        AND TRIM(base.[{den.SUBKEY_1_NAME}]) = ''{den.SUBKEY_1}''");
                                if (!string.IsNullOrWhiteSpace(den.SUBKEY_2_NAME)) sqlPackage.Add($"        AND TRIM(base.[{den.SUBKEY_2_NAME}]) = ''{den.SUBKEY_2}''");
                                if (den.COL_NAME.Contains("CHART_OF_ACCT_NUM")) sqlPackage.Add($"        AND base.[{den.COL_NAME}] <> ''0''");

                                break;

                            default:
                                sqlPackage.Add("    INSERT INTO [DocumentData] ( [DOC_NUM], [DocumentType], [DocumentTemplateId], [FieldTemplateId], [RangeName], [DataValue] )");
                                sqlPackage.Add($"    SELECT base.[DOC_NUM], ft.[DocumentType], ft.[DocumentTemplateId], ft.[FieldTemplateId], ft.[RangeName], base.[{den.COL_NAME}]");
                                sqlPackage.Add($"    FROM [{den.TABLE_NAME}] base");
                                sqlPackage.Add("    LEFT JOIN [CR_FACIL_TYPE] cft on base.[DOC_NUM] = cft.[DOC_NUM]");
                                sqlPackage.Add("    OUTER APPLY (");
                                sqlPackage.Add("        SELECT TOP 1 [DocumentTemplateId], [DocumentType], [FieldTemplateId], [RangeName] FROM [FieldTemplate]");
                                sqlPackage.Add($"        WHERE [DEN] = ''{den.DATA_ELEM_NUM}'' AND [DocumentType] = @DocumentType");
                                if (!string.IsNullOrWhiteSpace(den.SUFFIX_NAME1))
                                {
                                    if (!string.IsNullOrWhiteSpace(den.SUFFIX_NAME2)) sqlPackage.Add($"        AND [Suffix] = base.[{den.SUFFIX_NAME1}] + base.[{den.SUFFIX_NAME2}]");
                                    else sqlPackage.Add($"        AND [Suffix] = base.[{den.SUFFIX_NAME1}]");
                                }
                                sqlPackage.Add("    ) ft");
                                sqlPackage.Add($"    WHERE cft.[FACIL_TYPE] = @FacilityType AND ft.[RangeName] IS NOT NULL AND base.[{den.COL_NAME}] IS NOT NULL"); // AND base.[DOC_NUM] = @DocumentNumber");
                                if (!string.IsNullOrWhiteSpace(den.SUBKEY_1_NAME)) sqlPackage.Add($"        AND TRIM(base.[{den.SUBKEY_1_NAME}]) = ''{den.SUBKEY_1}''");
                                if (!string.IsNullOrWhiteSpace(den.SUBKEY_2_NAME)) sqlPackage.Add($"        AND TRIM(base.[{den.SUBKEY_2_NAME}]) = ''{den.SUBKEY_2}''");
                                if (den.COL_NAME.Contains("CHART_OF_ACCT_NUM")) sqlPackage.Add($"        AND base.[{den.COL_NAME}] <> ''0''");
                                break;
                        }
                        sqlPackage.Add("");
                    }

                    // Now add "calculated" values that are required to be present, but not stored in the data.
                    // This is specifically for C-1 schedules
                    sqlPackage.Add("    -- This is to save the County Adjustment Percentage that would otherwise be calculated and saved.");
                    sqlPackage.Add("    INSERT INTO [DocumentData] ([DOC_NUM], [DocumentType], [DocumentTemplateId], [FieldTemplateId], [RangeName], [DataValue])");
                    sqlPackage.Add("    SELECT base.[DOC_NUM], ft.[DocumentType], ft.[DocumentTemplateId], ft.[FieldTemplateId], ft.[RangeName], ");
                    sqlPackage.Add("        CASE");
                    sqlPackage.Add("            WHEN base.[COUNTY] IN (''9'', ''18'', ''25'', ''31'', ''48'', ''57'', ''76'')");
                    sqlPackage.Add("            THEN ''6''");
                    sqlPackage.Add("            ELSE ''0''");
                    sqlPackage.Add("        END as [DataValue]");
                    sqlPackage.Add("    FROM [ADDRESS] base");
                    sqlPackage.Add("    LEFT JOIN [CR_FACIL_TYPE] cft on base.[DOC_NUM] = cft.[DOC_NUM]");
                    sqlPackage.Add("    OUTER APPLY (");
                    sqlPackage.Add("        SELECT TOP 1 [DocumentTemplateId], [DocumentType], [FieldTemplateId], [RangeName] FROM [FieldTemplate]");
                    sqlPackage.Add("        WHERE [DEN] = ''C27085'' AND [DocumentType] = @DocumentType");
                    sqlPackage.Add("    ) ft");
                    sqlPackage.Add("    WHERE cft.[FACIL_TYPE] = @FacilityType AND ft.[RangeName] IS NOT NULL AND base.[COUNTY] IS NOT NULL");
                    sqlPackage.Add("        AND TRIM(base.[ADDR_TYPE]) = ''FA''");
                    sqlPackage.Add("");

                    sqlPackage.Add("    --This is to save the Allowance Percentage that would otherwise be calculated and saved.");
                    sqlPackage.Add("    INSERT INTO [DocumentData] ([DOC_NUM], [DocumentType], [DocumentTemplateId], [FieldTemplateId], [RangeName], [DataValue])");
                    sqlPackage.Add("    SELECT base.[DOC_NUM], ft.[DocumentType], ft.[DocumentTemplateId], ft.[FieldTemplateId], ft.[RangeName], ");
                    sqlPackage.Add("    (SELECT MIN(value) FROM(VALUES((100 + ");
                    sqlPackage.Add("        (ISNULL(base.[YEARS_WORK_EXPER], 0) * 4) + ");
                    sqlPackage.Add("        (ISNULL(base.[FORMAL_EDUCATION], 0) * 5) + ");
                    sqlPackage.Add("        ((CASE WHEN base.[DUTY_ACCT] = ''Y'' THEN 1 ELSE 0 END) * 4) + ");
                    sqlPackage.Add("        ((CASE WHEN base.[DUTY_MAINT] = ''Y'' THEN 1 ELSE 0 END) * 4) + ");
                    sqlPackage.Add("        ((CASE WHEN base.[DUTY_HOUSE_KEEP] = ''Y'' THEN 1 ELSE 0 END) * 4) + ");
                    sqlPackage.Add("        ((CASE WHEN base.[DUTY_OTHER_1] = ''Y'' THEN 1 ELSE 0 END) * 4) + ");
                    sqlPackage.Add("        ((CASE WHEN base.[DUTY_OTHER_2] = ''Y'' THEN 1 ELSE 0 END) * 4) + ");
                    sqlPackage.Add("        (CASE WHEN dd.[DataValue] in (''9'', ''18'', ''25'', ''31'', ''48'', ''57'', ''76'') THEN 6 ELSE 0 END))), (150)) AS AllValues(value)) as [DataValue] ");
                    sqlPackage.Add("    FROM [ADMINISTRATOR] base    ");
                    sqlPackage.Add("    LEFT JOIN [CR_FACIL_TYPE] cft on base.[DOC_NUM] = cft.[DOC_NUM]");
                    sqlPackage.Add("    LEFT JOIN [DocumentData] dd on dd.[DOC_NUM] = base.[DOC_NUM] AND dd.[RangeName] = ''10110'' -- County Id range name");
                    sqlPackage.Add("    OUTER APPLY (");
                    sqlPackage.Add("        SELECT TOP 1 [DocumentTemplateId], [DocumentType], [FieldTemplateId], [RangeName] FROM [FieldTemplate]");
                    sqlPackage.Add($"        WHERE [DEN] = ''{"T27125"}'' AND [DocumentType] = @DocumentType");
                    sqlPackage.Add($"        AND [Suffix] = base.[{"SEQUENCE"}]");
                    sqlPackage.Add("    ) ft");
                    sqlPackage.Add($"    WHERE cft.[FACIL_TYPE] = @FacilityType AND ft.[RangeName] IS NOT NULL "); // AND base.[DOC_NUM] = @DocumentNumber");
                    sqlPackage.Add("");
                }

                sqlPackage.Add("");
                sqlPackage.Add("END');");
                System.IO.File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "Sql", "ImportHistorical", $"HistImport{type}{year}.sql"), sqlPackage);
                return Json(new
                {
                    Success = true
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new
                {
                    Success = false,
                    Message = e.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private List<string> GenerateHistoricalImportNfAtt12()
        {
            return new List<string>
            {
                "    DECLARE @Attachment12CrossWalk TABLE ",
                "        ( [CHART_OF_ACCT_NUM]   VARCHAR(4) ",
                "        , [RangeName]           VARCHAR(10) ",
                "        ); ",
                " ",
                "    INSERT INTO @Attachment12CrossWalk ",
                "    VALUES ",
                "        -- Attachment 12 Pt 1, for Schedule B1 ",
                "        (''6060'', ''63310A''), --Amount ",
                "        (''6070'', ''63310B''), ",
                "        (''6080'', ''63310C''), ",
                "        (''6085'', ''63310D''), ",
                "        (''6060'', ''63320A''), --Justification ",
                "        (''6070'', ''63320B''), ",
                "        (''6080'', ''63320C''), ",
                "        (''6085'', ''63320D''), ",
                " ",
                "        --Attachment 12 Pt 1, for Schedule B2 ",
                "        (''6100'', ''63350AA''),    --Amount ",
                "        (''6105'', ''63350AB''), ",
                "        (''6110'', ''63350AC''), ",
                "        (''6115'', ''63350AD''), ",
                "        (''6120'', ''63350AE''), ",
                "        (''6125'', ''63350AF''), ",
                "        (''6130'', ''63350AG''), ",
                "        (''6170'', ''63350AH''), ",
                "        (''6185'', ''63350AI''), ",
                "        (''6205'', ''63350AJ''), ",
                "        (''6210'', ''63350AK''), ",
                "        (''6220'', ''63350AL''), ",
                "        (''6230'', ''63350AM''), ",
                "        (''6301'', ''63350AN''), ",
                "        (''6311'', ''63350AO''), ",
                "        (''6321'', ''63350AP''), ",
                "        (''6330'', ''63350AQ''), ",
                "        (''6340'', ''63350AR''), ",
                "        (''6401'', ''63350AS''), ",
                "        (''6411'', ''63350AT''), ",
                "        (''6421'', ''63350AU''), ",
                "        (''6500'', ''63350AV''), ",
                "        (''6511'', ''63350AW''), ",
                "        (''6521'', ''63350AX''), ",
                "        (''6531'', ''63350AY''), ",
                "        (''6541'', ''63350AZ''), ",
                "        (''6551'', ''63350BA''), ",
                "        (''6560'', ''63350BB''), ",
                "        (''6570'', ''63350BC''), ",
                "        (''6580'', ''63350BD''), ",
                "        (''6590'', ''63350BE''), ",
                "        (''6600'', ''63350BF''), ",
                "        (''6605'', ''63350BG''), ",
                "        (''6610'', ''63350BH''), ",
                "        (''6615'', ''63350BI''), ",
                "        (''6620'', ''63350BJ''), ",
                "        (''6630'', ''63350BK''), ",
                "        (''6640'', ''63350BL''), ",
                "        (''6650'', ''63350BM''), ",
                "        (''6660'', ''63350BN''), ",
                "        (''6665'', ''63350BO''), ",
                "        (''6670'', ''63350BP''), ",
                "        (''6680'', ''63350BQ''), ",
                "        (''6700'', ''63350BR''), ",
                "        (''6710'', ''63350BS''), ",
                "        (''6720'', ''63350BT''), ",
                "        (''6730'', ''63350BU''), ",
                "        (''6740'', ''63350BV''), ",
                "        (''6750'', ''63350BW''), ",
                "        (''6100'', ''63360AA''),    --Justification ",
                "        (''6105'', ''63360AB''), ",
                "        (''6110'', ''63360AC''), ",
                "        (''6115'', ''63360AD''), ",
                "        (''6120'', ''63360AE''), ",
                "        (''6125'', ''63360AF''), ",
                "        (''6130'', ''63360AG''), ",
                "        (''6170'', ''63360AH''), ",
                "        (''6185'', ''63360AI''), ",
                "        (''6205'', ''63360AJ''), ",
                "        (''6210'', ''63360AK''), ",
                "        (''6220'', ''63360AL''), ",
                "        (''6230'', ''63360AM''), ",
                "        (''6301'', ''63360AN''), ",
                "        (''6311'', ''63360AO''), ",
                "        (''6321'', ''63360AP''), ",
                "        (''6330'', ''63360AQ''), ",
                "        (''6340'', ''63360AR''), ",
                "        (''6401'', ''63360AS''), ",
                "        (''6411'', ''63360AT''), ",
                "        (''6421'', ''63360AU''), ",
                "        (''6500'', ''63360AV''), ",
                "        (''6511'', ''63360AW''), ",
                "        (''6521'', ''63360AX''), ",
                "        (''6531'', ''63360AY''), ",
                "        (''6541'', ''63360AZ''), ",
                "        (''6551'', ''63360BA''), ",
                "        (''6560'', ''63360BB''), ",
                "        (''6570'', ''63360BC''), ",
                "        (''6580'', ''63360BD''), ",
                "        (''6590'', ''63360BE''), ",
                "        (''6600'', ''63360BF''), ",
                "        (''6605'', ''63360BG''), ",
                "        (''6610'', ''63360BH''), ",
                "        (''6615'', ''63360BI''), ",
                "        (''6620'', ''63360BJ''), ",
                "        (''6630'', ''63360BK''), ",
                "        (''6640'', ''63360BL''), ",
                "        (''6650'', ''63360BM''), ",
                "        (''6660'', ''63360BN''), ",
                "        (''6665'', ''63360BO''), ",
                "        (''6670'', ''63360BP''), ",
                "        (''6680'', ''63360BQ''), ",
                "        (''6700'', ''63360BR''), ",
                "        (''6710'', ''63360BS''), ",
                "        (''6720'', ''63360BT''), ",
                "        (''6730'', ''63360BU''), ",
                "        (''6740'', ''63360BV''), ",
                "        (''6750'', ''63360BW''), ",
                " ",
                "        --Attachment 12 Pt 2, for Schedule C ",
                "        (''7000'', ''63390AA''),    --Amount ",
                "        (''7005'', ''63390AB''), ",
                "        (''7015'', ''63390AC''), ",
                "        (''7025'', ''63390AD''), ",
                "        (''7030'', ''63390AE''), ",
                "        (''7035'', ''63390AF''), ",
                "        (''7040'', ''63390AG''), ",
                "        (''7045'', ''63390AH''), ",
                "        (''7050'', ''63390AI''), ",
                "        (''7055'', ''63390AJ''), ",
                "        (''7056'', ''63390AK''), ",
                "        (''7060'', ''63390AL''), ",
                "        (''7065'', ''63390AM''), ",
                "        (''7070'', ''63390AN''), ",
                "        (''7075'', ''63390AO''), ",
                "        (''7080'', ''63390AP''), ",
                "        (''7090'', ''63390AQ''), ",
                "        (''7105'', ''63390AR''), ",
                "        (''7110'', ''63390AS''), ",
                "        (''7115'', ''63390AT''), ",
                "        (''7120'', ''63390AU''), ",
                "        (''7125'', ''63390AV''), ",
                "        (''7201'', ''63390AW''), ",
                "        (''7211'', ''63390AX''), ",
                "        (''7221'', ''63390AY''), ",
                "        (''7231'', ''63390AZ''), ",
                "        (''7241'', ''63390BA''), ",
                "        (''7251'', ''63390BB''), ",
                "        (''7261'', ''63390BC''), ",
                "        (''7271'', ''63390BD''), ",
                "        (''7281'', ''63390BE''), ",
                "        (''7301'', ''63390BF''), ",
                "        (''7302'', ''63390BG''), ",
                "        (''7501'', ''63390BH''), ",
                "        (''7511'', ''63390BI''), ",
                "        (''7521'', ''63390BJ''), ",
                "        (''7531'', ''63390BK''), ",
                "        (''7600'', ''63390BL''), ",
                "        (''7605'', ''63390BM''), ",
                "        (''7610'', ''63390BN''), ",
                "        (''7615'', ''63390BO''), ",
                "        (''7620'', ''63390BP''), ",
                "        (''7625'', ''63390BQ''), ",
                "        (''7630'', ''63390BR''), ",
                "        (''7631'', ''63390BS''), ",
                "        (''7635'', ''63390BT''), ",
                "        (''7640'', ''63390BU''), ",
                "        (''7645'', ''63390BV''), ",
                "        (''7650'', ''63390BW''), ",
                "        (''7655'', ''63390BX''), ",
                "        (''7660'', ''63390BY''), ",
                "        (''7665'', ''63390BZ''), ",
                "        (''7670'', ''63390CA''), ",
                "        (''7675'', ''63390CB''), ",
                "        (''7680'', ''63390CC''), ",
                "        (''7685'', ''63390CD''), ",
                "        (''7686'', ''63390CE''), ",
                "        (''7690'', ''63390CF''), ",
                "        (''7695'', ''63390CG''), ",
                "        (''7700'', ''63390CH''), ",
                "        (''7710'', ''63390CI''), ",
                "        (''7720'', ''63390CJ''), ",
                "        (''7730'', ''63390CK''), ",
                "        (''7740'', ''63390CL''), ",
                "        (''7800'', ''63390CM''), ",
                "        (''7810'', ''63390CN''), ",
                "        (''7820'', ''63390CO''), ",
                "        (''7830'', ''63390CP''), ",
                "        (''7840'', ''63390CQ''), ",
                "        (''7850'', ''63390CR''), ",
                "        (''9705'', ''63390CS''), ",
                "        (''9710'', ''63390CT''), ",
                "        (''9715'', ''63390CU''), ",
                "        (''9720'', ''63390CV''), ",
                "        (''9725'', ''63390CW''), ",
                "        (''9730'', ''63390CX''), ",
                "        (''9735'', ''63390CY''), ",
                "        (''9740'', ''63390CZ''), ",
                "        (''9745'', ''63390DA''), ",
                "        (''9750'', ''63390DB''), ",
                "        (''9755'', ''63390DC''), ",
                "        (''9760'', ''63390DD''), ",
                "        (''9765'', ''63390DE''), ",
                "        (''9770'', ''63390DF''), ",
                "        (''9776'', ''63390DG''), ",
                "        (''7000'', ''63400AA''),    --Justification ",
                "        (''7005'', ''63400AB''), ",
                "        (''7015'', ''63400AC''), ",
                "        (''7025'', ''63400AD''), ",
                "        (''7030'', ''63400AE''), ",
                "        (''7035'', ''63400AF''), ",
                "        (''7040'', ''63400AG''), ",
                "        (''7045'', ''63400AH''), ",
                "        (''7050'', ''63400AI''), ",
                "        (''7055'', ''63400AJ''), ",
                "        (''7056'', ''63400AK''), ",
                "        (''7060'', ''63400AL''), ",
                "        (''7065'', ''63400AM''), ",
                "        (''7070'', ''63400AN''), ",
                "        (''7075'', ''63400AO''), ",
                "        (''7080'', ''63400AP''), ",
                "        (''7090'', ''63400AQ''), ",
                "        (''7105'', ''63400AR''), ",
                "        (''7110'', ''63400AS''), ",
                "        (''7115'', ''63400AT''), ",
                "        (''7120'', ''63400AU''), ",
                "        (''7125'', ''63400AV''), ",
                "        (''7201'', ''63400AW''), ",
                "        (''7211'', ''63400AX''), ",
                "        (''7221'', ''63400AY''), ",
                "        (''7231'', ''63400AZ''), ",
                "        (''7241'', ''63400BA''), ",
                "        (''7251'', ''63400BB''), ",
                "        (''7261'', ''63400BC''), ",
                "        (''7271'', ''63400BD''), ",
                "        (''7281'', ''63400BE''), ",
                "        (''7301'', ''63400BF''), ",
                "        (''7302'', ''63400BG''), ",
                "        (''7501'', ''63400BH''), ",
                "        (''7511'', ''63400BI''), ",
                "        (''7521'', ''63400BJ''), ",
                "        (''7531'', ''63400BK''), ",
                "        (''7600'', ''63400BL''), ",
                "        (''7605'', ''63400BM''), ",
                "        (''7610'', ''63400BN''), ",
                "        (''7615'', ''63400BO''), ",
                "        (''7620'', ''63400BP''), ",
                "        (''7625'', ''63400BQ''), ",
                "        (''7630'', ''63400BR''), ",
                "        (''7631'', ''63400BS''), ",
                "        (''7635'', ''63400BT''), ",
                "        (''7640'', ''63400BU''), ",
                "        (''7645'', ''63400BV''), ",
                "        (''7650'', ''63400BW''), ",
                "        (''7655'', ''63400BX''), ",
                "        (''7660'', ''63400BY''), ",
                "        (''7665'', ''63400BZ''), ",
                "        (''7670'', ''63400CA''), ",
                "        (''7675'', ''63400CB''), ",
                "        (''7680'', ''63400CC''), ",
                "        (''7685'', ''63400CD''), ",
                "        (''7686'', ''63400CE''), ",
                "        (''7690'', ''63400CF''), ",
                "        (''7695'', ''63400CG''), ",
                "        (''7700'', ''63400CH''), ",
                "        (''7710'', ''63400CI''), ",
                "        (''7720'', ''63400CJ''), ",
                "        (''7730'', ''63400CK''), ",
                "        (''7740'', ''63400CL''), ",
                "        (''7800'', ''63400CM''), ",
                "        (''7810'', ''63400CN''), ",
                "        (''7820'', ''63400CO''), ",
                "        (''7830'', ''63400CP''), ",
                "        (''7840'', ''63400CQ''), ",
                "        (''7850'', ''63400CR''), ",
                "        (''9705'', ''63400CS''), ",
                "        (''9710'', ''63400CT''), ",
                "        (''9715'', ''63400CU''), ",
                "        (''9720'', ''63400CV''), ",
                "        (''9725'', ''63400CW''), ",
                "        (''9730'', ''63400CX''), ",
                "        (''9735'', ''63400CY''), ",
                "        (''9740'', ''63400CZ''), ",
                "        (''9745'', ''63400DA''), ",
                "        (''9750'', ''63400DB''), ",
                "        (''9755'', ''63400DC''), ",
                "        (''9760'', ''63400DD''), ",
                "        (''9765'', ''63400DE''), ",
                "        (''9770'', ''63400DF''), ",
                "        (''9776'', ''63400DG''), ",
                " ",
                "        --Attachment 12 Pt 2, for Schedule D ",
                "        (''8010'', ''63430A''), --Amount ",
                "        (''8020'', ''63430B''), ",
                "        (''8030'', ''63430C''), ",
                "        (''8040'', ''63430D''), ",
                "        (''8050'', ''63430E''), ",
                "        (''8060'', ''63430F''), ",
                "        (''8065'', ''63430G''), ",
                "        (''8070'', ''63430H''), ",
                "        (''8080'', ''63430I''), ",
                "        (''8085'', ''63430J''), ",
                "        (''8090'', ''63430K''), ",
                "        (''8110'', ''63430L''), ",
                "        (''8140'', ''63430M''), ",
                "        (''8170'', ''63430N''), ",
                "        (''8180'', ''63430O''), ",
                "        (''8195'', ''63430P''), ",
                "        (''8010'', ''63440A''), --Justification ",
                "        (''8020'', ''63440B''), ",
                "        (''8030'', ''63440C''), ",
                "        (''8040'', ''63440D''), ",
                "        (''8050'', ''63440E''), ",
                "        (''8060'', ''63440F''), ",
                "        (''8065'', ''63440G''), ",
                "        (''8070'', ''63440H''), ",
                "        (''8080'', ''63440I''), ",
                "        (''8085'', ''63440J''), ",
                "        (''8090'', ''63440K''), ",
                "        (''8110'', ''63440L''), ",
                "        (''8140'', ''63440M''), ",
                "        (''8170'', ''63440N''), ",
                "        (''8180'', ''63440O''), ",
                "        (''8195'', ''63440P'') "
            };
        }

        [ARUMFeatureActionFilter("CostReport:Edit")]
        public JsonResult ImportHistorical(string templateYear, string crYear,
        string path, bool bypassFiles = false, bool bypassProcUpdate = false,
            string revisionYear = null, string[] documentNumbers = null, string startAt = null)
        {
            List<string> results = new List<string>();
            ImportHistoricalProgress = new ImportProgressViewModel();
            Session[SESSION_KEYS_MGMT.IMPORT_PROGRESS] = ImportHistoricalProgress;

            string documentType = string.Empty;
            string facilityType = string.Empty;

            SqlConnectionStringBuilder strBuilder = new SqlConnectionStringBuilder(Functions.DatabaseConnection);

            string targetDb = strBuilder["Database"].ToString();
            string sqlPath = OHFISession.ImportHistoricalSqlLocation;

            // Setup values based on original connection string information.
            if (strBuilder["Database"].ToString().ToUpper().Contains("ICF"))
            {
                documentType = "I";
                facilityType = "2";
            }
            else if (strBuilder["Database"].ToString().ToUpper().Contains("NF") ||
                strBuilder["Database"].ToString().ToUpper().Contains("OHFI"))
            {
                documentType = "N";
                facilityType = "1";
            }
            else
            {
                // If we reach here, then this is not a supported database for this process.
                return Json(new
                {
                    Success = true,
                    Message = "No Import Available."
                });
            }

            // Get the working DB and update connection string to match.
            string workingDb = OHFISession.ImportHistoricalWorkingDb;
            strBuilder["Database"] = workingDb;

            try
            {
                #region Get Files
                string[] files = new string[] { };
                if (!bypassFiles)
                {
                    // Get the file names
                    files = GetFileNames(path);
                    if (files.Length == 0)
                    {
                        _logger.Error("No files found at [{0}]", path);
                        throw new FileNotFoundException($"No files found at [{path}]");
                    }
                }
                #endregion

                // Reminder - this is the working DB.
                using (SqlConnection conn = new SqlConnection(strBuilder.ConnectionString))
                {
                    conn.Open();

                    if (!bypassFiles)
                    {
                        #region Load Tables from Files
                        //List<TableInfo> tables = new List<TableInfo>();
                        try
                        {
                            ImportHistoricalProgress.FileProgress = 0;

                            for (int i = 0; i < files.Length; i++)
                            {
                                string file = files[i];
                                // load the table from file and add the result table name to the list of tables to process.
                                //tables.Add(LoadTable(conn, file));
                                LoadTable(conn, file);
                                ImportHistoricalProgress.FileProgress = (int)((decimal)i / files.Length * 100);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, e.Message);
                            throw;
                        }
                        #endregion
                    }

                    if (!bypassProcUpdate)
                    {
                        #region Ensure update of required stored procedures
                        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "Sql", "ImportHistorical");
                        SqlCommand procCommand = new SqlCommand();
                        procCommand.Connection = conn;
                        procCommand.CommandType = CommandType.Text;

                        List<SqlFileInfo> sqlFiles = new List<SqlFileInfo>();

                        foreach (var file in Directory.GetFiles(baseDir).OrderBy(x => x))
                        {
                            SqlFileInfo sqlFileInfo = new SqlFileInfo
                            {
                                Name = file,
                                BaseName = Path.GetFileNameWithoutExtension(file)
                            };

                            // check if file is dated...
                            string f = sqlFileInfo.BaseName.Last(4);
                            if (f.IsNumeric())
                            {
                                if (int.TryParse(f, out int fYear))
                                {
                                    sqlFileInfo.Year = fYear;
                                    sqlFileInfo.BaseName = sqlFileInfo.BaseName.Substring(0, sqlFileInfo.BaseName.Length - 4);
                                }
                            }

                            sqlFiles.Add(sqlFileInfo);
                        }

                        List<SqlFileInfo> list = new List<SqlFileInfo>();
                        List<IGrouping<string, SqlFileInfo>> groups = sqlFiles.GroupBy(x => x.BaseName).ToList();

                        foreach (IGrouping<string, SqlFileInfo> group in groups)
                        {
                            if (group.Count() == 1)
                            {
                                list.Add(group.FirstOrDefault());
                                continue;
                            }

                            SqlFileInfo file = group.Where(x => x.Year <= int.Parse(templateYear))
                                .OrderByDescending(x => x.Year)
                                .FirstOrDefault();
                            if (file == null)
                            {
                                // If one at or before the template was found, then get the first one.
                                file = group
                                    .OrderBy(x => x.Year)
                                    .FirstOrDefault();
                            }

                            list.Add(file);
                        }

                        // ensure that any changes in setup tables are accommodated.
                        procCommand.CommandText = "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FieldTemplate]') AND type in (N'U')) DROP TABLE [dbo].[FieldTemplate]";
                        procCommand.ExecuteNonQuery();
                        procCommand.CommandText = "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentData]') AND type in (N'U')) DROP TABLE [dbo].[DocumentData]";
                        procCommand.ExecuteNonQuery();

                        foreach (SqlFileInfo sqlFileInfo in list)
                        { 
                            Console.WriteLine($"Executing ${sqlFileInfo.Name}");
                            procCommand.CommandText = System.IO.File.ReadAllText(sqlFileInfo.Name);
                            procCommand.ExecuteNonQuery();
                        }
                        #endregion
                    }

                    #region Prepare Processing Tables
                    try
                    {
                        RunSetup(conn, documentType, templateYear, targetDb);
                        ImportHistoricalProgress.ProcessSetup = true;
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, e.Message);
                        throw;
                    }
                    #endregion

                    // Now we need to work on the target database.
                    using (SqlConnection connTarget = new SqlConnection(Functions.DatabaseConnection))
                    {
                        connTarget.Open();

                        #region Get Provider Info
                        ARUMApplicationServiceProxy _arumApplicationClient = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);

                        IAllProviders providers = _arumApplicationClient.GetAllActiveProvidersWithOptions(RetrieveSession.Classification.ARUMToken,
                            false, true, true, false, false, false, false,
                            "Management/ImportHistorical");
                        #endregion

                        Dictionary<string, string> docNumbers = new Dictionary<string, string>();
                        if (documentNumbers == null) documentNumbers = new string[] { };
                        try
                        {
                            docNumbers = GetDocumentNumbers(conn, facilityType, crYear, revisionYear, results, documentNumbers.ToArray());
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, e.Message);
                            results.Add($"Could not get document numbers: {e.Message}");
                        }

                        int costReportYear = int.Parse(crYear);

                        #region Process the Tables for Document
                        try
                        {
                            ProcessTables(conn, documentType);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, e.Message);
                            throw;
                        }
                        #endregion

                        bool process = (startAt == null);
                        int length = docNumbers.Count;
                        ImportHistoricalProgress.DocumentTotal = length;
                        for (int i = 0; i < length; i++)
                        {
                            ImportHistoricalProgress.DocumentCount = i;
                            string docNumber = docNumbers.Keys.ElementAt(i);
                            string providerNumber = docNumbers.Values.ElementAt(i);
                            string providerHeader = $"{docNumber}; {providerNumber}: ";

                            if (!process)
                            {
                                if (startAt == docNumber) process = true;
                                else continue;
                            }

                            MSLC.ApplicationProxy.Interfaces.Models.Provider provider = providers.lstProviders.FirstOrDefault(x => x.MCAID == providerNumber && x.IsActive);

                            // If we didn't find this as an "active" provider, skip it.
                            if (provider == null)
                            {
                                results.Add($"{providerHeader}Provider not found as an available provider.");
                                continue;
                            }

                            // Determine if there is any document data for this provider.
                            List<ProviderDocumentInfo> info = GetProviderDocumentsForDocumentNumber(conn, docNumber, provider);

                            foreach (ProviderDocumentInfo item in info)
                            {
                                try
                                {
                                    item.StartYear = costReportYear;    // this will be the same for all of them.
                                    CopyProviderDocumentData(conn, connTarget, item, templateYear, provider);
                                    results.Add($"{providerHeader} SUCCESS");
                                }
                                catch (Exception e)
                                {
                                    _logger.Error(e, e.Message);
                                    results.Add($"{providerHeader}Could not copy document data: {e.Message}");
                                }
                            }
                        }

                        // Execute SQL script to correct UI values from the import.
                        string proc = string.Empty;
                        if (connTarget.Database.ToUpper().Contains("ICF"))
                        {
                            proc = $"UpdateUiValues{RetrieveSession.Classification.StateAbbreviation}ICF";
                        }
                        else if (connTarget.Database.ToUpper().Contains("NF") ||
                            connTarget.Database.ToUpper().Contains("OHFI"))
                        {
                            proc = $"UpdateUiValues{RetrieveSession.Classification.StateAbbreviation}NF";
                        }

                        using (SqlCommand cmd = connTarget.CreateCommand())
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = proc;
                            cmd.CommandTimeout = 1200;
                            cmd.ExecuteNonQuery();
                        }

                        List<string> tables = new List<string>
                        {
                            "[crdata].[CostProfile]",
                            "[crdata].[ProviderCostReport]",
                            "[crdata].[DocumentData]",
                            "[crdata].[Document]",
                        };

                        foreach (string table in tables)
                        {
                            using (SqlCommand cmd = connTarget.CreateCommand())
                            {
                                cmd.CommandType = CommandType.Text;
                                cmd.CommandText = $"ALTER INDEX ALL ON {table} REBUILD";
                                cmd.CommandTimeout = 1500;
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                ImportHistoricalProgress.Complete = true;

                ARUMHandler.ClearCache();
                return Json(new
                {
                    Success = true,
                    Data = results.ToArray()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                ImportHistoricalProgress.Failure = true;
                ARUMHandler.ClearCache();
                return Json(new
                {
                    Success = false,
                    Data = results.ToArray(),
                    Message = e.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private static Dictionary<string, string> GetDocumentNumbers(SqlConnection conn,
            string facilityType, string crYear, string revisionYear, List<string> results,
            string[] documentNumber = null)
        {
            Dictionary<string, string> docNumbers = new Dictionary<string, string>();

            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;

                // Determine what fiscal year of document data we need to address.
                // This is from the "RY" field of the DOCUMENT table.
                cmd.CommandText = @"SELECT DISTINCT d.DOC_NUM, p.SRC_PROV_NUM, t.FACIL_TYPE
                    FROM DOCUMENT d
                        LEFT JOIN PROV_XREF p ON p.PROV_ID = d.PROV_ID

                        LEFT JOIN CR_FACIL_TYPE t on t.DOC_NUM = d.DOC_NUM
                    WHERE FACIL_TYPE = " + facilityType + " AND YEAR(d.CR_TO_DATE) = " + crYear;

                if (!string.IsNullOrWhiteSpace(revisionYear)) cmd.CommandText += $" AND d.[RY] = '{revisionYear}'";

                // Either get the specific document, or get all non-obsoleted, submitted cost reports.
                if (documentNumber != null && documentNumber.Length > 0) cmd.CommandText += $" AND d.DOC_NUM in ('{string.Join("','", documentNumber)}')";

                cmd.CommandText += " ORDER BY d.DOC_NUM";

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    string doc = null;
                    string provNum = null;
                    while (rdr.Read())
                    {
                        doc = null;
                        provNum = null;
                        try
                        {
                            doc = ((IDataRecord)rdr)[0].ToString();
                            provNum = ((IDataRecord)rdr)[1].ToString();
                            if (!string.IsNullOrWhiteSpace(doc) && !string.IsNullOrWhiteSpace(provNum))
                            {
                                docNumbers.Add(doc, provNum);
                            }
                            else
                            {
                                results.Add($"Missing information for either Document Number [{doc}] or Provider Number [{provNum}].");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message, e);
                            results.Add($"Error processing Document Number [{doc}] or Provider Number [{provNum}]: {e.Message}");
                        }
                    }
                }
            }

            return docNumbers;
        }

        /// <summary>
        /// Pull all table and column info and the appropriate mapping information for the
        /// specified document template to match the historical source data files/tables.
        /// </summary>
        /// <param name="conn">SQL connection to the historical database.</param>
        /// <param name="facilityType">Type of facility; N = NF, I = ICF</param>
        /// <param name="templateYear">The document template year represented as a string</param>
        /// <returns></returns>
        private static List<DenRecord> GetDataElementNumberDataForTemplate(SqlConnection conn,
            string facilityType, string templateYear)
        {
            List<DenRecord> records = new List<DenRecord>();
            List<string> sql = new List<string>
            {
                // Determine what fiscal year of document data we need to address.
                // This is from the "RY" field of the DOCUMENT table.
                "SELECT DISTINCT",
                " den.[DATA_ELEM_NUM]",
                " , den.[SUBKEY_1_NAME]",
                " , TRIM(den.[SUBKEY_1]) AS [SUBKEY_1]",
                " , den.[SUBKEY_2_NAME]",
                " , TRIM(den.[SUBKEY_2]) AS [SUBKEY_2]",
                " , den.[LOW_SUFFIX]",
                " , den.[HIGH_SUFFIX]",
                " , den.[SUFFIX_NAME] as [SUFFIX_NAME1]",
                " , den.[SUFFIX_NAME_2] as [SUFFIX_NAME2]",
                " , den.[TABLE_NAME_EFF] AS [TABLE_NAME]",
                " , den.[COL_NAME]",
                " , den.[CR_COL]",
                " FROM [DATA_ELEMENT_NUMBER] den",
                " LEFT JOIN [FieldTemplate] ft on ft.[DEN] = den.[DATA_ELEM_NUM]",
                $" WHERE [RY_BEG_YEAR] <= '{templateYear}'",
                $" AND [RY_END_YEAR] >= '{templateYear}'",
                " AND [COL_NAME] IS NOT NULL",
                $" AND ([FACIL_TYPE] = 'A' OR [FACIL_TYPE] = '{facilityType}')",
                " AND ft.[RangeName] IS NOT NULL",
                " AND COL_LENGTH('dbo.' + den.[TABLE_NAME_EFF], den.[COL_NAME]) IS NOT NULL",
                " ORDER BY [TABLE_NAME_EFF], [COL_NAME]"
            };

            // Prep for the document template for the correct year
            SqlCommand cmd = conn.CreateCommand();
            string s = "setup";
            if (facilityType == "N") s += "NF";
            else s += "ICF";
            cmd.CommandTimeout = 600;
            cmd.CommandText = $"exec {s} {templateYear}";
            cmd.ExecuteNonQuery();

            DataTable table = Database.LoadDataTableSQLUsingSqlConnection(conn, string.Join(" ", sql.ToArray()));

            PropertyInfo[] props = new DenRecord().GetType().GetProperties();
            foreach (DataRow row in table.Rows)
            {
                DenRecord record = new DenRecord();
                foreach (PropertyInfo prop in props)
                {
                    prop.SetValue(record, Functions.GetStringDataFromDataRow(row, prop.Name));
                }
                records.Add(record);
            }

            return records;
        }

        /// <summary>
        ///  Load any document data from the working Db to the target Db for the specified provider.
        ///  The fiscal year will be pulled from the document data (if exists) and used to create
        ///  the cost report entry information so that a target actually exists for the document data.
        ///  If the document already exists, the data is cleared and repopualated with the new data.
        /// </summary>
        /// <param name="connWorking">Working (source) SQL connection object.</param>
        /// <param name="connTarget">Target SQL connection object.</param>
        /// <param name="provider">The provider information object.</param>
        private static List<ProviderDocumentInfo> GetProviderDocumentsForDocumentNumber(SqlConnection connWorking,
            string documentNumber, MSLC.ApplicationProxy.Interfaces.Models.Provider provider,
            SqlTransaction transWorking = null)
        {
            //bool externalTransWorking = transWorking != null;
            List<ProviderDocumentInfo> info = new List<ProviderDocumentInfo>();

            try
            {
                using (SqlCommand cmdWorking = connWorking.CreateCommand())
                {
                    cmdWorking.CommandType = CommandType.Text;

                    // Determine what fiscal year of document data we need to address.
                    // this will use the "CMS Certification Number" which should be the Medicare Id.
                    cmdWorking.CommandText = @"SELECT DISTINCT 
    [DOC_NUM],
    [10020],
    [10030],
    [10370],
	[10380],
	[10000],
    [RY]
FROM (
	SELECT [DOC_NUM], [RangeName], [DataValue]
	FROM [dbo].[DocumentData] 
	WHERE [RangeName] IN (
		'10020',
		'10030',
		'10370',
		'10380',
		'10000')
    ) p
	PIVOT 
	(
		MAX([DataValue])
		FOR [RangeName] IN (
            [10020], 
            [10030], 
            [10370], 
            [10380], 
            [10000]
        )
) as PivotTable
OUTER APPLY (
    SELECT [RY]
    FROM [DOCUMENT]
    WHERE [DOC_NUM] = PivotTable.[DOC_NUM]
) d
WHERE [DOC_NUM] = '" + documentNumber + "' AND [10020] = '" + provider.MCAID + "';";

                    //if (!externalTransWorking) transWorking = connWorking.BeginTransaction();

                    using (SqlDataReader rdr = cmdWorking.ExecuteReader())
                    {
                        // Loop through all the documents for this provider number
                        while (rdr.Read())
                        {
                            string docNumber = rdr.GetFieldValue<string>(0);    // document number
                            string provNumber = rdr.GetFieldValue<string>(1); // provider Medicaid number
                            string sDate = rdr.GetFieldValue<string>(3);        // cost report start date
                            string eDate = rdr.GetFieldValue<string>(4);        // cost report end date
                            //TODO: This may need to be based on CR dates to get the actual "type"
                            // Example: 4.1, 4.3, etc.
                            string crType = "4." + rdr.GetFieldValue<string>(5);       // cost report type
                            string rYear = rdr.GetFieldValue<string>(6);       // cost Report Year

                            if (DateTime.TryParse(sDate, out DateTime startDate) &&
                                DateTime.TryParse(eDate, out DateTime endDate) &&
                                int.TryParse(rYear, out int year))
                            {
                                // Handle partial dates.
                                DateTime subDate = endDate;
                                if (subDate.Month <= 3 && subDate.Day <= 15) subDate = new DateTime(subDate.Year, 3, 15);
                                else if (subDate.Month < 7) subDate = endDate;
                                else subDate = new DateTime(endDate.Year + 1, 3, 15);

                                info.Add(new ProviderDocumentInfo
                                {
                                    ProviderId = provider.ProviderID,
                                    DocumentNumber = docNumber,
                                    StartDate = startDate,
                                    EndDate = endDate,
                                    //Submit on 3/15 of the year after the CR period.
                                    SubmissionDate = subDate,
                                    CostReportType = crType.Substring(0, 3)
                                });
                            }
                        }
                    }

                    //if (!externalTransWorking) transWorking.Commit();
                }
            }
            catch (Exception e)
            {
                _staticLogger.Error(e, e.Message);
                throw;
            }

            return info;
        }

        private static void CopyProviderDocumentData(SqlConnection connSource, SqlConnection connTarget,
            ProviderDocumentInfo historyInfo, string templateYear,
            MSLC.ApplicationProxy.Interfaces.Models.Provider provider)
        {
            SqlTransaction tranTarget = connTarget.BeginTransaction();
            bool transOpen = true;
            List<string> listParams = new List<string>();

            try
            {
                int idProviderCostReport = 0;
                string periodCode = string.Empty;
                int chowPeriodId = 0;
                int documentTemplateId = 1;
                string status = "O";
                int staffId = 0;

                using (SqlCommand cmdSource = connSource.CreateCommand())
                using (SqlCommand cmdTarget = connTarget.CreateCommand())
                {
                    cmdTarget.Transaction = tranTarget;

                    #region Get or Create Provider Cost Report entry
                    cmdTarget.CommandType = CommandType.Text;
                    // Determine if there is an existing Provider Cost Report record.
                    cmdTarget.CommandText = $"SELECT ProviderCostReportId FROM [crdata].[ProviderCostReport] WHERE [Version] = '{historyInfo.DocumentNumber}'";
                    using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            // there is an existing record.
                            idProviderCostReport = rdr.GetFieldValue<int>(0);
                        }
                    }

                    if (idProviderCostReport == 0)
                    {
                        // Get the period code
                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT [PeriodCode] FROM [crdata].[Period] WHERE [PeriodCode] = 'CY{historyInfo.EndDate.Year}'";
                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                periodCode = rdr.GetString(0);
                            }
                        }

                        // If no existing record - create a new Period Code record
                        if (string.IsNullOrWhiteSpace(periodCode))
                        {
                            periodCode = $"CY{historyInfo.EndDate.Year}";
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.StoredProcedure;
                            cmdTarget.CommandText = "CreatePeriod";
                            cmdTarget.Parameters.Add(new SqlParameter("@PeriodYear", historyInfo.StartYear));
                            cmdTarget.ExecuteNonQuery();
                        }

                        // Get the CHOW Id
                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT [CHOWPeriodId] FROM [crdata].[CHOWPeriod] WHERE [ProviderId] = {provider.ProviderID} AND [PeriodCode] = '{periodCode}'";
                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                chowPeriodId = rdr.GetInt32(0);
                            }
                        }

                        // If no existing record - create a new CHOW Period record
                        if (chowPeriodId == 0)
                        {
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.Text;
                            cmdTarget.CommandText = $"INSERT INTO [crdata].[CHOWPeriod] ( [ProviderId], [PeriodCode]) SELECT {provider.ProviderID}, '{periodCode}'";
                            cmdTarget.ExecuteNonQuery();

                            // Now get the newly created record Id
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.Text;
                            cmdTarget.CommandText = $"SELECT [CHOWPeriodId] FROM [crdata].[CHOWPeriod] WHERE [ProviderId] = {provider.ProviderID} AND [PeriodCode] = '{periodCode}'";
                            using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    chowPeriodId = rdr.GetInt32(0);
                                }
                            }
                        }

                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT TOP 1 [DocumentTemplateId] FROM [crdata].[DocumentTemplate] dt LEFT JOIN [crdata].[DocumentType] ty ON ty.[DocumentTypeId] = dt.[DocumentTypeId] WHERE dt.[EffectiveDate] <= '{templateYear}-12-31' and ty.[Name] = 'Annual Cost Report' ORDER BY dt.[EffectiveDate] DESC";
                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                documentTemplateId = rdr.GetInt32(0);
                            }
                        }

                        if (documentTemplateId == 0)
                        {
                            // If we were unable to get a document template prior to period end date, get the first document template.
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.Text;
                            cmdTarget.CommandText = $"SELECT TOP 1 [DocumentTemplateId] FROM [crdata].[DocumentTemplate] dt LEFT JOIN [crdata].[DocumentType] ty ON ty.[DocumentTypeId] = dt.[DocumentTypeId] WHERE ty.[Name] = 'Annual Cost Report' ORDER BY dt.[EffectiveDate]";
                            using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    documentTemplateId = rdr.GetInt32(0);
                                }
                            }
                        }

                        // Get status of source document to update the provider cost report record with
                        cmdSource.Parameters.Clear();
                        cmdSource.CommandType = CommandType.Text;
                        cmdSource.CommandText = $"SELECT [CR_STATUS_CODE], [STAFF_ID] FROM [TRACKING] WHERE [DOC_NUM] = '{historyInfo.DocumentNumber}'";

                        using (SqlDataReader rdr = cmdSource.ExecuteReader())
                        {
                            // if valid record, we can get the status.
                            if (rdr.Read())
                            {
                                status = rdr.GetString(0);
                                string idValue = rdr.GetString(1);
                                if (!int.TryParse(idValue, out staffId)) staffId = 0;
                            }
                        }


                        // Check if the cost report type is valid...
                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT [CostReportTypeCode] FROM [crdata].[CostReportType] WHERE [CostReportTypeCode] = {historyInfo.CostReportType}";

                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            // if valid record, we can import.
                            if (!rdr.HasRows)
                            {
                                Console.Write($"Skipping cost report type {historyInfo.CostReportType}");
                                rdr.Close();
                                tranTarget.Rollback();
                                return;
                            }
                        }

                        listParams = new List<string>();
                        try
                        {

                            //Try to clean data...
                            string sCostReportType = historyInfo.CostReportType;
                            if (sCostReportType.Length > 3) sCostReportType.Substring(0, 2);

                            // Create the new Cost Report package
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.StoredProcedure;
                            cmdTarget.CommandText = "CreateCostReportNoTB";
                            cmdTarget.Parameters.Add(new SqlParameter("@ProviderId", provider.ProviderID));
                            listParams.Add($"@ProviderId, {provider.ProviderID}; ");
                            cmdTarget.Parameters.Add(new SqlParameter("@PeriodCode", periodCode));
                            listParams.Add($"@PeriodCode, {periodCode}; ");
                            cmdTarget.Parameters.Add(new SqlParameter("@TypeCode", historyInfo.CostReportType));
                            listParams.Add($"@TypeCode, {historyInfo.CostReportType}; ");
                            cmdTarget.Parameters.Add(new SqlParameter("@Version", historyInfo.DocumentNumber));
                            listParams.Add($"@Version, {historyInfo.DocumentNumber}; ");
                            cmdTarget.Parameters.Add(new SqlParameter("@ProviderName", provider.Name));
                            listParams.Add($"@ProviderName, {provider.Name}; ");
                            string sCCN = provider.Identifiers.FirstOrDefault(x => x.Type.ToUpper() == "MEDICAREID")?.Value ?? null;
                            cmdTarget.Parameters.Add(new SqlParameter("@CCN", sCCN));
                            listParams.Add($"@CCN, {sCCN}; ");
                            string sFedId = provider.Identifiers.FirstOrDefault(x => x.Type.ToUpper() == "FEDTAXID" || x.Type.ToUpper() == "FEDERAL TAX ID NUMBER")?.Value.FormatUSTaxId() ?? null;
                            cmdTarget.Parameters.Add(new SqlParameter("@FedId", sFedId));
                            listParams.Add($"@FedId, {sFedId}; ");
                            cmdTarget.Parameters.Add(new SqlParameter("@MedicaidId", provider.MCAID));
                            listParams.Add($"@MedicaidId, {provider.MCAID}; ");
                            string sNPI = provider.Identifiers.FirstOrDefault(x => x.Type.ToUpper() == "NPI")?.Value ?? null;
                            cmdTarget.Parameters.Add(new SqlParameter("@NPI", sNPI));
                            listParams.Add($"@NPI, {sNPI}; ");
                            string sODHId = provider.Identifiers.FirstOrDefault(x => x.Type.ToUpper() == "ODHID" || x.Type.ToUpper() == "ODH ID NUMBER")?.Value ?? null;
                            cmdTarget.Parameters.Add(new SqlParameter("@ODHId", sODHId));
                            listParams.Add($"@ODHId, {sODHId}; ");
                            cmdTarget.Parameters.Add(new SqlParameter("@DocumentTemplateId", documentTemplateId)); // Default to the primary document template.
                            listParams.Add($"@DocumentTemplateId, {documentTemplateId}; ");
                            switch (status)
                            {
                                case "C":
                                    cmdTarget.Parameters.Add(new SqlParameter("@StatusId", (int)CostReportStatusEnum.Cleared));
                                    listParams.Add($"@StatusId, {(int)CostReportStatusEnum.Cleared}; ");
                                    break;

                                case "O":
                                    cmdTarget.Parameters.Add(new SqlParameter("@StatusId", (int)CostReportStatusEnum.Obsolete));
                                    listParams.Add($"@StatusId, {(int)CostReportStatusEnum.Obsolete}; ");
                                    break;

                                case "H":
                                    cmdTarget.Parameters.Add(new SqlParameter("@StatusId", (int)CostReportStatusEnum.OnHold));
                                    listParams.Add($"@StatusId, {(int)CostReportStatusEnum.OnHold}; ");
                                    break;
                            }
                            cmdTarget.Parameters.Add(new SqlParameter("@PeriodBegin", historyInfo.StartDate)); // Default to the primary document template.
                            listParams.Add($"@PeriodBegin, {historyInfo.StartDate}; ");
                            cmdTarget.Parameters.Add(new SqlParameter("@PeriodEnd", historyInfo.EndDate)); // Default to the primary document template.
                            listParams.Add($"@PeriodEnd, {historyInfo.EndDate}; ");

                            MSLC.ApplicationProxy.Interfaces.Models.Addresses address = provider.Addresses
                                .Where(x => (x.Type == "FA" || x.Type == null || x.Type == "") && x.IsActive)
                                .OrderByDescending(x => x.CreateDate)
                                .ThenByDescending(x => x.LastModified)
                                .FirstOrDefault();
                            if (address != null)
                            {
                                cmdTarget.Parameters.Add(new SqlParameter("@Address1", address.Address1));
                                listParams.Add($"@Address1, {address.Address1}; ");
                                cmdTarget.Parameters.Add(new SqlParameter("@Address2", address.Address2));
                                listParams.Add($"@Address2, {address.Address2}; ");
                                cmdTarget.Parameters.Add(new SqlParameter("@City", address.City));
                                listParams.Add($"@City, {address.City}; ");
                                cmdTarget.Parameters.Add(new SqlParameter("@ZipCode", address.ZipCode));
                                listParams.Add($"@ZipCode, {address.ZipCode}; ");
                            }

                            // Execute the stored procedure to create the cost report entry.
                            using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    idProviderCostReport = rdr.GetFieldValue<int>(0);
                                }
                            }
                            cmdTarget.Parameters.Clear();
                        }
                        catch (Exception ex)
                        {
                            throw new DataException($"Unable to create required cost report entry for document [{historyInfo.DocumentNumber}]. {ex.Message}; {string.Join(";", listParams.ToArray())}", ex);
                        }
                        if (idProviderCostReport == 0)
                        {
                            // Unable to create a missing cost report entry... throw error.
                            throw new DataException($"Unable to create required cost report entry for document [{historyInfo.DocumentNumber}] ; {string.Join(";", listParams.ToArray())}");
                        }
                    }
                    else
                    {
                        // Get the "correct" period code
                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT [PeriodCode] FROM [crdata].[Period] WHERE [PeriodCode] = 'CY{historyInfo.EndDate.Year}'";
                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                periodCode = rdr.GetString(0);
                            }
                        }

                        // If no existing record - create a new Period Code record
                        if (string.IsNullOrWhiteSpace(periodCode))
                        {
                            periodCode = $"CY{historyInfo.EndDate.Year}";
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.StoredProcedure;
                            cmdTarget.CommandText = "CreatePeriod";
                            cmdTarget.Parameters.Add(new SqlParameter("@PeriodYear", historyInfo.StartYear));
                            cmdTarget.ExecuteNonQuery();
                        }

                        // Get the "correct" CHOW Id based on provider and period code above.
                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT [CHOWPeriodId] FROM [crdata].[CHOWPeriod] WHERE [ProviderId] = {provider.ProviderID} AND [PeriodCode] = '{periodCode}'";
                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                chowPeriodId = rdr.GetInt32(0);
                            }
                        }

                        // If no existing record - create a new CHOW Period record
                        if (chowPeriodId == 0)
                        {
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.Text;
                            cmdTarget.CommandText = $"INSERT INTO [crdata].[CHOWPeriod] ( [ProviderId], [PeriodCode]) SELECT {provider.ProviderID}, '{periodCode}'";
                            cmdTarget.ExecuteNonQuery();

                            // Now get the newly created record Id
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.Text;
                            cmdTarget.CommandText = $"SELECT [CHOWPeriodId] FROM [crdata].[CHOWPeriod] WHERE [ProviderId] = {provider.ProviderID} AND [PeriodCode] = '{periodCode}'";
                            using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    chowPeriodId = rdr.GetInt32(0);
                                }
                            }
                        }

                        // Verify that the CHOW period Id matches for the cost report
                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT [CHOWPeriodId] FROM [crdata].[ProviderCostReport] WHERE [ProviderCostReportId] = {idProviderCostReport}";
                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                if (chowPeriodId != rdr.GetInt32(0))
                                {
                                    // close the reader as we will attempt to execute off the same command object.
                                    rdr.Close();

                                    cmdTarget.Parameters.Clear();
                                    cmdTarget.CommandType = CommandType.Text;
                                    cmdTarget.CommandText = $"UPDATE [crdata].[ProviderCostReport] SET [CHOWPeriodId] = {chowPeriodId} WHERE [ProviderCostReportId] = {idProviderCostReport}";
                                    cmdTarget.ExecuteNonQuery();
                                }
                            }
                        }

                        // Get the "correct" document template Id.
                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT TOP 1 [DocumentTemplateId] FROM [crdata].[DocumentTemplate] dt LEFT JOIN [crdata].[DocumentType] ty ON ty.[DocumentTypeId] = dt.[DocumentTypeId] WHERE dt.[EffectiveDate] <= '{templateYear}-12-31' and ty.[Name] = 'Annual Cost Report' ORDER BY dt.[EffectiveDate] DESC";
                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                documentTemplateId = rdr.GetInt32(0);
                            }
                        }

                        if (documentTemplateId == 0)
                        {
                            // If we were unable to get a document template prior to period end date, get the first document template.
                            cmdTarget.Parameters.Clear();
                            cmdTarget.CommandType = CommandType.Text;
                            cmdTarget.CommandText = $"SELECT TOP 1 [DocumentTemplateId] FROM [crdata].[DocumentTemplate] dt LEFT JOIN [crdata].[DocumentType] ty ON ty.[DocumentTypeId] = dt.[DocumentTypeId] WHERE ty.[Name] = 'Annual Cost Report' ORDER BY dt.[EffectiveDate]";
                            using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    documentTemplateId = rdr.GetInt32(0);
                                }
                            }
                        }

                        // Get the current document template Id from the cost report document.
                        cmdTarget.Parameters.Clear();
                        cmdTarget.CommandType = CommandType.Text;
                        cmdTarget.CommandText = $"SELECT [DocumentTemplateId] FROM [crdata].[ProviderCostReport] cr LEFT JOIN [crdata].[Document] d ON d.[DocumentId] = cr.[MedicaidDocumentId] WHERE cr.[ProviderCostReportId] = {idProviderCostReport}";
                        using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                if (documentTemplateId != rdr.GetInt32(0))
                                {
                                    // close the reader as we will attempt to execute off the same command object.
                                    rdr.Close();

                                    // The document templates don't match - update the document template Id
                                    cmdTarget.Parameters.Clear();
                                    cmdTarget.CommandType = CommandType.Text;
                                    cmdTarget.CommandText = $"UPDATE [crdata].[Document] SET [DocumentTemplateId] = {documentTemplateId} WHERE [DocumentId] = (SELECT [MedicaidDocumentId] FROM [crdata].[ProvidercostReport] WHERE [ProviderCostReportId] = {idProviderCostReport})";
                                    cmdTarget.ExecuteNonQuery();
                                }
                            }
                        }

                        // Get status of source document to update the provider cost report record with
                        cmdSource.Parameters.Clear();
                        cmdSource.CommandType = CommandType.Text;
                        cmdSource.CommandText = $"SELECT [CR_STATUS_CODE], [STAFF_ID] FROM [TRACKING] WHERE [DOC_NUM] = '{historyInfo.DocumentNumber}'";

                        using (SqlDataReader rdr = cmdSource.ExecuteReader())
                        {
                            // if valid record, we can get the status.
                            if (rdr.Read())
                            {
                                status = rdr.GetString(0);
                                if (int.TryParse(rdr.GetString(1), out int i)) staffId = i;
                            }
                        }
                    }
                    #endregion

                    #region Copy the Medicaid Document information
                    // Initiate the stored proc to clear and copy the data
                    cmdTarget.CommandType = CommandType.StoredProcedure;
                    cmdTarget.CommandText = "CopyHistoricalData";

                    listParams = new List<string>();

                    cmdTarget.Parameters.Clear();
                    cmdTarget.Parameters.Add(new SqlParameter("@ProviderCostReportId", idProviderCostReport));
                    listParams.Add($"@ProviderCostReportId,{idProviderCostReport}");
                    cmdTarget.Parameters.Add(new SqlParameter("@DocumentNumber", historyInfo.DocumentNumber));
                    listParams.Add($"@DocumentNumber,{historyInfo.DocumentNumber}");

                    using (SqlDataReader rdr = cmdTarget.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            // Verify we completed successfully
                            if (rdr.FieldCount == 1)
                            {
                                // Successful!
                            }
                            else if (rdr.FieldCount == 3)
                            {
                                // We got an error message! :(
                                throw new Exception($"Unable to copy cost report data to target DB; {rdr.GetFieldValue<int>(0)}, {rdr.GetFieldValue<int>(1)}, {rdr.GetFieldValue<string>(2)} - {string.Join(";", listParams.ToArray())}");
                            }
                        }
                    }

                    // Ensure there are no cost report edits records for this historical cost report.
                    cmdTarget.Parameters.Clear();
                    cmdTarget.CommandType = CommandType.Text;
                    cmdTarget.CommandText = $"DELETE FROM [crdata].[CostReportEdits] WHERE [ProviderCostReportId] = {idProviderCostReport}";
                    cmdTarget.ExecuteNonQuery();

                    //Commit the record copy from history database.
                    tranTarget.Commit();
                    transOpen = false;
                    #endregion Copy the Medicaid Document information

                    #region Lock and Submit the cost report
                    //"Lock" the cost report.
                    CostReportHistoryBL.LockCostReport(idProviderCostReport, historyInfo.SubmissionDate.ToString("yyyy-MM-dd"));

                    //"Submit" the cost report.
                    string appVersion = typeof(MvcApplication).Assembly.GetName().Version.ToString();
                    CostReportHistoryBL.SubmitCostReport(idProviderCostReport, appVersion, historyInfo.DocumentNumber, out string s,
                        historyInfo.SubmissionDate.ToString("yyyy-MM-dd"), false, true);

                    int examinerId = CostReportBL.GetUserIdByLegacyId(connTarget, staffId);
                    if (examinerId == 0) throw new Exception($"Legacy Staff Id {staffId} was not found to assign examiner.");
                    CostReportBL.SaveCostReportExaminer(connTarget, idProviderCostReport, examinerId);
                    #endregion Lock and Submit the cost report

                    #region Run Calculations
                    // Note, since this will have NO trial balance,
                    // it should skip all of that in the calculation.
                    //ScheduleBL.CalculateAllSchedules(idProviderCostReport);
                    #endregion Run Calculations
                }
            }
            catch (Exception e)
            {
                _staticLogger.Error(e, e.Message);
                if (transOpen) tranTarget.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Run the Setup SQL files using the values specified.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="docType"></param>
        /// <param name="facilityType"></param>
        /// <param name="workingDb"></param>
        /// <param name="targetDb"></param>
        private static void RunSetup(SqlConnection conn, string docType, string year,
            string targetDb, SqlTransaction transaction = null)
        {
            bool externalTransaction = (transaction != null);
            transaction = transaction ?? conn.BeginTransaction();

            try
            {
                string proc = string.Empty;
                int iYear = 0;
                if (int.TryParse(year, out int temp))
                {
                    iYear = temp;
                }
                else
                {
                    iYear = 2020;
                }

                switch (docType)
                {
                    case "N":
                        proc = "SetupNF";
                        break;

                    case "I":
                        proc = "SetupICF";
                        break;

                    default:
                        throw new ArgumentException($"Invalid document type [{docType}]");
                }

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Transaction = transaction;
                    cmd.CommandText = proc;
                    cmd.CommandTimeout = 3600;  // one hour timeout

                    cmd.Parameters.Add(new SqlParameter("@Year", iYear));

                    cmd.ExecuteNonQuery();
                }

                if (!externalTransaction) transaction.Commit();
            }
            catch (Exception e)
            {
                _staticLogger.Error(e, e.Message);
                if (!externalTransaction) transaction.Rollback();
                throw;
            }
        }

        private static void ProcessTables(SqlConnection conn, string docType,
            SqlTransaction transaction = null)
        {
            bool externalTransaction = (transaction != null);
            transaction = transaction ?? conn.BeginTransaction();
            string proc = string.Empty;

            try
            {
                // Clear out previous data in the document data table. This will speed up the process.
                // This also makes this single document transaction atomic.
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DELETE FROM DOCUMENTDATA;";
                    cmd.ExecuteNonQuery();
                }

                // Reset the primary key, just in case.
                switch (docType)
                {
                    case "I":
                        proc = "HistImportICF";
                        break;

                    case "N":
                        proc = "HistImportNF";
                        break;

                    default:
                        throw new NotSupportedException($"Unable to process unknown document type [{docType}]");
                }

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = proc;
                    //cmd.Parameters.Add(new SqlParameter("@DocumentNumber", documentNumber));
                    cmd.CommandTimeout = 3600;  // one hour timeout

                    cmd.ExecuteNonQuery();
                }

                if (!externalTransaction) transaction.Commit();
            }
            catch (Exception e)
            {
                _staticLogger.Error(e, e.Message);
                if (!externalTransaction) transaction.Rollback();
                throw;
            }
        }

        private static string[] GetFileNames(string inputPath)
        {
            // get the path from the input file path
            string path = Path.GetDirectoryName(inputPath);
            // if no path was specified (just a name or search criteria, assume current directory.
            if (string.IsNullOrWhiteSpace(path)) path = Directory.GetCurrentDirectory();
            // if specified, then ensure we have a qualified path from our current directory.
            else path = Path.Combine(Directory.GetCurrentDirectory(), path);

            // get the files that match the specified input name
            return Directory.GetFiles(path, Path.GetFileName(inputPath));
        }

        /// <summary>
        /// Create a new table named after the specified file and load it with the data
        /// from the file. 
        /// If the table already exists, it will drop it first.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="filepath"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        static TableInfo LoadTable(SqlConnection conn, string filepath, SqlTransaction transaction = null)
        {
            bool externalTransaction = (transaction != null);
            transaction = transaction ?? conn.BeginTransaction();
            TableInfo table = new TableInfo()
            {
                Schema = "dbo"
            };

            using (SqlCommand cmd = new SqlCommand())
            {
                try
                {
                    table.TableName = Database.GetTableNameFromFilepath(filepath);

                    //Open existing Excel file
                    using (ExcelEngine excelEngine = new ExcelEngine())
                    using (MemoryStream ExcelMemStream = new MemoryStream())
                    using (FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        file.CopyTo(ExcelMemStream);

                        Syncfusion.XlsIO.IApplication app = excelEngine.Excel;
                        app.DefaultVersion = ExcelVersion.Xlsx;

                        ExcelMemStream.Position = 0;
                        IWorkbook workbook = app.Workbooks.Open(ExcelMemStream, ExcelOpenType.Automatic, ExcelParseOptions.Default);

                        List<string[]> accumulatedRows = new List<string[]>();

                        for (int i = 0; i < workbook.Worksheets.Count; i++)
                        {
                            IWorksheet sheet = workbook.Worksheets[i];

                            if (i == 0)
                            {
                                // The fields should be the row values from the row 1.
                                table.Fields = GetRowValuesFromSheet(sheet);

                                // Create the table from the table name and fields.
                                // Drop it if it already exists.
                                Database.CreateTable(conn, table.TableName, table.Fields, table.Schema, transaction);
                            }

                            int counter = 2;
                            string[] values = GetRowValuesFromSheet(sheet, counter, table.Fields.Length);

                            cmd.Connection = conn;
                            cmd.Transaction = transaction;
                            cmd.CommandType = System.Data.CommandType.Text;

                            // while we have value characters, attempt to loop and insert the data
                            while (!string.IsNullOrWhiteSpace(string.Join(" ", values)))
                            {
                                // Accumulate rows of data to insert...
                                accumulatedRows.Add(values);

                                // Every 25 lines, do a SQL statement to save the parsed data.
                                // This should be faster than doing it every line.
                                if (accumulatedRows.Count > 25)
                                {
                                    cmd.CommandText = Database.InsertMassRecordsSqlString(table.TableName, table.Fields, accumulatedRows);
                                    try
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.ToString());
                                    }

                                    // clear out the accumulated rows
                                    accumulatedRows.Clear();
                                }

                                counter++;
                                values = GetRowValuesFromSheet(sheet, counter, table.Fields.Length);
                            }

                            if (accumulatedRows.Count > 0)
                            {
                                cmd.CommandText = Database.InsertMassRecordsSqlString(table.TableName, table.Fields, accumulatedRows);
                                try
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.ToString());
                                }
                            }
                        }
                    }

                    if (!externalTransaction) transaction.Commit();
                }
                catch (Exception e)
                {
                    _staticLogger.Error(e, e.Message);
                    if (!externalTransaction) transaction.Rollback();
                    throw;
                }
            }

            return table;
        }

        /// <summary>
        /// Get the values from the specified row of the worksheet.
        /// Once a blank cell is found, it will stop searching through the row.
        /// </summary>
        /// <param name="worksheet"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string[] GetRowValuesFromSheet(IWorksheet worksheet, int row = 1, int limit = -1)
        {
            List<string> fields = new List<string>();

            // Get columns from Row specified.
            string col = "A";
            string cell = worksheet[$"{col}{row}"].Value;
            int counter = 1;

            // loop only until we either exceed our limit or we reach a cell with no data.
            while (limit > 0 ? counter <= limit : !string.IsNullOrWhiteSpace(cell))
            {
                // add the current column cell value.
                fields.Add(cell);
                counter++;
                // if we exceed our specified number of columns - stop.
                if (limit > 0 && counter > limit) break;
                col = GetNextExcelColumn(col);
                //int t = counter;
                //char? c = null;
                //while (t > 26)
                //{
                //    t -= 26;
                //    // If we need to do multi-alphanumeric c, set to before 'A'
                //    c = c ?? (char)64;
                //    // Increment the c
                //    c = (char)(((int)c) + 1);
                //    if ((int)c > 90) throw new Exception("Too many columns in table XLSX.");
                //}
                //string colName = (c.HasValue ? c.ToString() : "") + (char)(t + 64);

                IRange target = worksheet[$"{col}{row}"];

                if (target == null) cell = "";
                else cell = target.Value;
            }

            return fields.ToArray();
        }

        private static string GetNextExcelColumn(string excelColumn)
        {
            int? pos1 = null;
            int? pos2 = null;

            switch (excelColumn.Length)
            {
                case 1:
                    pos2 = excelColumn[0];
                    break;

                case 2:
                    pos1 = excelColumn[0];
                    pos2 = excelColumn[1];
                    break;

                default:
                    throw new ArgumentException($"Invalid Excel column passed: [{excelColumn}]");
            }

            // Remember: "A" = 65... "Z" = 90.
            if (pos1.HasValue && (pos1 < 65 || pos1 > 90)) throw new ArgumentException($"Invalid Excel column passed: [{excelColumn}]");
            if (pos2.HasValue && (pos2 < 65 || pos2 > 90)) throw new ArgumentException($"Invalid Excel column passed: [{excelColumn}]");

            pos2++;

            if (pos2 > 90)
            {
                pos2 = 65;
                if (pos1.HasValue) pos1++;
                else pos1 = 65;
            }

            if (pos1 > 90) throw new ArgumentException($"Column exceeds limit: [{excelColumn}]");

            if (pos1.HasValue)
            {
                return ((char)pos1).ToString() + ((char)pos2).ToString();
            }
            else
            {
                return ((char)pos2).ToString();
            }
        }

        /// <summary>
        /// Get the cost report extract template for the specified document template effective date
        /// </summary>
        /// <param name="templateEffectiveDate">Effective date of the document template to find the matching extract template</param>
        /// <returns>File path to the cost report extract template</returns>
        /// <exception cref="ArgumentException"></exception>
        private static string GetCostReportTemplate(DateTime templateEffectiveDate, bool isAudit) //TODO: Implement audit report templates into this piece
        {
            string templateDir = Path.GetDirectoryName(isAudit ? OHFISession.AuditReportTemplateLocation : OHFISession.CostReportExtractTemplateLocation);
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templateDir);
            string[] files = Directory.GetFiles(templatePath).Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();
            int year = templateEffectiveDate.Year;
            Dictionary<int, string> templates = new Dictionary<int, string>();
            string fileName = templateDir;

            foreach (string file in files)
            {
                string yearPart = file.Substring(file.Length - 8, 4);
                if (!int.TryParse(yearPart, out int result))
                {
                    // Log that there is an invalid template file name and continue.
                    _staticLogger.Warning("Invalid template file name {0}", 
                        file,
                        Assembly.GetExecutingAssembly().GetName(),
                        MethodBase.GetCurrentMethod(),
                        "GetCostReportDataById");
                    continue;
                }
                if (result == year)
                {
                    fileName = Path.Combine(fileName, $"{file}.xlsx");
                    break;
                }
                else
                    templates[result] = file;
            }

            if (fileName == templateDir)
            {
                // Get the key at or before the current year.
                int key = templates.Keys?.OrderByDescending(x => x)?.FirstOrDefault(x => x <= year) ?? -1;
                // If key not found, get the first key.
                if (key == -1) key = templates.Keys?.OrderBy(x => x)?.FirstOrDefault() ?? -1;
                // If there were no keys, throw error.
                if (key == -1) throw new ArgumentException("Unable to determine correct template");
                // Get template file path for the key determined.
                if (!templates.TryGetValue(key, out string file)) throw new ArgumentException("Unable to get selected template");
                fileName = Path.Combine(fileName, $"{file}.xlsx");
            }
            return fileName;
        }

        public JsonResult GenerateAuditAdjustmentReport(string providerData)
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int i))
            {
                providerCostReportId = i;
            }

            int providerId = 0;
            int.TryParse(selection.ProviderId, out providerId);
            string providerNumber = ReportBL.GetProvidersList().FirstOrDefault(x => x.ProviderId == providerId)?.ProviderNumber ?? string.Empty;
            string fileName = $"AuditReport_{providerNumber}_CY{selection.PeriodCode.Substring(2)}.xlsx";
            string pdfName = fileName.Replace(".xlsx", ".pdf");
            
            string costReportExtractTemplateLocation = GetCostReportTemplate(CostReportBL.GetCostReportTemplateEffectiveDate(providerCostReportId), true);
            var myUser = RetrieveSession.ARUMUser;
            try
            {
                ManagementBL.GenerateAuditCostReportExtractExcel(providerCostReportId, providerNumber, selection, fileName, costReportExtractTemplateLocation);
                // BL layer uses different version of XlsIO so we have to recall engine here and not just pass ref of engine to the BL 
                // BL Layer does not have access to XlsIORenderer so we need to use it here
                using(ExcelEngine excelEngine = new ExcelEngine())
                {
                    Syncfusion.XlsIO.IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Xlsx;

                    using (FileStream excelStream = new FileStream(Path.Combine(Path.GetTempPath(), fileName), FileMode.Open, FileAccess.Read))
                    {
                        IWorkbook workbook = application.Workbooks.Open(excelStream);
                        XlsIORendererSettings settings = new XlsIORendererSettings();
                        XlsIORenderer renderer = new XlsIORenderer();
                        using (PdfDocument pdfDocument = renderer.ConvertToPDF(workbook, settings))
                        {
                            using (FileStream pdfStream = new FileStream(Path.Combine(Path.GetTempPath(), pdfName), FileMode.Create, FileAccess.Write))
                            {
                                pdfDocument.Save(pdfStream);
                            }
                        }
                    }
                }

                #region Statlog Message
                string eventName = string.Empty;
                switch (selection.CostReportTypeId)
                {
                    case "4.90":
                        eventName = "Initial Audit Report Printed";
                        break;
                    case "4.91":
                        eventName = "Revised Audit Report Printed";
                        break;
                    case "4.92":
                        eventName = "Final Audit Report Printed";
                        break;
                }
                int crid;
                if (!int.TryParse(selection.ProviderCostReportId, out crid))
                {
                    crid = 0;
                }
                if (!string.IsNullOrEmpty(eventName))
                {
                    RateBL.SaveCostReportStatlog(new ProviderCostReportStatlog
                    {
                        CRID = crid,
                        EventDate = DateTime.Now,
                        Stamp = DateTime.Now,
                        EventName = eventName,
                        UserId = myUser.Email,
                        CreatedOn = DateTime.UtcNow,
                        CreatedBy = $"{myUser.FirstName} {myUser.LastName}",
                        LastUpdatedOn = DateTime.UtcNow,
                        LastUpdateBy = $"{myUser.FirstName} {myUser.LastName}",
                    });
                }
                #endregion

                return Json(new { Success = true, fileName = HttpUtility.UrlEncode(pdfName) });
            }

            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);

                return Json(new
                {
                    Success = false,
                    Message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GenerateAuditAdjustmentHistoryReport()
        {
            try
            {
                IWorkbook workbook;
                IWorksheet worksheet;

                string date = DateTime.Now.ToString("yyyyMMdd");
                string projType = RetrieveSession.Classification?.Type.Trim().ToUpper();
                string fileName = $"{projType}_AuditAdjustmentHistoryReport_Run_{date}.xlsx";
                string fullPath = string.Empty;

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
                int providerId = 0;
                int providerCostReportId = 0;


                if (!int.TryParse(selection.ProviderId, out providerId))
                    throw new Exception("No cost report selected");

                if (!int.TryParse(selection.ProviderCostReportId, out providerCostReportId))
                    throw new Exception("No cost report selected");

                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    #region Data Gathering

                    MSLC.ProviderProxy.Interfaces.IArumDetailedProvider provider = _providerServiceProxy.GetProviderById(RetrieveSession.Classification.ARUMToken, providerId, "Rate/Provider");

                    DataTable table = CostReportAuditBL.GetAuditAdjustmentHistoryData(providerCostReportId);
                    List<AuditAdjustmentHistoryReportModel> rowData = ExcelStyleHelper.GetReportModelsFromDataTable<AuditAdjustmentHistoryReportModel>(table);

                    string npi = provider.Identifiers.FirstOrDefault(x => x.Type == "NPI")?.Value ?? string.Empty;
                    string providerName = provider.Provider.Name;
                    foreach (AuditAdjustmentHistoryReportModel data in rowData)
                    {
                        if (data.Schedule == "C-1")
                            data.PageNumber = GetPageNumber(data.RangeName).ToString();

                        data.NPI = npi;
                        data.ProviderName = providerName;
                    }

                    #endregion

                    #region Writing Report to File

                    Syncfusion.XlsIO.IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Xlsx;
                    workbook = application.Workbooks.Create(1);
                    worksheet = workbook.Worksheets[0];

                    int row = 1;
                    int col = 1;

                    //--- Creating Headers
                    Dictionary<string, ExcelStylingAttribute> headers = ExcelStyleHelper.GetReportHeaders<AuditAdjustmentHistoryReportModel>();
                    IRange titleHeader = worksheet.Range[row, col, row, headers.Count];
                    titleHeader.Merge();
                    titleHeader.Text = "Audit Adjustment History Report";

                    row += 2;

                    foreach (KeyValuePair<string, ExcelStylingAttribute> header in headers)
                    {
                        worksheet.Range[row, col].Text = header.Key;
                        col++;
                    }

                    row++;

                    //--- Printing Out Records
                    List<PropertyInfo> reportProps = ExcelStyleHelper.GetReportProperties<AuditAdjustmentHistoryReportModel>();
                    foreach (AuditAdjustmentHistoryReportModel rowItem in rowData)
                    {
                        col = 1;
                        foreach (PropertyInfo prop in reportProps)
                        {
                            AddValueToCell(rowItem, prop, worksheet, row, col);
                            col++;
                        }
                        row++;
                    }

                    col = 0;

                    //--- Format Document
                    var titleStyleName = CreateStyle(workbook, "TitleStyle", true, 11, false, "Times New Roman",
                        ExcelVAlign.VAlignBottom, ExcelHAlign.HAlignCenter);
                    var headerStyleName = CreateStyle(workbook, "TitleStyle", true, 11, false, "Times New Roman",
                        ExcelVAlign.VAlignBottom, ExcelHAlign.HAlignLeft);
                    var bodyStyleName = CreateStyle(workbook, "BodyStyle", false, 10, false, "Times New Roman",
                        ExcelVAlign.VAlignBottom, ExcelHAlign.HAlignRight);

                    worksheet.UsedRange.CellStyleName = bodyStyleName;
                    worksheet.Rows[0].CellStyleName = titleStyleName;
                    worksheet.Rows[2].CellStyleName = headerStyleName;

                    ApplyCellFormatting(headers, worksheet, col);

                    worksheet.UsedRange.AutofitRows();
                    worksheet.PageSetup.FitToPagesTall = 1;
                    worksheet.PageSetup.FitToPagesWide = 1;
                    worksheet.PageSetup.Orientation = ExcelPageOrientation.Landscape;

                    #endregion

                    #region File Saving

                    fullPath = Path.Combine(Path.GetTempPath(), fileName);

                    MemoryStream outputStream = new MemoryStream();
                    workbook.SaveAs(outputStream);
                    outputStream.Position = 0;

                    workbook.Close();

                    MemoryStream report = outputStream;

                    using (FileStream fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        report.WriteTo(fileStream);
                    }

                    #endregion
                }
                return Json(new { Success = true, fileName = HttpUtility.UrlEncode(fileName) });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new
                {
                    Success = false,
                    FileName = string.Empty,
                    Message = e.Message
                },
                    JsonRequestBehavior.AllowGet);
            }
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

        private void AddValueToCell<T>(T rowItem, PropertyInfo prop, IWorksheet worksheet, int row, int col)
        {
            object value = prop.GetValue(rowItem);
            if (value is double d)
                worksheet.Range[row, col].Number = d;
            else if (value is int i)
                worksheet.Range[row, col].Number = i;
            if (value != null && value is decimal dl)
                worksheet.Range[row, col].Number = (double)dl;
            else if (value is DateTime t)
                worksheet.Range[row, col].Text = t.ToString("MM/dd/yyyy");
            else if (!(value is null))
                worksheet.Range[row, col].Text = value.ToString();
        }

        private void ApplyCellFormatting(Dictionary<string, ExcelStylingAttribute> headers, IWorksheet worksheet, int col, bool outLineCells = false)
        {
            foreach (var header in headers)
            {
                worksheet.SetColumnWidth((col + 1), header.Value.ColumnWidth);
                if (header.Value.NumberFormat != null)
                    worksheet.Columns[col].NumberFormat = header.Value.NumberFormat;
                if (header.Value.AlignWithCenter)
                    worksheet.Columns[col].HorizontalAlignment = ExcelHAlign.HAlignCenter;
                col++;
            }
            if (outLineCells)
            {
                foreach (IRange cell in worksheet.UsedCells)
                {
                    cell.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    cell.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    cell.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    cell.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                }
            }
        }

        private string CreateStyle(IWorkbook workbook, string styleName, bool bold, int size, bool wrapText,
            string fontNane, ExcelVAlign vAlign, ExcelHAlign hAlign, bool underline = false, IWorksheet worksheet = null)
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

        private sealed class TableInfo
        {
            public string TableName { get; set; }
            public string Schema { get; set; }
            public string[] Fields { get; set; }
        }

        private sealed class ProviderDocumentInfo
        {
            public int ProviderId { get; set; }
            public string ProviderName { get; set; }
            public string DocumentNumber { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime SubmissionDate { get; set; }
            public string CostReportType { get; set; }
            public int StartYear { get; set; }
        }

        private sealed class SqlFileInfo
        {
            public string Name { get; set; }
            public string BaseName { get; set; }
            public int? Year { get; set; }
        }

        private sealed class DenRecord
        {
            public string DATA_ELEM_NUM { get; set; }
            public string RY_END_YEAR { get; set; }
            public string SUBKEY_1_NAME { get; set; }
            public string SUBKEY_1 { get; set; }
            public string SUBKEY_2_NAME { get; set; }
            public string SUBKEY_2 { get; set; }
            public string LOW_SUFFIX { get; set; }
            public string HIGH_SUFFIX { get; set; }
            public string SUFFIX_NAME1 { get; set; }
            public string SUFFIX_NAME2 { get; set; }
            public string TABLE_NAME { get; set; }
            public string COL_NAME { get; set; }
            public string CR_COL { get; set; }
            public string TABLE_EXISTS { get; set; }
            public string COL_EXISTS { get; set; }
        }
    }
}