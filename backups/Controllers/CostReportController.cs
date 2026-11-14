using Microsoft.Ajax.Utilities;
using MSLC.ApplicationProxy;
using MSLC.ArumProxy;
using MSLC.ArumProxy.Models;
using MSLC.FileProxy;
using MSLC.FileProxy.Interfaces;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.ProviderProxy;
using MSLC.StatLog.BL.Event;
using MSLC.StatLog.BL.EventLifeCycle;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.BL.StatLogBL;
using MSLC.StatLog.Controllers;
using MSLC.StatLog.Domain;
using MSLC.StatLog.DTO;
using MSLC.StatLog.MVC.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using WebPortal_OH_FI.BL;
using WebPortal_OH_FI.BL.CostReport;
using WebPortal_OH_FI.BL.CostReportEdits;
using WebPortal_OH_FI.BL.JsonObjects.CostReport;
using WebPortal_OH_FI.BL.MappedObjects;
using WebPortal_OH_FI.Common.Extensions;
using WebPortal_OH_FI.Models;
using WebPortal_OH_FI.Models.CostReport;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.crdata;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;
using WebPortal_OH_FI.ProjectSpecific.Data.Seed;
using WebPortal_OH_FI.Web;
using StatLogCostReportHistory = MSLC.StatLog.BL.CostReportHistory;


namespace WebPortal_OH_FI.Controllers
{
    public static class SESSION_KEYS_OCRT
    {
        public const string SELECTED_PROVIDER_ID = "selectedProviderId";
        public const string PROVIDER_DATA = "ProviderData";
        public const string END_DATE = "EndDate";
        public const string SCHEDULE_WORKSHEETS = "ScheduleWorksheets";
        public const string SELECTED_COST_REPORT = "Selected Cost Report";
        public const string SELECTION_EXPAND_COLLAPSE = "Selection Expand/Collapse";
        public const string SCHED_EXPAND_COLLAPSE = "Schedule Expand/Collapse";
        public const string USE_LOCAL_CALCULATIONS = "UseLocalCalculations";
        public const string COST_REPORT_START_DATES_4_1 = "CostReportStartDates_4_1";
        public const string COST_REPORT_TYPE_LIST = "CostReportTypeList";
        public const string SELECTED_TAB = "Selected Tab";
        public const string SELECTED_SCHEDULE = "Selected Schedule";
        public const string SCHED_LAST = "ScheduleLastVisited";
        public const string GRID_DEFINITION = "GridDefinitionCache";
        public const string SCHEDULE_VALUES_UPDATED = "ScheduleValuesUpdated";
        public const string SCHEDULE_EDITS_REQUIRED = "ScheduleEditsRequired";
        public const string SCHED_EDITS_CURRENT = "CurrentScheduleEdits";
        public const string CHART_OF_ACCTS_DROP_DOWN = "ChartOfAccountsDropDown";
        public const string SCHEDULE_GRID_MASK = "ScheduleGridMask";
        public const string SCHED_PAGE_COUNTER = "SchedulePageCounter";
        public const string SCHED_PAGE_COUNT_FIRST = "SchedulePageCountFirst";
        public const string SCHED_PAGE_COUNT_LAST = "SchedulePageCountLast";
    }

    public class CostReportController : LoggedInBaseController
    {
        private readonly ArumServiceProxy _arumUserProxy;
        private readonly FileServiceProxy _fileServiceProxy;
        private readonly ARUMProviderProxy _providerServiceProxy;
        private readonly ARUMProxy_User _user;
        private readonly ClassificationDTO _classification;
        private readonly ARUMApplicationServiceProxy _applicationServiceProxy;
        private List<Tab> Tabs { get; set; }

        private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<ManagementController>();
        private static string SimplifiedType =>
           Common.Extensions.ClassificationDTOExtensions.GetSimplifiedType(RetrieveSession.Classification);

        public CostReportController()
        {
            _classification = RetrieveSession.Classification;
            _arumUserProxy = ARUMUser.GetArumServiceProxy(_classification);
            _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(_classification);
            _providerServiceProxy = ARUMProvider.GetARUMProviderServiceProxy(_classification);
            _applicationServiceProxy = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);
            _user = RetrieveSession.ARUMUser;
        }

        private List<Tab> LoadTabs()
        {
            // if this is the first time calling during this session call, create the list.
            if (Tabs == null) Tabs = Session[SESSION_KEYS_OCRT.SCHEDULE_WORKSHEETS] as List<Tab>;
            // if the list has items, it has already been populated this session call; use it.
            if (Tabs != null && Tabs.Count > 0) return Tabs;

            Tabs = Session[SESSION_KEYS_OCRT.SCHEDULE_WORKSHEETS] as List<Tab>;

            try
            {
                int providerCostReportId = 0;

                if (Session != null)
                {
                    SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                    int.TryParse(selection.ProviderCostReportId, out providerCostReportId);
                }

                if (providerCostReportId != 0)
                {
                    if (Tabs == null || Tabs.Count == 0)
                    {
                        Tabs = ScheduleBL.GetScheduleList(providerCostReportId, null);
                        Session[SESSION_KEYS_OCRT.SCHEDULE_WORKSHEETS] = Tabs;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }

            return Tabs;
        }

        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult Index()
        {
            if (_user.Providers == null || !_user.Providers.Any())
            {
                TempData["Message"] = "No Providers Assigned to Current User.  Contact Support.  Click the icon below.";
                return RedirectToAction("Login", "Home");
            }

            string primaryIdentifier = ProviderBL.GetPrimaryIdentifierDisplayName(_classification);
            int.TryParse(TempData["ARUMProviderID"]?.ToString(), out int arumProviderID);
            int.TryParse(TempData["CostReportID"]?.ToString(), out int costReportID);

            var providerList1 = ProviderBL.GetAllARUMProviderInfoByARUMUserID(_classification, _user, "CostReport:View");
            var providerList = providerList1.Select(p => new StatLogViewModel.ProviderDropDown
            {
                Name = p.Name,
                Number = p.Number,
                ARUMProviderID = ClientMapping.MapInternalIdToClientId(p.ARUMProviderID.ToString(), "provider")
            }).ToList();

            object value = Session[SESSION_KEYS_OCRT.SELECTION_EXPAND_COLLAPSE];

            var myViewModel = new Index
            {
                AutoProviderInformation = providerList,
                PrimaryIdentifier = !string.IsNullOrEmpty(primaryIdentifier) ? primaryIdentifier : "MCAID",
                IsCollapsed = value != null && value.ToString() == "collapsed",
                InRuleEngineURL = string.Empty,
                UseLocalCalculations = (bool)(Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] ?? true)
            };

            // calculate the InRule JavaScript engine name for the client to download
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
            if (!string.IsNullOrWhiteSpace(selection?.PeriodCode ?? string.Empty))
            {
                myViewModel.InRuleEngineURL = ARUMConfigurationOHFI.InRuleEngineFileName(RetrieveSession.Classification, selection.PeriodCode);
            }

            // get the cost report start dates for type 4.1 from ARUM and cache in the session.
            Dictionary<string, DateTime> costReportStartDates_4_1 = new Dictionary<string, DateTime>();
            string s = ARUMConfigurationOHFI.CostReportStartDates_4_1(RetrieveSession.Classification);
            string[] items = s.Split(';');
            foreach (string item in items)
            {
                string[] i = item.Split('-');
                costReportStartDates_4_1.Add(i[0], DateTime.Parse(i[1]));
            }
            Session[SESSION_KEYS_OCRT.COST_REPORT_START_DATES_4_1] = costReportStartDates_4_1;

            return View(myViewModel);
        }

        #region Selection Lists
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        private ActionResult SelectionValid(SelectionModel selection)
        {
            MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
            int providerId = 0;

            if (selection == null)
            {
                SelectionModel value = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
                if (value != null)
                {
                    selection.ProviderCostReportId = ClientMapping.MapInternalIdToClientId(value.ProviderCostReportId, "costreport");
                    selection.ProviderId = ClientMapping.MapInternalIdToClientId(value.ProviderId, "provider");
                    selection.CostReportTypeId = value.CostReportTypeId;
                    selection.CostReportType = value.CostReportType;
                    selection.VersionName = value.VersionName;
                    selection.PeriodCode = value.PeriodCode;
                }

                if (!int.TryParse(value.ProviderId, out providerId)) providerId = 0;
            }

            if (selection == null || string.IsNullOrWhiteSpace(selection.ProviderId) || string.IsNullOrWhiteSpace(selection.PeriodCode))
            {
                return Json(new { Success = true, JsonRequestBehavior = JsonRequestBehavior.AllowGet });
            }

            if (!CostReportHistoryBL.GetPeriodList(providerId).Any(p => p.PeriodCode == selection.PeriodCode))
            {
                _logger.Error("An invalid reporting period was indicated: {0}", selection.PeriodCode);
                return Json(new { Success = false, Message = @"Invalid request.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
            }

            if (!myUser.Providers.Any(p => p.ProviderID == providerId))
            {
                _logger.Error("An invalid provider was indicated: {0}", providerId);
                return Json(new { Success = false, Message = @"Invalid request.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
            }

            return null;
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        public JsonResult GetCurrentProviderRole()
        {
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = new { success = true, role = RetrieveSession.CurrentRoleName }
            };
        }

        // ============================================================================================================
        // Cost Report Type Index
        // ------------------------------------------------------------------------------------------------------------
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult GetCostReportTypeIndex(SelectionModel selection)
        {
            try
            {
                // MUST have a provider selected.
                int providerId = 0;

                if (int.TryParse(ClientMapping.GetInternalIdFromClientId(selection.ProviderId, "provider"), out int i))
                {
                    providerId = i;
                }

                if (providerId == 0)
                {
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }

                // MUST have a cost report period selected.
                if (string.IsNullOrWhiteSpace(selection.PeriodCode))
                {
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }

                Dictionary<string, List<CostReportTypeListItem>> typeLists = (Dictionary<string, List<CostReportTypeListItem>>)Session[SESSION_KEYS_OCRT.COST_REPORT_TYPE_LIST];
                if (!typeLists.TryGetValue(selection.PeriodCode, out List<CostReportTypeListItem> index))
                {
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { Success = true, Index = index }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        // ============================================================================================================
        // Document Index
        // ------------------------------------------------------------------------------------------------------------
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult GetDocumentIndex(SelectionModel selection)
        {
            try
            {
                int? providerId = null;
                if (int.TryParse(ClientMapping.GetInternalIdFromClientId(selection.ProviderId, "provider"), out int i))
                {
                    providerId = i;
                }
                RetrieveSession.SelectedProviderID = providerId.Value;

                // MUST have a cost report period selected.
                if (string.IsNullOrWhiteSpace(selection.PeriodCode))
                {
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }

                // MUST have a cost report type selected
                if (selection.CostReportTypeId == null)
                {
                    return Json(new { Success = true, JsonRequestBehavior = JsonRequestBehavior.AllowGet });
                }

                List<CostReportTypeListItem> typeIndex = CostReportHistoryBL.GetTypeList(providerId, selection.PeriodCode);
                CostReportTypeListItem type = typeIndex.FirstOrDefault(t => t.TypeCode == selection.CostReportTypeId);
                if (type == null)
                {
                    _logger.Error("An invalid cost report type was indicated: {0}", selection.CostReportType);
                    return Json(new { Success = false, Message = @"Invalid request.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
                }

                List<CostReportListItem> index = CostReportHistoryBL.GetList(providerId, selection.PeriodCode, selection.CostReportTypeId);

                List<CostReportListItem> list = new List<CostReportListItem>();

                // NOTE: there will ALWAYS be a "0" id to account for the "[Create New]" item!
                foreach (var item in index)
                {
                    if (item.ProviderCostReportId == "0")
                    {
                        // If the user does not have "Edit" access, then there should not be a "[Create New]" option!
                        // We will skip adding the "[Create New]" if no access.
                        if (!CostReportPermsissionsBL.CheckPermissionCostReportCreate(item.CostReportTypeCode))
                        {
                            // If permission is not granted, skip to the next iteration
                            continue;
                        }
                    }

                    list.Add(new CostReportListItem
                    {
                        ProviderCostReportId = ClientMapping.MapInternalIdToClientId(item.ProviderCostReportId, "costreport"),
                        PeriodCode = item.PeriodCode,
                        PeriodBeginDate = item.PeriodBeginDate,
                        PeriodEndDate = item.PeriodEndDate,
                        ProviderId = ClientMapping.MapInternalIdToClientId(item.ProviderId, "provider"),
                        MedicaidDocumentId = ClientMapping.MapInternalIdToClientId(item.MedicaidDocumentId, "mdoc"),
                        TrialBalanceDocumentId = ClientMapping.MapInternalIdToClientId(item.TrialBalanceDocumentId, "tbdoc"),
                        CRBeginDate = item.CRBeginDate,
                        CREndDate = item.CREndDate,
                        DueDate = item.DueDate,
                        DueDateExtended = item.DueDateExtended,
                        Version = item.Version,
                        SubmitDate = item.SubmitDate,
                        CostReportTypeCode = item.CostReportTypeCode,
                        Status = item.Status
                    });
                }

                return Json(new { Success = true, Index = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        // ============================================================================================================
        // Facility Index
        // ------------------------------------------------------------------------------------------------------------
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetFacilityIndex(SelectionModel selection)
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;

                return Json(
                    new
                    {
                        Success = true,
                        Index = myUser.Providers
                            .Select(x => new
                            {
                                ProviderId = ClientMapping.MapInternalIdToClientId(x.ProviderID.ToString(), "provider"),
                                Name = x.ProviderName,
                                //CCN = x.ProviderIdentifiers.FirstOrDefault(i => i.Type == "CCN"),
                                //FedId = x.ProviderIdentifiers.FirstOrDefault(i => i.Type == "FEDID"),
                                NPI = x.ProviderIdentifiers.FirstOrDefault(i => i.Type == "NPI"),
                                //ODHId = x.ProviderIdentifiers.FirstOrDefault(i => i.Type == "ODHID")
                            })
                            .Select(x => new { ProviderId = x.ProviderId, Name = $"{x.Name} {(x.NPI != null ? $"(NPI: {x.NPI.Value})" : "")}" })
                            .OrderBy(p => p.Name)
                            .ToList()
                    },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        // ============================================================================================================
        // Reporting Period Index
        // ------------------------------------------------------------------------------------------------------------
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetReportingPeriodIndex(SelectionModel selection)
        {
            try
            {
                string p = ClientMapping.GetInternalIdFromClientId(selection.ProviderId, "provider");
                if (string.IsNullOrWhiteSpace(p))
                {
                    return Json(new { Success = true, Index = new List<string>().ToArray() });
                }

                int providerId = 0;
                if (!int.TryParse(p, out providerId))
                {
                    return Json(new { Success = false, Message = "Invalid or unknown provider selected. Please check your selection." });
                }

                MSLC.ProviderProxy.Interfaces.IArumDetailedProvider arum_provider = _providerServiceProxy.GetProviderInfoByARUMProviderID(RetrieveSession.Classification.ARUMToken, providerId, "CostReport/Provider");
                if (arum_provider == null)
                {
                    return Json(new { Success = false, Message = "Unable to retrieve provider information; please check your provider access." });
                }

                DateTime? initalCertDate = null;
                DateTime? closureDate = null;
                IEnumerable<MSLC.ProviderProxy.Interfaces.Models.ProviderEffectiveDate> dates = arum_provider.EffectiveDates?
                            .OrderByDescending(x => x.EndDate == null ? DateTime.MaxValue : x.EndDate);
                MSLC.ProviderProxy.Interfaces.Models.ProviderEffectiveDate effDate = null;
                switch (SimplifiedType)
                {
                    case "NF":
                        // put the check for type first before the null check to prioritize records with the type.
                        effDate = dates?.FirstOrDefault(x => x.IsActive && (string.IsNullOrWhiteSpace(x.Type) || x.Type.StartsWith("NF")));
                        initalCertDate = effDate?.EffectiveDate;
                        closureDate = effDate?.EndDate;
                        break;

                    case "ICF":
                        // put the check for type first before the null check to prioritize records with the type.
                        effDate = dates?.FirstOrDefault(x => x.IsActive && (string.IsNullOrWhiteSpace(x.Type) || x.Type.StartsWith("ICF")));
                        initalCertDate = effDate?.EffectiveDate;
                        closureDate = effDate?.EndDate;
                        break;

                    default:
                        return Json(new { Success = false, Message = "Unknown provider type for cost reports" });
                }
                if (!initalCertDate.HasValue)
                {
                    return Json(new { Success = false, Message = "Unable to determine provider initial cert date; please review provider information." });
                }

                // Store in session to reuse under cost report type index
                Session[SESSION_KEYS_OCRT.END_DATE] = closureDate.Value;// null is valid

                List<CostReportPeriodListItem> list = CostReportHistoryBL.GetPeriodList(providerId, initalCertDate, closureDate);

                bool isActiveChop = false;
                bool isActiveInititalEntry = false;

                MSLC.ProviderProxy.Interfaces.IArumProviderStatusList arum_Status =
                        _providerServiceProxy.GetAllProviderStatusesByProviderID(RetrieveSession.Classification.ARUMToken, providerId, "Rate/Provider");
                if (arum_Status.ProviderStatuses?.Any() ?? false)
                {
                    // Get Medicaid statuses for the provider init cert date.
                    List<MSLC.ProviderProxy.Interfaces.Models.ProviderStatus> effStatuses = arum_Status.ProviderStatuses.Where(x => x.EffectiveDate == initalCertDate.Value && x.Type.ToUpper() == "MEDICAID").ToList();

                    // Determine if any are applicable for the flags we seek.
                    isActiveChop = effStatuses.Any(x => x.Status == "CHA");
                    isActiveInititalEntry = effStatuses.Any(x => x.Status == "IEN");
                }

                Dictionary<string, List<CostReportTypeListItem>> typeList = new Dictionary<string, List<CostReportTypeListItem>>();

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    CostReportPeriodListItem item = list[i];

                    DateTime startDate_4_1 = DateTime.MaxValue;
                    Dictionary<string, DateTime> costReportStartDates_4_1 = (Dictionary<string, DateTime>)Session[SESSION_KEYS_OCRT.COST_REPORT_START_DATES_4_1];
                    if (costReportStartDates_4_1.ContainsKey(item.PeriodCode))
                    {
                        startDate_4_1 = costReportStartDates_4_1[item.PeriodCode];
                        // DateTime.TryParse(ss, out startDate_4_1);
                    }
                    List<CostReportTypeListItem> index = CostReportHistoryBL.GetCostReportTypeList(providerId, initalCertDate.Value, closureDate.Value, startDate_4_1, item.PeriodCode, isActiveChop, isActiveInititalEntry);

                    if (index.Count() == 0) list.Remove(item);
                    else
                    {
                        // Create a separate list to store items to be removed
                        var itemsToRemove = new List<CostReportTypeListItem>();

                        foreach (var auditItem in index)
                        {
                            if (auditItem.TypeCode.StartsWith("4.9") && !CostReportPermsissionsBL.CheckPermissionCostReportView(auditItem.TypeCode))
                            {
                                // If the user does not have "Audit View" access, the "4.90, 4.91, 4.92" audit cost report types should not appear in the dropdown. 
                                // These audit cost report types will be skipped if the user lacks access.
                                itemsToRemove.Add(auditItem);
                            }
                        }

                        // Remove the collected items from the index
                        foreach (var itemToRemove in itemsToRemove)
                        {
                            index.Remove(itemToRemove);
                        }

                        if (index.Count() == 0) list.Remove(item);
                        else typeList.Add(item.PeriodCode, index);
                    }
                }

                Session[SESSION_KEYS_OCRT.COST_REPORT_TYPE_LIST] = typeList;

                return Json(
                    new
                    {
                        Success = true,
                        Index = list.Select(cr => new
                        {
                            cr.PeriodCode,
                            BeginDate = !cr.BeginDate.HasValue ? string.Empty : cr.BeginDate.Value.ToString("yyyyMMdd"),
                            EndDate = !cr.EndDate.HasValue ? string.Empty : cr.EndDate.Value.ToString("yyyyMMdd"),
                        })
                            .OrderByDescending(x => x.EndDate)
                            .ToList()
                    },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Selection
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult SelectedDocument()
        {
            try
            {
                SelectionModel viewModel = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerId = 0;
                if (int.TryParse(viewModel.ProviderId, out int i))
                {
                    providerId = i;
                }

                RetrieveSession.SelectedProviderID = providerId;

                // If there is no selected document, clear out schedule dependencies
                if (providerId == 0) Session[SESSION_KEYS_OCRT.SCHEDULE_WORKSHEETS] = null;

                return Json(new SelectionModel
                {
                    ProviderId = ClientMapping.MapInternalIdToClientId(viewModel.ProviderId, "provider"),
                    ProviderCostReportId = ClientMapping.MapInternalIdToClientId(viewModel.ProviderCostReportId, "costreport"),
                    CostReportTypeId = viewModel.CostReportTypeId,
                    PeriodCode = viewModel.PeriodCode,
                    CostReportType = viewModel.CostReportType,
                    FacilityText = viewModel.FacilityText,
                    VersionName = viewModel.VersionName,
                    CanEditCostReport = viewModel.CanEditCostReport,
                    InRuleEngineName = viewModel.InRuleEngineName ?? ARUMConfigurationOHFI.InRuleEngineFileName(RetrieveSession.Classification, viewModel.PeriodCode),
                    InRuleEngineTbName = viewModel.InRuleEngineTbName ?? ARUMConfigurationOHFI.InRuleEngineTbFileName(RetrieveSession.Classification, viewModel.PeriodCode)
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        [HttpGet]
        public ActionResult SelectDocument()
        {
            return PartialView();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selection">This is a JSON object created on the client and submitted.</param>
        /// <returns></returns>
        [HttpPost]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult SelectDocument(SelectionModel selection)
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
                SelectionModel model = new SelectionModel();

                if (string.IsNullOrWhiteSpace(selection.ProviderId))
                {
                    return Json(new { Success = false });
                    //return Json(new { Success = false, Message = @"You must select a facility.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
                }

                // get the internal value for the client ID
                int? providerId = null;
                if (int.TryParse(ClientMapping.GetInternalIdFromClientId(selection.ProviderId, "provider"), out int i))
                {
                    providerId = i;
                }
                if (providerId.HasValue)
                {
                    model.ProviderId = providerId.Value.ToString();

                    // The "ProviderId" is the ARUM provider ID
                    RetrieveSession.SelectedProviderID = providerId.Value;
                }

                // get the internal value for the client ID
                string providerCostReportString = ClientMapping.GetInternalIdFromClientId(selection.ProviderCostReportId, "costreport");
                if (!int.TryParse(providerCostReportString, out int providerCostReportId))
                {
                    _logger.Error("An invalid cost report Id was indicated: {0}", providerCostReportString);
                    return Json(new { Success = false, Message = @"Invalid request." });
                }
                model.ProviderCostReportId = providerCostReportString;

                // can't do this until the provider is selected.
                // Audit Setting
                model.CanCreateCostReport = CostReportPermsissionsBL.CheckPermissionCostReportCreate(selection.CostReportTypeId);
                model.CanEditCostReport = CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId) && (CostReportHistoryBL.CheckSubmissionStatus(providerCostReportId) == CostReportStatusEnum.Open && !CostReportHistoryBL.CheckLockedStatus(providerCostReportId));

                if (string.IsNullOrEmpty(selection.PeriodCode))
                {
                    return Json(new { Success = false, Message = @"You must select a reporting period.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
                }
                // get the internal value for the client ID
                model.PeriodCode = selection.PeriodCode;

                if (string.IsNullOrEmpty(selection.CostReportTypeId))
                {
                    return Json(new { Success = false, Message = @"You must select a cost report type.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
                }
                // get the internal value for the client ID
                model.CostReportTypeId = selection.CostReportTypeId;

                if (string.IsNullOrEmpty(selection.ProviderCostReportId))
                {
                    return Json(new { Success = false, Message = @"You must select a version.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
                }

                CostReportPeriodListItem period = CostReportHistoryBL.GetPeriodList(providerId).FirstOrDefault(p => p.PeriodCode == selection.PeriodCode);
                if (period == null)
                {
                    _logger.Error("An invalid period was indicated for provider: {0}", providerId.Value);
                    return Json(new { Success = false, Message = @"Invalid request." });
                }

                //Audit cost report types
                if (model.CostReportTypeId.StartsWith("4.9"))
                {
                    Dictionary<string, List<CostReportTypeListItem>> typeLists = (Dictionary<string, List<CostReportTypeListItem>>)Session[SESSION_KEYS_OCRT.COST_REPORT_TYPE_LIST];
                    if (typeLists.TryGetValue(selection.PeriodCode, out List<CostReportTypeListItem> index))
                    {
                        if (model.CostReportTypeId == "4.90")
                        {
                            //Audit cost report type "4.90"
                            var initialAuditedRateCreate = index.Where(x => x.Status == "Submitted" && x.Cleared && (x.TypeCode == "4.1" || x.TypeCode == "4.6")).FirstOrDefault();
                            if (initialAuditedRateCreate == null)
                            {
                                _logger.Error("There is no 4.1 or 4.6 Cost Report for this CY, a 4.90 Cost Report cannot be created: {0}", model.CostReportTypeId);
                                return Json(new { Success = false, Message = @"There is no 4.1 or 4.6 Cost Report for this CY, a 4.90 Cost Report cannot be created." });
                            }
                        }
                        else if (model.CostReportTypeId == "4.91")
                        {
                            var initialAudit = index.Where(x => x.Status == "Submitted" && x.Cleared && x.TypeCode == "4.90").FirstOrDefault();
                            if (initialAudit == null)
                            {
                                _logger.Error("4.90 cost report must be uploaded and submitted {0}", model.CostReportTypeId);
                                return Json(new { Success = false, Message = @"4.90 cost report must be uploaded and submitted." });
                            }
                        }
                        else if (model.CostReportTypeId == "4.92")
                        {
                            var initialAudit = index.Where(x => x.Status == "Submitted" && x.Cleared && (x.TypeCode == "4.90" || x.TypeCode == "4.91")).FirstOrDefault();
                            if (initialAudit == null)
                            {
                                _logger.Error("4.90 or 4.91 cost report must be uploaded and submitted {0}", model.CostReportTypeId);
                                return Json(new { Success = false, Message = @"4.90 or 4.91 cost report must be uploaded and submitted." });
                            }
                        }
                    }
                }

                MSLC.ProviderProxy.Interfaces.IArumDetailedProvider provider = _providerServiceProxy.GetProviderById(RetrieveSession.Classification.ARUMToken, providerId.Value, "Rate/Provider");
                if (provider == null)
                {
                    _logger.Error("An invalid provider was indicated: {0}", providerId.Value);
                    return Json(new { Success = false, Message = @"Invalid request." });
                }

                var npi = provider.Identifiers.FirstOrDefault(x => x.Type == "NPI");
                model.FacilityText = $"{provider.Provider.Name} {(npi != null ? $"(NPI: {npi.Value})" : "")}";

                MSLC.ProviderProxy.Interfaces.IArumDetailedProvider providerDetails = _providerServiceProxy.GetProviderInfoByARUMProviderID(RetrieveSession.Classification.ARUMToken, provider.Provider.ProviderID, "CostReport/SelectDocument");
                if (providerDetails == null)
                {
                    _logger.Error("Unable to get ARUM provider details for {0}", provider.Provider.ProviderID);
                    return Json(new { Success = false, Message = @"Invalid request." });
                }
                RateSettingPackage.ProviderPackage providerPackage = RateProviderBL.GetProviderPackage(providerDetails);

                MSLC.ProviderProxy.Interfaces.IArumChowReviewChain arum_CHOP = _providerServiceProxy.GetChowReviewChainByNewProviderID(RetrieveSession.Classification.ARUMToken, providerPackage.ProviderId, "CostReport/Provider");
                // Ensure, if there is CHOP data, that this is after the cost report end date - which would make this the "first" time the provider is rated, even if it is chopped later in the year.
                MSLC.ProviderProxy.Interfaces.Models.ChowReview anyChopsBeforePeriodEnd = arum_CHOP?.ChowReviews?.FirstOrDefault(x => x.NewProviderID == providerPackage.ProviderId);
                if (anyChopsBeforePeriodEnd != null) providerPackage.ChopOriginated = true;
                Session[SESSION_KEYS_OCRT.PROVIDER_DATA] = providerPackage;

                List<CostReportTypeListItem> typeIndex = CostReportHistoryBL.GetTypeList(providerId, model.PeriodCode);
                CostReportTypeListItem type = typeIndex.FirstOrDefault(t => t.TypeCode == selection.CostReportTypeId);
                if (type == null)
                {
                    _logger.Error("An invalid cost report type was indicated: {0}", selection.CostReportTypeId);
                    return Json(new { Success = false, Message = @"Invalid request." });
                }
                model.CostReportType = type.TypeCode;

                List<CostReportListItem> docIndex = CostReportHistoryBL.GetList(providerId, selection.PeriodCode, selection.CostReportTypeId);
                CostReportListItem doc = docIndex.FirstOrDefault(d => d.ProviderCostReportId == providerCostReportString);
                if (doc == null)
                {
                    _logger.Error("An invalid cost report was indicated; provider: {0}, period: {1}, type: {2}", providerId.Value, selection.PeriodCode, selection.CostReportTypeId);
                    return Json(new { Success = false, Message = @"Invalid request." });
                }
                model.VersionName = doc.Version;

                //Get period code based on cost report document template 
                string periodCode = ScheduleBL.GetCostReportPeriodCode(providerCostReportId, period.PeriodCode);
                // calculate the InRule JavaScript engine name for the client to download
                model.InRuleEngineName = ARUMConfigurationOHFI.InRuleEngineFileName(RetrieveSession.Classification, periodCode);
                model.InRuleEngineTbName = ARUMConfigurationOHFI.InRuleEngineTbFileName(RetrieveSession.Classification, periodCode);

                if (model.ProviderCostReportId == "0")
                {
                    if (!CostReportPermsissionsBL.CheckPermissionCostReportCreate(selection.CostReportTypeId))
                    {
                        return Json(new { Success = false, Message = "Cost report indicated does not exist; insufficient rights to create report.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
                    }

                    CostReportModel cr = new CostReportModel
                    {
                        ProviderId = providerId,
                        PeriodId = model.PeriodCode,
                        CostReportTypeId = model.CostReportTypeId,
                        VersionName = model.VersionName,
                        ProviderName = provider.Provider.Name,
                        CanEditCostReport = model.CanEditCostReport,
                        CanCreateCostReport = model.CanCreateCostReport
                    };

                    MSLC.ProviderProxy.Interfaces.Models.Address address = providerDetails.Addresses
                        .Where(x => (x.Type == "FA" || x.Type == null || x.Type == "") && x.IsActive)
                        .OrderByDescending(x => x.CreateDate)
                        .ThenByDescending(x => x.LastModified)
                        .FirstOrDefault();
                    if (address != null)
                    {
                        cr.Address1 = address.Address1;
                        cr.Address2 = address.Address2;
                        cr.City = address.City;
                        cr.ZipCode = address.ZipCode;
                    }

                    MSLC.ProviderProxy.Interfaces.Models.ProviderEffectiveDate provInfo = providerDetails.EffectiveDates
                        .Where(x => x.EffectiveDate <= period.EndDate)
                        .OrderByDescending(x => x.EffectiveDate)
                        .ThenByDescending(x => x.CreateDate)
                        .FirstOrDefault();

                    if (provInfo != null)
                    {
                        cr.County = provInfo.County;
                    }

                    foreach (MSLC.ProviderProxy.Interfaces.Models.ProviderIdentifier identifier in providerDetails.Identifiers)
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

                    if (!string.IsNullOrEmpty(provider.Provider.MCAID))
                    {
                        cr.MedicaidId = provider.Provider.MCAID;
                    }

                    cr.ODHId = CostReportBL.GetODHFromAffiliate(_providerServiceProxy, providerDetails);

                    cr.VersionName = selection.VersionName;
                    cr = CostReportHistoryBL.CreateCostReport(cr);
                    model.ProviderCostReportId = cr.ProviderCostReportId;

                    if (int.TryParse(cr.ProviderCostReportId, out i)) providerCostReportId = i;

                    model.VersionName = cr.VersionName;

                    //Create StatLog for appliable CR Types
                    #region OCRT- StatLog Connection

                    try
                    {
                        //If OCRT-StatLog is not enabled, skip
                        string check = ARUMConfigurationOHFI.EnableOCRTStatLogConnection(RetrieveSession.Classification);
                        if (check.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //Check setting to see if CR Type is allowed
                            //Check setting to see if CR Type is allowed
                            string CRTypeCheck = ARUMConfigurationOHFI.CostReportCreationStatLogTypes(RetrieveSession.Classification);
                            List<string> CRTypeList = CRTypeCheck.Split(';').ToList();

                            if (CRTypeList.Contains(type.TypeCode))
                            {
                                //Create/Update StatLog with CostReport info
                                var _classification = RetrieveSession.Classification;

                                //Check that provider Effective Date is valid
                                if (provInfo.EffectiveDate == null)
                                {
                                    throw new Exception($"Provider {provider.Provider.MCAID} does not have a start date in their effective date record.");
                                }

                                if (period.BeginDate == null || period.EndDate == null)
                                {
                                    throw new Exception($"Cost Report Period {period.PeriodCode} for provider {provider.Provider.MCAID} does not have a begin or end date.");
                                }


                                var periodBeginDate = period.BeginDate.Value;
                                var periodEndDate = period.EndDate.Value;

                                if (provInfo.EffectiveDate.Value > periodBeginDate && provInfo.EffectiveDate.Value.Year == periodBeginDate.Year)
                                    periodBeginDate = provInfo.EffectiveDate.Value;

                                //Search for Statlog based on CostReport info
                                int statLogID = -1;

                                var providerStatLogs = StatLogBL.GetStatLogsByARUMProvider(_classification, provider.Provider.ProviderID);
                                var preexistingStatLog = providerStatLogs.FirstOrDefault(sl =>
                                    sl.AuditType.Trim().ToUpper() == type.TypeCode.Trim().ToUpper()
                                    && sl.EndDate.Year == periodEndDate.Year
                                    && (sl.IsActive == true || sl.IsActive == null));


                                var statLogProvider = ProviderBL.GetProviderInfoByARUMUserAndProviderID(_classification, RetrieveSession.ARUMUser, provider.Provider.ProviderID);
                                var PortalURL = new UrlHelper(((MvcHandler)System.Web.HttpContext.Current.Handler).RequestContext).AbsoluteAction("Login", "Home");
                                var processLifeCycle = new ProcessLifeCycle(_classification, PortalURL, _arumUserProxy, _fileServiceProxy, _providerServiceProxy);

                                //Create Statlog if it doesn't exist
                                if (preexistingStatLog == null)
                                {
                                    statLogID = StatLogBL.InsertStatLog(_classification,
                                        new StatLogDTO
                                        {
                                            ARUMProviderID = provider.Provider.ProviderID,
                                            CRID = 0,
                                            ProviderType = ProviderBL.GetAllActiveProvTypes(_classification).FirstOrDefault()?.ProvType1,
                                            AuditType = type.TypeCode,
                                            BeginDate = periodBeginDate,
                                            DueDate = periodEndDate,
                                            EndDate = periodEndDate,
                                            Notes = $"StatLog Created by OCRT Creation on {DateTime.Now.ToShortDateString()}",
                                            IsActive = true,
                                            UserID = myUser.Email,
                                            Stamp = DateTime.Now,
                                            MCDVersion = "", // Depreciated
                                            MCRVersion = "", // Depreciated
                                            MSComplete = 0, // Depreciated
                                            CHOWID = 0, // Depreciated
                                            OwnerID = 0, // Foreign key to the Owner table. The owner can be an ID from various tables. Chain, Rate, etc. 
                                            OwnerType = ""
                                        },
                                        "CostReportController",
                                        "CreateCostReport",
                                        statLogProvider,
                                        processLifeCycle);

                                    if (statLogID == 0)
                                        throw new Exception($"StatLog failed to be created for provider {provider.Provider.MCAID}, CR Type {type.TypeCode}");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, e.Message);
                    }

                    #endregion

                    // When the 4.90 cost report is created	Initial Audit in Progress
                    #region When the 4.90 cost report is created
                    string eventName = string.Empty;
                    switch (selection.CostReportTypeId)
                    {
                        case "4.90":
                            eventName = "Initial Audit in Progress";
                            break;
                        case "4.91":
                            eventName = "Revised Audit in Progress";
                            break;
                        case "4.92":
                            eventName = "Final Audit in Progress";
                            break;
                    }
                    if (!string.IsNullOrEmpty(eventName))
                    {
                        RateBL.SaveCostReportStatlog(new ProviderCostReportStatlog
                        {
                            CRID = Convert.ToInt32(cr.ProviderCostReportId),
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
                }

                Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] = null;
                if (Session[SESSION_KEYS_OCRT.SELECTED_TAB] != null)
                {
                    string selectedTab = (string)Session[SESSION_KEYS_OCRT.SELECTED_TAB];
                    if (!string.IsNullOrEmpty(selectedTab))
                    {
                        if (selectedTab != "||")
                        {
                            string[] token = selectedTab.Split('|');
                            if (token.Length > 1)
                            {
                                List<Tab> tabs = ScheduleBL.GetScheduleList(providerCostReportId, null);
                                Session[SESSION_KEYS_OCRT.SCHEDULE_WORKSHEETS] = tabs;
                                Tab sParent = tabs.FirstOrDefault(t => t.Name == token[0]);
                                if (token[0] == sParent.Name)
                                {
                                    Tab s = tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == sParent.Name);
                                    Session[SESSION_KEYS_OCRT.SELECTED_TAB] = $"{sParent.Name}|{s.Name}|{""}";
                                }

                            }
                        }
                    }
                }
                // By saving the internal values to session, this saves a lot of lookup
                // as we process the cost report.
                Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] = model;

                selection.InRuleEngineName = model.InRuleEngineName;
                selection.InRuleEngineTbName = model.InRuleEngineTbName;
                selection.VersionName = model.VersionName;
                selection.CostReportType = model.CostReportType;
                selection.FacilityText = model.FacilityText;

                // force reload of schedule dependencies
                Session[SESSION_KEYS_OCRT.SCHEDULE_WORKSHEETS] = null;
                Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] = null;

                return Json(new
                {
                    Success = true,
                    Model = selection,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = "An error was encountered while selecting the cost report.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
            }
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public ActionResult UpdateCostReport()
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
                if (selection != null)
                {
                    MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;

                    // get the internal value for the client ID
                    int? providerId = null;
                    if (int.TryParse(ClientMapping.GetInternalIdFromClientId(selection.ProviderId, "provider"), out int i))
                    {
                        providerId = i;
                    }

                    if (!providerId.HasValue)
                    {
                        _logger.Error("An error occurred retrieving the provider number: {0}", selection.ProviderId);
                        return Json(new { Success = false, Message = @"Invalid request." });
                    }

                    MSLC.ProviderProxy.Interfaces.IArumDetailedProvider provider = _providerServiceProxy.GetProviderById(RetrieveSession.Classification.ARUMToken, providerId.Value, "Rate/Provider");
                    if (provider == null)
                    {
                        _logger.Error("An invalid provider was indicated: {0}", selection.ProviderId);
                        return Json(new { Success = false, Message = @"Invalid request." });
                    }

                    MSLC.ProviderProxy.Interfaces.IArumDetailedProvider providerDetails = _providerServiceProxy.GetProviderInfoByARUMProviderID(RetrieveSession.Classification.ARUMToken, providerId.Value, "CostReport/SelectDocument");
                    if (providerDetails == null)
                    {
                        _logger.Error("An error occurred retrieving provider details for: {0}", provider.Provider.ProviderID);
                        return Json(new { Success = false, Message = @"Invalid request." });
                    }

                    CostReportPeriodListItem period = CostReportHistoryBL.GetPeriodList(providerId).FirstOrDefault(p => p.PeriodCode == selection.PeriodCode);
                    if (period == null)
                    {
                        _logger.Error("An invalid period was indicated: {0}", selection.PeriodCode);
                        return Json(new { Success = false, Message = @"Invalid request." });
                    }

                    CostReportModel cr = new CostReportModel()
                    {
                        ProviderId = providerId,
                        PeriodId = selection.PeriodCode,
                        CostReportTypeId = selection.CostReportTypeId,
                        VersionName = selection.VersionName,
                        ProviderName = provider.Provider.Name,
                        CanEditCostReport = selection.CanEditCostReport
                    };

                    // get the internal value for the client ID
                    string providerCostReportString = ClientMapping.GetInternalIdFromClientId(selection.ProviderCostReportId, "costreport");
                    if (!int.TryParse(providerCostReportString, out int providerCostReportId))
                    {
                        _logger.Error("An invalid cost report provider id was indicated: {0}", providerCostReportString);
                        return Json(new { Success = false, Message = @"Invalid request." });
                    }
                    cr.ProviderCostReportId = providerCostReportString;

                    MSLC.ProviderProxy.Interfaces.Models.Address address = providerDetails.Addresses
                        .Where(x => (x.Type == "FA" || x.Type == null || x.Type == "") && x.IsActive)
                                .OrderByDescending(x => x.CreateDate)
                                .ThenByDescending(x => x.LastModified)
                                .FirstOrDefault();
                    if (address != null)
                    {
                        cr.Address1 = address.Address1;
                        cr.Address2 = address.Address2;
                        cr.City = address.City;
                        cr.ZipCode = address.ZipCode;
                    }

                    MSLC.ProviderProxy.Interfaces.Models.ProviderEffectiveDate provInfo = providerDetails.EffectiveDates
                        .OrderByDescending(x => x.EffectiveDate)
                        .ThenByDescending(x => x.CreateDate)
                        .FirstOrDefault(x => x.EffectiveDate <= period.EndDate);

                    if (provInfo != null)
                    {
                        cr.County = provInfo.County;
                    }

                    foreach (MSLC.ProviderProxy.Interfaces.Models.ProviderIdentifier identifier in providerDetails.Identifiers)
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

                    if (!string.IsNullOrEmpty(provider.Provider.MCAID))
                    {
                        cr.MedicaidId = provider.Provider.MCAID;
                    }

                    cr.ODHId = CostReportBL.GetODHFromAffiliate(_providerServiceProxy, providerDetails);

                    cr.VersionName = selection.VersionName;

                    ManagementBL.UpdateCostReport(cr);
                    return Json(new { Success = true });
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }

            return Json(new { Success = false, Message = "An error was encountered while updating the cost report.", JsonRequestBehavior = JsonRequestBehavior.AllowGet });
        }
        #endregion

        #region Tab Index
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult TabIndex(string providerCostReportId, string tab, string area)
        {
            string selectedTab = (string)Session[SESSION_KEYS_OCRT.SELECTED_TAB];
            if (!string.IsNullOrEmpty(selectedTab) && selectedTab.Contains("|"))
            {
                string[] token = selectedTab.Split('|');
                if (token.Length == 3)
                {
                    // The "area" is the row of menu being loaded
                    switch (area)
                    {
                        case "child":
                            selectedTab = token[2];
                            break;

                        case "sub":
                            selectedTab = token[1];
                            break;

                        case "main":
                        default:
                            selectedTab = token[0];
                            break;
                    }
                }
            }
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
            List<Tab> tabs = LoadTabs();

            string internalProviderCostReportId = ClientMapping.GetInternalIdFromClientId(providerCostReportId, "costreport");
            int facilityRevisionId = 0;
            if (!string.IsNullOrWhiteSpace(internalProviderCostReportId)) facilityRevisionId = int.Parse(internalProviderCostReportId);

            IEnumerable<Tab> index = new List<Tab>();
            // We ONLY want to load "top level" items (tab will be blank) if we are in main area.
            // Otherwise, it means that we are in a lower level of something that doesn't exist
            // ("help" for example)!
            if (!string.IsNullOrWhiteSpace(selectedTab) || string.IsNullOrWhiteSpace(area) || area == "main")
            {
                // Load all sub-items for the "tab" value (the currently selected value of the next higher menu).
                index = tabs.Where(t => t.Parent == (tab ?? string.Empty));
            }

            Tab selectedIndex = new Tab();
            if (string.IsNullOrWhiteSpace(selectedTab) && area == "sub" && tab == "schedule")
            {
                selectedIndex = index.OrderBy(i => i.Sequence)
                            .Skip(1)
                            .FirstOrDefault();
            }
            else
            {
                selectedIndex = index.FirstOrDefault(t => t.Name == tab);
            }

            TabIndexModel model = new TabIndexModel
            {
                FacilityRevisionId = facilityRevisionId,
                Item = index.ToList(),
                // First, attempt to set selected to the selected tab
                Selected = selectedIndex,
                CanEditCostReport = selection.CanEditCostReport,
                CostReportSubmitted = CostReportHistoryBL.CheckSubmissionStatus(facilityRevisionId) != CostReportStatusEnum.Open,
                CostReportTypeId = selection.CostReportTypeId
            };

            if (!selection.CostReportType.StartsWith("4.9"))
            {
                string[] auditOnlyOptions = new string[] { "special narratives", "audit adjustments", "AuditReport", "AuditAdjustmentHistoryReport", "audit comments", "std narr search" };
                model.Item.RemoveAll(x => auditOnlyOptions.Contains(x.Name));
            }

            //if (model.Selected == null) // || model.Item.All(i => i.Name != model.Selected.Name))

            // If specified value is not found, attempt to set selected to the previous value.
            if (model.Selected == null) model.Selected = model.Item.FirstOrDefault(i => i.Name == selectedTab);
            // If previous selected item is not found, then set to the first item in the list.
            if (model.Selected == null) model.Selected = model.Item.OrderBy(i => i.Sequence).FirstOrDefault();

            Session[SESSION_KEYS_OCRT.SELECTED_SCHEDULE] = null;
            if (model.Selected != null) Session[SESSION_KEYS_OCRT.SELECTED_SCHEDULE] = model.Selected;

            Tabs.Clear();
            return PartialView(model);
        }

        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult SelectedTab()
        {
            try
            {
                LoadTabs();

                var s = Tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == string.Empty);
                var main = s != null ? s.Name : string.Empty;

                s = Tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == main);
                string sub = s != null ? s.Name : string.Empty;

                s = Tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == sub);
                string child = s != null ? s.Name : string.Empty;

                string selection = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (!string.IsNullOrEmpty(selection) && selection.Contains("|"))
                {
                    string[] token = selection.Split('|');
                    if (token.Length == 2)
                    {
                        if (Tabs.Any(t => t.Parent == string.Empty && t.Name == token[0]))
                        {
                            main = token[0];
                            if (Tabs.Any(t => t.Parent == main && t.Name == token[1]))
                            {
                                sub = token[1];
                            }
                            else if (Tabs.Any(t => t.Parent == sub && t.Name == token[2]))
                            {
                                s = Tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == main);
                                sub = s != null ? s.Name : string.Empty;
                            }
                            else
                            {
                                s = Tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == sub);
                                child = s != null ? s.Name : string.Empty;
                            }
                        }
                    }
                }

                Tabs.Clear();
                return Json(new { Success = true, Main = main, Sub = sub, Child = child, JsonRequestBehavior = JsonRequestBehavior.AllowGet });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult SelectTab(string main, string sub, string child, bool triggerEdits = false)
        {
            try
            {
                List<Tab> tabs = LoadTabs();

                string selectedTab = (string)Session[SESSION_KEYS_OCRT.SELECTED_TAB];
                string selectedMain = main;
                string selectedSub = sub;
                string selectedChild = child;
                if (!string.IsNullOrEmpty(selectedTab) && selectedTab.Contains("|"))
                {
                    string[] token = selectedTab.Split('|');
                    if (token.Length == 3)
                    {
                        selectedMain = token[0];
                        selectedSub = token[1];
                        selectedChild = token[2];
                    }
                }

                if (string.IsNullOrEmpty(main) || !tabs.Any(t => t.Parent == string.Empty && t.Name == main))
                {
                    main = selectedMain;
                    if (string.IsNullOrEmpty(main) || !tabs.Any(t => t.Parent == main && t.Name == sub))
                    {
                        Tab m = tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == "");
                        main = m != null ? m.Name : string.Empty;
                    }
                }

                if (!main.Equals("costreporthelp"))
                {
                    if (string.IsNullOrEmpty(sub) || !tabs.Any(t => t.Parent == main && t.Name == sub))
                    {
                        //sub = selectedSub;
                        //if (string.IsNullOrEmpty(sub) || !Tabs.Any(t => t.Parent == main && t.Name == sub))
                        //{
                        Tab s = tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == main);
                        if (!string.IsNullOrWhiteSpace(selectedSub))
                        {
                            sub = selectedSub;
                        }
                        else
                        {
                            sub = s != null ? s.Name : string.Empty;
                        }
                        //}
                    }
                }
                else
                {
                    sub = string.Empty;
                    child = string.Empty;
                }

                // We only want to do this if we are NOT on "Help" (which has no sub or child items),
                // or we know we have a child item in queue.
                if (!main.Equals("costreporthelp") && (string.IsNullOrEmpty(child) || !tabs.Any(t => t.Parent == sub && t.Name == child)))
                {
                    //child = selectedChild;
                    //if (string.IsNullOrEmpty(child) || !Tabs.Any(t => t.Parent == sub && t.Name == child))
                    //{
                    Tab c = tabs.OrderBy(t => t.Sequence).FirstOrDefault(t => t.Parent == sub);
                    child = c != null ? c.Name : string.Empty;
                    //}
                }

                // calculate the schedule that's being left
                // NOTE: if using local calculation engine, then NeedCalc() will not return "true".
                if (selectedMain == "schedule")
                {
                    // Do we need to do server side calculations?
                    if (NeedCalc())
                    {
                        SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                        if (selection != null && int.TryParse(selection.ProviderCostReportId, out int providerCostReportId))
                        {
                            // If we have calculations pending and going to or from trial balance, need to
                            // recalculate the whole document.
                            if (selectedSub == "TrialBal" || sub == "TrialBal") ScheduleBL.CalculateAllSchedules(providerCostReportId);
                            // Otherwise, just calcualte the specific schedule.
                            else ScheduleBL.CalculateSingleSchedule(providerCostReportId, selectedSub);
                            Session[SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED] = null;
                        }
                    }
                }

                if (triggerEdits)
                {
                    // Trigger reprocessing edits
                    Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "RECALC";
                    Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] = null;
                }

                Session[SESSION_KEYS_OCRT.SELECTED_TAB] = string.Format("{0}|{1}|{2}", main, sub, child);
                Tabs.Clear();
                return Json(new
                {
                    Success = true,
                    Data = Session[SESSION_KEYS_OCRT.SELECTED_TAB],
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                Session[SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED] = null;
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = null;

                return Json(new
                {
                    Success = false,
                    Message = "An error was encountered while trying to navigate to the tab.",
                    Data = Session[SESSION_KEYS_OCRT.SELECTED_TAB]
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Check the session flags to determine if schedule calculations are needed.
        /// If using LOCAL CALCULATION ENGINE this will always return FALSE.
        /// </summary>
        /// <returns></returns>
        public bool NeedCalc()
        {
            // If we are using local calculation engine, then it should not need recalculation.
            if ((bool)(Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] ?? true)) return false;

            // assume false - we want the session value to tell us if there was
            // an update that requires recalculations to occur.
            return !string.IsNullOrWhiteSpace((string)(Session[SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED]));
        }

        public JsonResult GetCostReportEditsModel(string sidx, string sord)
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                ClassificationDTO classification = RetrieveSession.Classification;

                if (!CostReportPermsissionsBL.CheckPermissionCostReportView(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request to load edits."
                    }, JsonRequestBehavior.AllowGet);
                }

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                // Are there already edits loaded?
                bool editsLoaded = Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] != null;
                bool canEdit = selection?.CanEditCostReport ?? false;
                bool needsReprocessing = !string.IsNullOrWhiteSpace(Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED]?.ToString() ?? "");

                List<EditResult> editsList;

                // If there are edits loaded and don't need to reprocess, then use them.
                if (!needsReprocessing && editsLoaded)
                {
                    editsList = (List<EditResult>)Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT];
                }
                // If we need to reprocess, do so
                else if (canEdit && needsReprocessing)
                {
                    // get the provider package
                    RateSettingPackage.ProviderPackage providerPackage = (RateSettingPackage.ProviderPackage)Session[SESSION_KEYS_OCRT.PROVIDER_DATA];

                    editsList = CostReportEditsBL.GetCostReportEdits(SimplifiedType, providerCostReportId, selection.PeriodCode, providerPackage, selection.CostReportTypeId);

                    // reprocessed, so clear out need to do that again.
                    Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = null;

                    // Update the session edits.
                    Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] = editsList;
                }
                // otherwise, we just load from the database.
                else
                {
                    editsList = CostReportEditsBL.ReadCostReportEditsList(providerCostReportId);
                }

                // If this is a sort order request, sort the data before returning.
                if (!string.IsNullOrEmpty(sidx))
                {
                    // Determine the property being sorted by, and then sort by specified order.
                    switch (sidx)
                    {
                        case "CostReportSchedule":
                            if (sord == "asc") editsList = editsList.OrderBy(x => x.CostReportSchedule).ToList();
                            else editsList = editsList.OrderByDescending(x => x.CostReportSchedule).ToList();
                            break;
                        case "DataElementName":
                            if (sord == "asc") editsList = editsList.OrderBy(x => x.DataElementName).ToList();
                            else editsList = editsList.OrderByDescending(x => x.DataElementName).ToList();
                            break;
                        case "DataElementSuffix":
                            if (sord == "asc") editsList = editsList.OrderBy(x => x.DataElementSuffix).ToList();
                            else editsList = editsList.OrderByDescending(x => x.DataElementSuffix).ToList();
                            break;
                        case "Message":
                            if (sord == "asc") editsList = editsList.OrderBy(x => x.Message).ToList();
                            else editsList = editsList.OrderByDescending(x => x.Message).ToList();
                            break;
                        case "EditNumber":
                            if (sord == "asc") editsList = editsList.OrderBy(x => x.EditNumber.ToInt()).ToList();
                            else editsList = editsList.OrderByDescending(x => x.EditNumber.ToInt()).ToList();
                            break;
                        default:
                            break;
                    }
                }
                
                Models.jqGrid.ResultModel results = new Models.jqGrid.ResultModel();
                List<Models.jqGrid.RowModel> rows = new List<Models.jqGrid.RowModel>();

                List<string> columns = new List<string> {
                    "CostReportSchedule",
                    "DataElementName",
                    "DataElementSuffix",
                    "Message",
                    "EditNumber"
                };

                PropertyInfo[] props = typeof(EditResult).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                for (int currentDisplayLine = 0; currentDisplayLine < editsList.Count; currentDisplayLine++)
                {
                    EditResult obj = editsList[currentDisplayLine];
                    Dictionary<string, string> dataRow = columns.ToDictionary(k => k, k => props.FirstOrDefault(x => x.Name == k).GetValue(obj)?.ToString() ?? "");

                    // Add the enumeration description rather than the string literal.
                    dataRow.Add("Type", ((int)obj.Type).ToString());

                    rows.Add(
                        new Models.jqGrid.RowModel
                        {
                            id = currentDisplayLine + 1,
                            cell = dataRow.Keys.ToDictionary(k => k, k => dataRow[k])
                        });
                }

                results.page = 1;
                results.total = 1;
                results.records = rows.Count;
                results.rows = rows.ToArray();
                results.GridCellDefArray = string.Empty;

                return Json(results, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new
                {
                    Success = false,
                    Message = "Unable to retrieve cost report edits."
                }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion


        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetOcrtData()
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                ICalculationModelInRule modelObject = CostReportBL.GetOcrtModelData(providerCostReportId, SimplifiedType);

                return Json(new
                {
                    Success = true,
                    Model = modelObject
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new
                {
                    Success = false,
                    Message = "Unable to return data; " + e.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetTbData()
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                ICalculationModelInRule modelObject = CostReportBL.GetTbModelData(providerCostReportId, SimplifiedType);

                return Json(new
                {
                    Success = true,
                    Model = modelObject
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Unable to return data; " + e.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        [HttpGet]
        public ActionResult Viewport()
        {
            try
            {

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                ClassificationDTO classification = RetrieveSession.Classification;

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                int providerId = 0;
                if (int.TryParse(selection.ProviderId, out int p))
                {
                    providerId = p;
                }

                List<Tab> tabs = LoadTabs();

                string selectedTab = (string)Session[SESSION_KEYS_OCRT.SELECTED_TAB];
                if (string.IsNullOrEmpty(selectedTab) || !selectedTab.Contains("|"))
                {
                    throw new Exception(@"Unable to determine the selected tab.");
                }

                string[] token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new Exception(@"Unable to determine the selected tab.");
                }

                string main = token[0];
                string sub = token[1];
                string child = token[2];

                if (string.IsNullOrEmpty(main) || !tabs.Any(t => t.Parent == string.Empty && t.Name == main))
                {
                    throw new Exception(@"An invalid tab was selected.");
                }

                if (!string.IsNullOrEmpty(sub) && !tabs.Any(t => t.Parent == main && t.Name == sub))
                {
                    throw new Exception(@"An invalid tab was selected.");
                }

                Tabs.Clear();
                if (main.Equals("management") && sub.Equals("version") && child.Equals("CreateNewFacilityVersion"))
                {
                    return RedirectToAction("CreateNewFacility", "Management");
                }
                else if (main.Equals("management") && sub.Equals("version") && child.Equals("CopyCostReport"))
                {
                    return RedirectToAction("Version", "Management");
                }
                else if (main.Equals("management") && sub.Equals("version") && child.Equals("ExportExcel"))
                {
                    return RedirectToAction("ExportExcel", "Management");
                }
                else if (main.Equals("management") && sub.Equals("version") && child.Equals("ExportText"))
                {
                    return RedirectToAction("ExportText", "Management");
                }
                else if (main.Equals("management") && sub.Equals("note"))
                {
                    return RedirectToAction("Note", "Management");
                }
                else if (main.Equals("management") && sub.Equals("AuditReport"))
                {
                    return RedirectToAction("AuditReport", "Management");
                }
                else if (main.Equals("management") && sub.Equals("AuditAdjustmentHistoryReport"))
                {
                    return RedirectToAction("AuditAdjustmentHistoryReport", "Management");
                }
                else if (main.Equals("management") && (sub.Equals("submit") || sub.Equals("lock")))
                {
                    List<CostReportEdits> editList = CostReportEditsBL.ReadCostReportEdits(providerCostReportId);
                    bool exceptRE = editList.Any(t => (t.EditType.Equals(EditType.REQUIRED.ToString())) || (t.EditType.Equals(EditType.ERROR.ToString())));
                    CostReportSubmissionModel submitModel = new CostReportSubmissionModel
                    {
                        // map to client id for sending to client.
                        ProviderCostReportId = ClientMapping.MapInternalIdToClientId(selection.ProviderCostReportId, "costreport"),
                        UnresolvedExceptions = exceptRE,
                        CostReportIsSubmitted = CostReportHistoryBL.CheckSubmissionStatus(providerCostReportId) != CostReportStatusEnum.Open,
                        CostReportIsLocked = CostReportHistoryBL.CheckLockedStatus(providerCostReportId),
                        CostReportIsOutsideScope = CostReportHistoryBL.CheckOutsideScopeStatus(selection.PeriodCode, selection.CostReportType),
                        LocalCalculations = (bool)(Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] ?? true),
                        CanToggleLocalCalculations = OHFISession.CanAccessProviderFeature("OHFI:SettingsManagement"),
                        SubmissionAuthority = OHFISession.GetApplicationInformation.lstApplicationSettings.FirstOrDefault(x => x.Category == "SubmissionAuthority")?.Value ?? "State Submission Authority",
						CostReportMessage = ""
                    };

                    submitModel.CostReportUsedInRate =
                        CostReportHistoryBL.CheckRateStatus(providerId, providerCostReportId, selection.CostReportType);
                    submitModel.CanSubmitCostReport = CostReportPermsissionsBL.CheckPermissionCostReportSubmit(selection.CostReportTypeId);

                    submitModel.CanLockCostReport = CostReportPermsissionsBL.CheckPermissionLockCostReport(selection.CostReportTypeId);
                    ICostReportUtils costReportUtils = CostReportBL.GetInstanceCostReportUtils(RetrieveSession.Classification, selection.PeriodCode);

                    if (submitModel.CostReportIsSubmitted)
                        submitModel.CostReportMessage = "This cost report version has been Uploaded and marked \"Submitted\".";
                    else if (exceptRE)
                        submitModel.CostReportMessage = "Please review the Edits tab in the Schedules section and correct any outstanding errors. The Cost Report cannot be uploaded until these errors are corrected.";
                    else if (submitModel.CostReportIsOutsideScope)
                        submitModel.CostReportMessage = costReportUtils.GetOutOfScopeMessage(selection.CostReportType, submitModel.SubmissionAuthority);

                    return PartialView("Submit", submitModel);
                }
                else
                {
                    return RedirectToAction("Index", "Schedule");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Content("");
            }
        }

        public JsonResult LockCostReport()
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionLockCostReport(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for lock cost report."
                    }, JsonRequestBehavior.AllowGet);
                }

                // get the internal value for the client ID
                if (string.IsNullOrWhiteSpace(selection.ProviderCostReportId) || selection.ProviderCostReportId == "0")
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for lock cost report."
                    }, JsonRequestBehavior.AllowGet);
                }

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                CostReportHistoryBL.LockCostReport(providerCostReportId, DateTime.Now.ToShortDateString());

                // update the cached model.
                selection.CanEditCostReport = false;

                #region When the 4.90 cost report
                string eventName = string.Empty;
                switch (selection.CostReportTypeId)
                {
                    case "4.90":
                        eventName = "Initial Audit is Locked";
                        break;
                    case "4.91":
                        eventName = "Revised Audit in Locked";
                        break;
                    case "4.92":
                        eventName = "Final Audit in Locked";
                        break;
                }
                if (!string.IsNullOrEmpty(eventName))
                {
                    RateBL.SaveCostReportStatlog(new ProviderCostReportStatlog
                    {
                        CRID = providerCostReportId,
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
                    Message = "An exception occurred locking the cost report."
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult UnLockCostReport()
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionLockCostReport(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request."
                    }, JsonRequestBehavior.AllowGet);
                }

                // get the internal value for the client ID
                if (string.IsNullOrWhiteSpace(selection.ProviderCostReportId) || selection.ProviderCostReportId == "0")
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for lock cost report."
                    }, JsonRequestBehavior.AllowGet);
                }

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                CostReportHistoryBL.UnLockCostReport(providerCostReportId, DateTime.Now.ToShortDateString());

                // update the cached model.
                selection.CanEditCostReport = true;

                #region When the 4.90 cost report is created
                string eventName = string.Empty;
                switch (selection.CostReportTypeId)
                {
                    case "4.90":
                        eventName = "Initial Audit is Unlocked";
                        break;
                    case "4.91":
                        eventName = "Revised Audit in Unlocked";
                        break;
                    case "4.92":
                        eventName = "Final Audit in Unlocked";
                        break;
                }
                if (!string.IsNullOrEmpty(eventName))
                {
                    RateBL.SaveCostReportStatlog(new ProviderCostReportStatlog
                    {
                        CRID = providerCostReportId,
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
                    Message = "An exception occurred locking the cost report."
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult SubmitCostReport()
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
                ClassificationDTO classification = RetrieveSession.Classification;
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCostReportSubmit(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request."
                    }, JsonRequestBehavior.AllowGet);
                }

                // get the internal value for the client ID
                if (string.IsNullOrWhiteSpace(selection.ProviderCostReportId) || selection.ProviderCostReportId == "0")
                {
                    return Json(
                        new { Success = false, Message = "Invalid request for submit cost report." }
                        , JsonRequestBehavior.AllowGet);
                }

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                if (!CostReportHistoryBL.CheckLockedStatus(providerCostReportId))
                {
                    return Json(
                        new { Success = false, Message = "Cost report is not locked; cannot submit." }
                        , JsonRequestBehavior.AllowGet);
                }

                RateSettingPackage.ProviderPackage providerData = (RateSettingPackage.ProviderPackage)Session[SESSION_KEYS_OCRT.PROVIDER_DATA];
                // recalculate and process edits to ensure that this is ready for submission.
                // this is to happen here regardless if we are using local engine or server engine

                ScheduleBL.CalculateAllSchedules(providerCostReportId);
                List<EditResult> editList = CostReportEditsBL.GetCostReportEdits(SimplifiedType, providerCostReportId, selection.PeriodCode, providerData, selection.CostReportTypeId);
                if (editList.Any(t => (t.Type.Equals(EditType.REQUIRED)) || (t.Type.Equals(EditType.ERROR))))
                {
                    // There are edits that should prevent submission.
                    return Json(
                       new { Success = false, unresolvedExceptions = true, Message = "One or more cost report edits are preventing submission. Please review and try again." },
                       JsonRequestBehavior.AllowGet);
                }

                string appVersion = typeof(MvcApplication).Assembly.GetName().Version.ToString();
                string version = string.Empty;
                if (selection.CostReportTypeId.StartsWith("4.9"))
                    version = selection.VersionName;
                else
                    version = CostReportBL.GetDocumentNumber();

                ICostReportUtils costReportUtils = CostReportBL.GetInstanceCostReportUtils(RetrieveSession.Classification, selection.PeriodCode);
                System.IO.MemoryStream s = CostReportHistoryBL.SubmitCostReport(providerCostReportId, appVersion, version, out string filename);

                CostReportStatusEnum status = costReportUtils.GetCrStatusForSubmit(editList);
                int? statusID = (int)status;
                string statusName = status.GetDescription();

                #region OCRT- StatLog Connection

                int statLogID = -1;
                string crType = string.Empty;
                try
                {
                    //If OCRT-StatLog is not enabled, skip
                    string connectionCheck = ARUMConfigurationOHFI.EnableOCRTStatLogConnection(classification);
                    if (connectionCheck.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //Create/Update StatLog with CostReport info
                        var _classification = RetrieveSession.Classification;

                        //Get Cost Report Info
                        //List<DocumentData> list = CostReportBL.GetCostReportData(providerCostReportId);
                        var costReportInfo = CostReportHistoryBL.GetBasicCostReportInformation(providerCostReportId);
                        var providerID = costReportInfo.ProvID;
                        //string periodBegin = list.FirstOrDefault(x => x.FieldTemplate.RangeName == "10370")?.Data ?? string.Empty;
                        var periodBeginDate = costReportInfo.PeriodBegin;
                        //string periodEnd = list.FirstOrDefault(x => x.FieldTemplate.RangeName == "10380")?.Data ?? string.Empty;
                        var periodEndDate = costReportInfo.PeriodEnd;
                        //string typeCode = list.FirstOrDefault(x => x.FieldTemplate.RangeName == "10000")?.Data ?? string.Empty;
                        var type = costReportInfo.CostReportTypeCode;
                        if (type.StartsWith("4.9"))
                            crType = "4.9";
                        else
                            crType = type;
                            //Get Cost Report from ID
                            int ARUMProviderID = providerID;
                        //Search for Statlog based on CostReport info

                        var providerStatLogs = StatLogBL.GetStatLogsByARUMProvider(_classification, ARUMProviderID);
                        var preexistingStatLog = providerStatLogs.FirstOrDefault(sl =>
                            sl.AuditType.Trim().ToUpper() == crType.Trim().ToUpper()
                            && sl.EndDate.Year == periodEndDate.Year
                            && (sl.IsActive == true || sl.IsActive == null));


                        var provider = ProviderBL.GetProviderInfoByARUMUserAndProviderID(_classification, RetrieveSession.ARUMUser, providerID);
                        var PortalURL = new UrlHelper(((MvcHandler)System.Web.HttpContext.Current.Handler).RequestContext).AbsoluteAction("Login", "Home");
                        var processLifeCycle = new ProcessLifeCycle(_classification, PortalURL, _arumUserProxy, _fileServiceProxy, _providerServiceProxy);

                        //Create Statlog if it doesn't exist
                        if (preexistingStatLog == null)
                        {
                            statLogID = StatLogBL.InsertStatLog(_classification,
                                new StatLogDTO
                                {
                                    ARUMProviderID = ARUMProviderID,
                                    CRID = 0,
                                    ProviderType = ProviderBL.GetAllActiveProvTypes(_classification).FirstOrDefault()?.ProvType1,
                                    AuditType = crType,
                                    BeginDate = periodBeginDate,
                                    DueDate = periodEndDate,
                                    EndDate = periodEndDate,
                                    Notes = $"StatLog Created by OCRT Submission on {DateTime.Now.ToShortDateString()}",
                                    IsActive = true,
                                    UserID = myUser.Email,
                                    Stamp = DateTime.Now,
                                    MCDVersion = "", // Depreciated
                                    MCRVersion = "", // Depreciated
                                    MSComplete = 0, // Depreciated
                                    CHOWID = 0, // Depreciated
                                    OwnerID = 0, // Foreign key to the Owner table. The owner can be an ID from various tables. Chain, Rate, etc. 
                                    OwnerType = ""
                                    // The type indicating the owner of this StatLog record. Some records are owned by providers, others by firms:
                                    // Chains, Mgmt Firms, etc, or perhaps by Rate records. Use P = Providers, C = Chains, A = Accounting firms, M = Mgmt, R = Rates.
                                },
                                "CostReportController",
                                "SubmitCostReport",
                                provider,
                                processLifeCycle);
                        }
                        else
                        {
                            statLogID = preexistingStatLog.CRID;
                        }

                        //Get EventIDs for required Events from ARUM Settings
                        int initialSubmitEventID = -1;
                        int subsequentSubmitEventID = -1;
                        int auditedSubmitEventID = -1;
                        string initialEventConfig = null;
                        //CategoryName: StatLog: 4.90_AuditedCostReportSubmissionEventID Value: 244

                        if (type.StartsWith("4.9"))
                        {
                           var auditedEventConfig = ARUMConfigurationOHFI.AuditedCostReportSubmissionEventID(_classification, type);

                            var eventChainIDStrings = auditedEventConfig.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                            List<int> auditedEventChainIDs = new List<int>();
                            foreach (var eventChainIDString in eventChainIDStrings)
                            {
                                if (!int.TryParse(eventChainIDString, out auditedSubmitEventID))
                                    throw new Exception($"Audited Cost Report Submission Event ID for {type} setting is not an integer list. ({RetrieveSession.Classification?.Type ?? "Null"})");

                                auditedEventChainIDs.Add(auditedSubmitEventID);
                            }
                            if (auditedEventChainIDs == null || !auditedEventChainIDs.Any())
                                throw new Exception($"Audited Cost Report Submission Event ID for {type} setting is not an integer list. ({RetrieveSession.Classification?.Type ?? "Null"})");

                            var certificationEventName = ARUMConfigurationOHFI.CostReportSubmitStatlogName(_classification);
                            //Check if Statlog already has Initial Submission upload
                            int parentEventID = 0;
                            bool doesInitialUploadExist = true;
                            int uploadEventID = MSLC.StatLog.Configuration.StatLogConfiguration.EventIDForUpload;
                            HistoryDTO parentHistory = null;
                            var statLogHistory = StatLogCostReportHistory.CostReportHistoryBL.GetHistoryByCostReportID(_classification, statLogID, myUser.UserID, "CostReportController", "SubmitCostReport", providerID, false);
                            var initialSubmissionHistory = statLogHistory.FirstOrDefault(h => h.EventID == auditedSubmitEventID);
                            //Check if initial sumbission event exists in statlog
                            if (initialSubmissionHistory == null)
                            {
                                doesInitialUploadExist = false;
                                parentEventID = auditedSubmitEventID;
                            }
                            //Check if there is a certification for the initial submission
                            else if (initialSubmissionHistory.IsActive && !statLogHistory.Any(h => h.ParentHistoryID == initialSubmissionHistory.ParentHistoryID && certificationEventName.Split(',').Contains(h.EventName) && h.IsReviewOkay && h.IsActive))
                            {
                                doesInitialUploadExist = false;
                                parentEventID = auditedSubmitEventID;
                                parentHistory = statLogHistory.FirstOrDefault(h => h.EventID == auditedSubmitEventID).ToDTO();
                            }
                            else
                                parentEventID = subsequentSubmitEventID;

                            StatLogDTO statLog = StatLogBL.GetStatLogByStatLogID(_classification, statLogID);
                            EventDO parentEventDO = EventBL.GetEventByID(_classification, parentEventID);
                            if (parentEventDO == null)
                            {
                                throw new Exception($"Event ID {parentEventID} does not exist in the database. ({RetrieveSession.Classification?.Type ?? "Null"})");
                            }

                            //If for some reason there hasn't been an initial upload and the event isn't in the statlog yet (ex: not part of default events), we'll add it now
                            if (!doesInitialUploadExist && parentHistory == null)
                            {
                                var startingEventID = -1;

                                //loop through eventID list to add the events needed for an initial submission
                                for (int e = auditedEventChainIDs.Count - 1; e >= 0; e--)
                                {
                                    startingEventID = auditedEventChainIDs[e];
                                    if (statLogHistory.FirstOrDefault(h => h.EventID == auditedSubmitEventID) != null)
                                        break;
                                }
                                //Add event w/ workflow
                                var workflowParentHistory = StatLogCostReportHistory.CostReportHistoryBL.SaveAddInteractive(_classification,
                                    statLogID, startingEventID, DateTime.Now, null, 0, myUser.Email, string.Empty);
                                var workflowParentEvent = EventBL.GetEventByID(_classification, startingEventID);

                                processLifeCycle.Create(workflowParentHistory, workflowParentEvent, myUser, RetrieveSession.IsMSLCEmployee, statLog, provider);

                                //Refresh statlog history and grab the history that we want to upload to (there is only one for initial CR submission)
                                statLogHistory = StatLogCostReportHistory.CostReportHistoryBL.GetHistoryByCostReportID(_classification, statLogID, myUser.UserID, "CostReportController", "SubmitCostReport", providerID, false);
                                parentHistory = statLogHistory.FirstOrDefault(h => h.EventID == auditedSubmitEventID) == null ? null :
                                    statLogHistory.FirstOrDefault(h => h.EventID == auditedSubmitEventID).ToDTO();

                                //If parent history is still null something went wrong and we should throw an error
                                if (parentHistory == null)
                                    throw new Exception($"Program was unable to create the history for the Initial Event. Check your event workflow to verify the event is added through a workflow process ({parentEventDO.Name})");
                            }

                            var fileName = $"{provider.MCAID}_{periodEndDate.ToString("yyyyMMdd")}_{type}";
                            var receiptString = $"Audited Cost Report Submitted. Document Number: {version}. Submitter: {myUser.FirstName} {myUser.LastName}. Submission Date: {costReportInfo.SubmitDate}";

                            //Create receipt file
                            var receiptFileResult = StatLogCostReportHistory.CostReportHistoryBL.GenerateAndUploadFileReceipt(RetrieveSession.Classification, parentEventID, ARUMProviderID, fileName, receiptString,
                                RetrieveSession.ARUMUser.UserID, RetrieveSession.IsMSLCEmployee);

                            if (receiptFileResult == null || receiptFileResult.File == null)
                            {
                                throw new Exception($"Error uploading receipt to ARUM: Null was returned");
                            }
                            if (!receiptFileResult.Result.Successful)
                            {
                                throw new Exception($"Error uploading receipt to ARUM: {receiptFileResult.Result.Message}");
                            }

                            IFile file = receiptFileResult.File;

                            IFileType applicationFileType = _fileServiceProxy.GetApplicationFileTypesByApplicationID(myUser.UserID, "StatLog/SaveFile")?
                                .FileTypes?.FirstOrDefault(aft => aft.ApplicationFileTypeID == file.ApplicationFileTypeID);

                            if (applicationFileType == null || applicationFileType.ApplicationFileTypeID < 1)
                                throw new Exception("Error: Issue Retrieving File Type.");

                            if (applicationFileType.ParentFileTypeID == null)
                                throw new Exception("Error: Issue Retrieving File Type Parent.");

                            //Create upload history for receipt and tie to submission event
                            HistoryDTO uploadHistory = StatLogBL.InsertHistory(_classification, statLogID,
                                uploadEventID, parentHistory.ID,
                                receiptString, false, myUser.Email, file.FileID, DateTime.Now, null,
                                null, null, myUser.UserID, "CostReportController", "SubmitCostReport", provider.ARUMProviderID);

                            string emailResult = processLifeCycle.Upload(parentHistory, parentEventDO, myUser, RetrieveSession.IsMSLCEmployee, statLog, provider,
                                applicationFileType.ParentFileTypeName, (int)applicationFileType.ParentFileTypeID, uploadHistory.ID, file.FileID, file.FileName);
                        }
                        else
                        {
                            initialEventConfig = ARUMConfigurationOHFI.InitialCostReportSubmissionEventID(_classification, type);

                            var eventChainIDStrings = initialEventConfig.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                            List<int> initialEventChainIDs = new List<int>();
                            foreach (var eventChainIDString in eventChainIDStrings)
                            {
                                if (!int.TryParse(eventChainIDString, out initialSubmitEventID))
                                    throw new Exception($"Initial Cost Report Submission Event ID for {type} setting is not an integer list. ({RetrieveSession.Classification?.Type ?? "Null"})");

                                initialEventChainIDs.Add(initialSubmitEventID);
                            }
                            if (initialEventChainIDs == null || !initialEventChainIDs.Any())
                                throw new Exception($"Initial Cost Report Submission Event ID for {type} setting is not an integer list. ({RetrieveSession.Classification?.Type ?? "Null"})");

                            var subsequentEventConfig = ARUMConfigurationOHFI.SubsequentCostReportSubmissionEventID(_classification);
                            eventChainIDStrings = subsequentEventConfig.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                            List<int> subsequentEventChainIDs = new List<int>();
                            foreach (var eventChainIDString in eventChainIDStrings)
                            {
                                if (!int.TryParse(eventChainIDString, out subsequentSubmitEventID))
                                    throw new Exception($"Subsequent Cost Report Submission Event ID setting is not an integer list. ({RetrieveSession.Classification?.Type ?? "Null"})");

                                subsequentEventChainIDs.Add(subsequentSubmitEventID);
                            }
                            if (subsequentEventChainIDs == null || !subsequentEventChainIDs.Any())
                                throw new Exception($"Subsequent Cost Report Submission Event ID setting is not an integer list. ({RetrieveSession.Classification?.Type ?? "Null"})");

                            var certificationEventName = ARUMConfigurationOHFI.CostReportSubmitStatlogName(_classification);
                            //Check if Statlog already has Initial Submission upload
                            int parentEventID = 0;
                            bool doesInitialUploadExist = true;
                            int uploadEventID = MSLC.StatLog.Configuration.StatLogConfiguration.EventIDForUpload;
                            HistoryDTO parentHistory = null;
                            var statLogHistory = StatLogCostReportHistory.CostReportHistoryBL.GetHistoryByCostReportID(_classification, statLogID, myUser.UserID, "CostReportController", "SubmitCostReport", providerID, false);
                            var initialSubmissionHistory = statLogHistory.FirstOrDefault(h => h.EventID == initialSubmitEventID);
                            //Check if initial sumbission event exists in statlog
                            if (initialSubmissionHistory == null)
                            {
                                doesInitialUploadExist = false;
                                parentEventID = initialSubmitEventID;
                            }
                            //Check if there is a certification for the initial submission
                            else if (initialSubmissionHistory.IsActive && !statLogHistory.Any(h => h.ParentHistoryID == initialSubmissionHistory.ParentHistoryID && certificationEventName.Split(',').Contains(h.EventName) && h.IsReviewOkay && h.IsActive))
                            {
                                doesInitialUploadExist = false;
                                parentEventID = initialSubmitEventID;
                                parentHistory = statLogHistory.FirstOrDefault(h => h.EventID == initialSubmitEventID).ToDTO();
                            }
                            else
                                parentEventID = subsequentSubmitEventID;

                            StatLogDTO statLog = StatLogBL.GetStatLogByStatLogID(_classification, statLogID);
                            EventDO parentEventDO = EventBL.GetEventByID(_classification, parentEventID);
                            if (parentEventDO == null)
                            {
                                throw new Exception($"Event ID {parentEventID} does not exist in the database. ({RetrieveSession.Classification?.Type ?? "Null"})");
                            }

                            //If for some reason there hasn't been an initial upload and the event isn't in the statlog yet (ex: not part of default events), we'll add it now
                            if (!doesInitialUploadExist && parentHistory == null)
                            {
                                var startingEventID = -1;

                                //loop through eventID list to add the events needed for an initial submission
                                for (int e = initialEventChainIDs.Count - 1; e >= 0; e--)
                                {
                                    startingEventID = initialEventChainIDs[e];
                                    if (statLogHistory.FirstOrDefault(h => h.EventID == initialSubmitEventID) != null)
                                        break;
                                }
                                //Add event w/ workflow
                                var workflowParentHistory = StatLogCostReportHistory.CostReportHistoryBL.SaveAddInteractive(_classification,
                                    statLogID, startingEventID, DateTime.Now, null, 0, myUser.Email, string.Empty);
                                var workflowParentEvent = EventBL.GetEventByID(_classification, startingEventID);

                                processLifeCycle.Create(workflowParentHistory, workflowParentEvent, myUser, RetrieveSession.IsMSLCEmployee, statLog, provider);

                                //Refresh statlog history and grab the history that we want to upload to (there is only one for initial CR submission)
                                statLogHistory = StatLogCostReportHistory.CostReportHistoryBL.GetHistoryByCostReportID(_classification, statLogID, myUser.UserID, "CostReportController", "SubmitCostReport", providerID, false);
                                parentHistory = statLogHistory.FirstOrDefault(h => h.EventID == initialSubmitEventID) == null ? null :
                                    statLogHistory.FirstOrDefault(h => h.EventID == initialSubmitEventID).ToDTO();

                                //If parent history is still null something went wrong and we should throw an error
                                if (parentHistory == null)
                                    throw new Exception($"Program was unable to create the history for the Initial Event. Check your event workflow to verify the event is added through a workflow process ({parentEventDO.Name})");
                            }

                            //Create Subsequent Submission from the start of its parent tree if an Initial submission has already been done
                            else if (doesInitialUploadExist)
                            {
                                bool availableSubsequentHistory = false;
                                HistoryDO parentHistoryDO = null;
                                //Check if the latest Subsequent Submission history is a non-certified Subsequest history that we can upload to
                                var existingSubHistory = statLogHistory.Where(h => h.EventID == subsequentSubmitEventID).OrderBy(h => h.Stamp).LastOrDefault();
                                if (existingSubHistory != null)
                                {
                                    //Check if subsequent history is certified
                                    if (existingSubHistory.IsActive && !statLogHistory.Any(h => h.ParentHistoryID == existingSubHistory.ParentHistoryID && certificationEventName.Split(',').Contains(h.EventName) && h.IsReviewOkay && h.IsActive))
                                    {
                                        availableSubsequentHistory = true;
                                        parentHistoryDO = statLogHistory.FirstOrDefault(h => h.ID == existingSubHistory.ID);
                                    }
                                }

                                //Otherwise create a new subsequent submission history
                                if (!availableSubsequentHistory)
                                {
                                    int startingEventID = subsequentEventChainIDs.First();

                                    var workflowParentHistory = StatLogCostReportHistory.CostReportHistoryBL.SaveAddInteractive(_classification,
                                        statLogID, startingEventID, DateTime.Now, null, 0, myUser.Email, string.Empty);
                                    var workflowParentEvent = EventBL.GetEventByID(_classification, startingEventID);

                                    processLifeCycle.Create(workflowParentHistory, workflowParentEvent, myUser, RetrieveSession.IsMSLCEmployee, statLog, provider);

                                    //Refresh statlog history and search for (based on workflow) the history that we want to upload to
                                    statLogHistory = StatLogCostReportHistory.CostReportHistoryBL.GetHistoryByCostReportID(_classification, statLogID, myUser.UserID, "CostReportController", "SubmitCostReport", providerID, false);
                                    parentHistoryDO = StatLogCostReportHistory.CostReportHistoryBL.SearchWorkflowForEvent(subsequentSubmitEventID, statLogHistory.FirstOrDefault(h => h.ID == workflowParentHistory.ID), statLogHistory);
                                }

                                //If parent history is still null something went wrong and we should throw an error
                                if (parentHistoryDO == null)
                                    throw new Exception($"Program was unable to create the history for the Subsequent Event. Check your event workflow to verify the event is added through a workflow process ({parentEventDO.Name})");

                                parentHistory = parentHistoryDO.ToDTO();
                            }

                            var fileName = $"{provider.MCAID}_{periodEndDate.ToString("yyyyMMdd")}_{type}";
                            var receiptString = $"Cost Report Submitted. Document Number: {version}. Submitter: {myUser.FirstName} {myUser.LastName}. Submission Date: {costReportInfo.SubmitDate}";

                            //Create receipt file
                            var receiptFileResult = StatLogCostReportHistory.CostReportHistoryBL.GenerateAndUploadFileReceipt(RetrieveSession.Classification, parentEventID, ARUMProviderID, fileName, receiptString,
                                RetrieveSession.ARUMUser.UserID, RetrieveSession.IsMSLCEmployee);

                            if (receiptFileResult == null || receiptFileResult.File == null)
                            {
                                throw new Exception($"Error uploading receipt to ARUM: Null was returned");
                            }
                            if (!receiptFileResult.Result.Successful)
                            {
                                throw new Exception($"Error uploading receipt to ARUM: {receiptFileResult.Result.Message}");
                            }

                            IFile file = receiptFileResult.File;

                            IFileType applicationFileType = _fileServiceProxy.GetApplicationFileTypesByApplicationID(myUser.UserID, "StatLog/SaveFile")?
                                .FileTypes?.FirstOrDefault(aft => aft.ApplicationFileTypeID == file.ApplicationFileTypeID);

                            if (applicationFileType == null || applicationFileType.ApplicationFileTypeID < 1)
                                throw new Exception("Error: Issue Retrieving File Type.");

                            if (applicationFileType.ParentFileTypeID == null)
                                throw new Exception("Error: Issue Retrieving File Type Parent.");

                            //Create upload history for receipt and tie to submission event
                            HistoryDTO uploadHistory = StatLogBL.InsertHistory(_classification, statLogID,
                                uploadEventID, parentHistory.ID,
                                receiptString, false, myUser.Email, file.FileID, DateTime.Now, null,
                                null, null, myUser.UserID, "CostReportController", "SubmitCostReport", provider.ARUMProviderID);

                            string emailResult = processLifeCycle.Upload(parentHistory, parentEventDO, myUser, RetrieveSession.IsMSLCEmployee, statLog, provider,
                                applicationFileType.ParentFileTypeName, (int)applicationFileType.ParentFileTypeID, uploadHistory.ID, file.FileID, file.FileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                    Log.Error(ex, exMessage);
                }

                #endregion

                RateBL.SaveCostReportInfo(SimplifiedType, providerCostReportId, statusID, null, null, statLogID, selection.CostReportTypeId);

                RateBL.SaveCostReportStatlog(new ProjectSpecific.Data.Models.ratesetting.ProviderCostReportStatlog
                {
                    CRID = providerCostReportId,
                    EventDate = DateTime.Now,
                    Stamp = DateTime.Now,
                    EventName = $"Status was set to {statusName} by [SYSTEM] on Date(" + DateTime.Now.ToString("MM / dd / yyyy") + ") by submitting cost report",
                    UserId = $"{myUser.FirstName} {myUser.LastName}",
                    CreatedOn = DateTime.Now,
                    CreatedBy = $"{myUser.FirstName} {myUser.LastName}",
                    LastUpdatedOn = DateTime.Now,
                    LastUpdateBy = $"{myUser.FirstName} {myUser.LastName}"
                });

                string key = string.Empty;
                if (s != null)
                {
                    key = Guid.NewGuid().ToString();
                    string sessionKey = $"{myUser.LastName}_{myUser.FirstName}:{key}";
                    Session[sessionKey] = s;
                }

                if (Session != null)
                {
                    selection.VersionName = version;
                    Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] = selection;
                }

                // update the cached model.
                selection.CanEditCostReport = false;

                #region When the 4.90 cost report
                string eventName = string.Empty;
                switch (selection.CostReportTypeId)
                {
                    case "4.90":
                        eventName = "Initial Audit is Submitted";
                        break;
                    case "4.91":
                        eventName = "Revised Audit is Submitted";
                        break;
                    case "4.92":
                        eventName = "Final Audit is Submitted";
                        break;
                }
                if (!string.IsNullOrEmpty(eventName))
                {
                    RateBL.SaveCostReportStatlog(new ProviderCostReportStatlog
                    {
                        CRID = providerCostReportId,
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

                return Json(new
                {
                    Success = true,
                    Key = key,
                    DocumentNumber = version,
                    Filename = filename,
                    CostReportMessage = "This cost report version has been Uploaded and marked \"Submitted\"."
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(
                    new { Success = false, Message = "An exception occurred submitting the cost report." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult ToggleLocalCalculations()
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request."
                    }, JsonRequestBehavior.AllowGet);
                }

                bool value = !(bool)(Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] ?? true);

                Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] = value;

                return Json(new
                {
                    Success = true,
                    LocalCalculations = value
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new
                {
                    Success = false,
                    Message = "An exception occurred toggling local calculation flag."
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetCostReportSubmissionStatus(SelectionModel selection)
        {
            // get the internal value for the client ID
            string id = ClientMapping.GetInternalIdFromClientId(selection.ProviderCostReportId, "costreport");
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(
                    new { Success = false, Message = "Invalid request for submission status." }
                    , JsonRequestBehavior.AllowGet);
            }

            int providerCostReportId = 0;
            if (int.TryParse(id, out int i))
            {
                providerCostReportId = i;
            }

            try
            {
                bool result = CostReportHistoryBL.CheckSubmissionStatus(providerCostReportId) != CostReportStatusEnum.Open;
                return Json(
                    new { Success = true, SubmissionStatus = result },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false, Message = "An exception occurred while checking the submission status." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult Expand()
        {
            Session[SESSION_KEYS_OCRT.SELECTION_EXPAND_COLLAPSE] = "expanded";
            return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult Collapse()
        {
            Session[SESSION_KEYS_OCRT.SELECTION_EXPAND_COLLAPSE] = "collapsed";
            return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CrossDocumentToOCRT(string providerCostReportId)
        {
            int i = 0;
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCostReportView(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for OCRT calculation."
                    }, JsonRequestBehavior.AllowGet);
                }

                if (!string.IsNullOrWhiteSpace(providerCostReportId))
                {
                    string internalProviderCostReportId = ClientMapping.GetInternalIdFromClientId(providerCostReportId, "costreport");
                    int.TryParse(internalProviderCostReportId, out i);
                }

                if (i == 0)
                {
                    int.TryParse(selection.ProviderCostReportId, out i);
                }

                if (i > 0)
                {
                    CostReportBL.CopyCrossDocumentTbToOcrt(i);
                }

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
                    Success = false
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult CrossDocumentToTB(string providerCostReportId)
        {
            int i = 0;
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCostReportView(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for OCRT calculation."
                    }, JsonRequestBehavior.AllowGet);
                }
                if (!string.IsNullOrWhiteSpace(providerCostReportId))
                {
                    string internalProviderCostReportId = ClientMapping.GetInternalIdFromClientId(providerCostReportId, "costreport");
                    int.TryParse(internalProviderCostReportId, out i);
                }

                if (i == 0)
                {
                    int.TryParse(selection.ProviderCostReportId, out i);
                }

                if (i > 0)
                {
                    CostReportBL.CopyCrossDocumentOcrtToTb(i);
                }

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
                    Success = false
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetCorpOfficeCeilingPositionNumber()
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                var year = selection.PeriodCode.Substring(2);
                int.TryParse(year, out int currentYear_FromPeriodCode);
                currentYear_FromPeriodCode = (currentYear_FromPeriodCode >= 2023) ? 2023 : currentYear_FromPeriodCode;
                string dd = string.Empty;
                if (Session[SESSION_KEYS_OCRT.CHART_OF_ACCTS_DROP_DOWN] != null)
                {
                    Dictionary<int, string> dict_COA = (Dictionary<int, string>)Session[SESSION_KEYS_OCRT.CHART_OF_ACCTS_DROP_DOWN];

                    dd = dict_COA[currentYear_FromPeriodCode];
                    if (!dd.IsNullOrWhiteSpace())
                    {
                        return Content(dd);
                    }
                }

                List<string> positions = null;

                positions = ChartOfAccountSeed_OH.LoadChartOfAccounts()
                   .Where(x => x.ExpenseFlag
                  && (!string.IsNullOrWhiteSpace(x.PositionCode) && x.PositionCode.Substring(0, 3) == "WH0")
                  && (x.FacilityType == "A" || x.FacilityType == "I")
                  && x.YearBegin <= currentYear_FromPeriodCode && x.YearEnd >= currentYear_FromPeriodCode)
                  .Select(g => g.PositionCode)
                  .Distinct()
                  .ToList();
                positions.AddRange(ScheduleBL.GetChartOfCorporatePositions(new DateTime(currentYear_FromPeriodCode, 1, 1)).Select(x => x.PositionNum));

                List<string> list = new List<string>{
                 ""
                  };
                list.AddRange(positions.OrderBy(x => x));

                dd = "<select>";

                foreach (string position in list)
                {
                    dd += $"<option value ='{position}'> {position} </option>";
                }

                dd += "</select>";

                Dictionary<int, string> dict = new Dictionary<int, string>
                {
                    { currentYear_FromPeriodCode, dd }
                };
                Session[SESSION_KEYS_OCRT.CHART_OF_ACCTS_DROP_DOWN] = null;
                Session[SESSION_KEYS_OCRT.CHART_OF_ACCTS_DROP_DOWN] = dict;
                return Content(dd);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false, Message = "An error occurred while retrieving the results." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        //TODO: Determine permissions for this section
        public ActionResult GetStandardNarrativesForSearch()
        {
            try
            {
                return Json(new
                {
                    Data = new StandardNarrativeSearchViewModel(CostReportAuditBL.GetNarrativeTableRecordsByNarrativeTypes("S")
                                .Select(x => new StandardNarrativeSearchViewItem(x))
                                .OrderBy(x => int.Parse(x.NarrativeNumber)).ToList()),
                    Success = true
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return Json(new
                {
                    Message = "Failed to retrieve standard narratives",
                    Success = false
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}