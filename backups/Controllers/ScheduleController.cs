using CsvHelper;
using CsvHelper.Configuration;
using Ganss.Xss;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.ProviderProxy;
using MSLC.StatLog.Controllers;
using Newtonsoft.Json;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using WebPortal_OH_FI.BL;
using WebPortal_OH_FI.BL.CostReport;
using WebPortal_OH_FI.BL.CostReportEdits;
using WebPortal_OH_FI.BL.MappedObjects;
using WebPortal_OH_FI.Common;
using WebPortal_OH_FI.Common.Extensions;
using WebPortal_OH_FI.Models;
using WebPortal_OH_FI.Models.CostReport;
using WebPortal_OH_FI.Models.ViewModels;
using WebPortal_OH_FI.ProjectSpecific.Data;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.crdata;
using WebPortal_OH_FI.ProjectSpecific.Data.Seed;

namespace WebPortal_OH_FI.Controllers
{
    public class ScheduleController : LoggedInBaseController
    {
        #region Schedule Properties
        private string ScheduleLastVisited
        {
            get { return (string)Session[SESSION_KEYS_OCRT.SCHED_LAST] ?? string.Empty; }
            set { Session[SESSION_KEYS_OCRT.SCHED_LAST] = value; }
        }

        private int SchedulePageCounter
        {
            get
            {
                string s = (Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNTER] ?? string.Empty).ToString();
                if (string.IsNullOrWhiteSpace(s)) return 0;
                if (int.TryParse(s, out int test))
                    return test;
                else
                    return 0;
            }
            set { 
                Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNTER] = value;
                Session[SESSION_KEYS_OCRT.GRID_DEFINITION] = null;
            }
        }

        private int SchedulePageCountFirst
        {
            get
            {
                string s = (Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_FIRST] ?? string.Empty).ToString();
                if (string.IsNullOrWhiteSpace(s)) return 1;
                if (int.TryParse(s, out int test))
                    return test;
                else
                    return 1;
            }
            set { 
                Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_FIRST] = value;
                Session[SESSION_KEYS_OCRT.GRID_DEFINITION] = null;
            }
        }

        private int SchedulePageCountLast
        {
            get
            {
                string s = (Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_LAST] ?? string.Empty).ToString();
                if (string.IsNullOrWhiteSpace(s)) return 26;
                if (int.TryParse(s, out int test))
                    return test;
                else
                    return 26;
            }
            set { 
                Session[SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_LAST] = value;
                Session[SESSION_KEYS_OCRT.GRID_DEFINITION] = null;
            }
        }

        private string SchedulePageMask
        {
            get { return (string)Session[SESSION_KEYS_OCRT.SCHEDULE_GRID_MASK]; }
            set { 
                Session[SESSION_KEYS_OCRT.SCHEDULE_GRID_MASK] = value;
                Session[SESSION_KEYS_OCRT.GRID_DEFINITION] = null;
            }
        }
        #endregion

        private readonly ARUMProviderProxy _providerServiceProxy;
        private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<ScheduleController>();

        private static string SimplifiedType =>
            Common.Extensions.ClassificationDTOExtensions.GetSimplifiedType(RetrieveSession.Classification);
        public ScheduleController()
        {
            _providerServiceProxy = MSLC.Portal.Utility.Proxies.ARUMProvider.GetARUMProviderServiceProxy(RetrieveSession.Classification);
        }

        #region Schedule Grid Helpers
        public string GetGridHeaderDef(GridDef gridDefinition)
        {
            if (gridDefinition == null || gridDefinition.ColumnTitles == null || gridDefinition.ColumnTitles.Count == 0 || !gridDefinition.ColumnTitles.Any(i => i.Layer > 1))
            {
                return string.Empty;
            }

            IEnumerable<GridDef.GridColumnTitle> headers = gridDefinition.ColumnTitles.Where(i => i.Layer > 1);

            var cols = headers
                .OrderBy(h => h.DisplayColumn)
                .ThenBy(h => h.Layer)
                .GroupBy(g => g.DisplayColumn)
                .Select(g => g.First())
                .Select(c => new { c.DisplayColumn, c.Layer, Reference = c.Name });

            string s = "{ tr: [" + headers
                .OrderByDescending(header => header.Layer)
                .ThenBy(header => header.DisplayColumn)
                .GroupBy(group => group.Layer)
                .Select(group => group.First())
                .Select(row => "{ td: ["
                    + headers
                        .OrderBy(header => header.DisplayColumn)
                        .Where(header => header.Layer == row.Layer)
                        .Select(cell => new { cell.Title, cell.ColSpan, Col = cols.FirstOrDefault(col => col.DisplayColumn == cell.DisplayColumn && col.Layer == cell.Layer) })
                        .Select(cell => new { cell.Title, cell.ColSpan, Reference = cell.Col != null ? cell.Col.Reference : "" })
                        .Select(cell => $"{{text: \"{cell.Title}\", span: \"{cell.ColSpan}\", reference: \"{cell.Reference}\"}}")
                        .Aggregate((concat, cell) => concat + cell)
                        .Replace("}{", "}, {")
                    + "]}")
                .Aggregate((concat, row) => concat + row)
                .Replace("}{", "}, {") + "]}";

            return s;
        }

        public string GetGridNamedDef(GridDef gridDefinition)
        {
            if (gridDefinition == null || gridDefinition.ColumnTitles == null || gridDefinition.ColumnTitles.Count == 0)
            {
                return "[]";
            }

            string s = gridDefinition.ColumnTitles
                .Where(gd => gd.Layer == 1)
                .Select(gd => $"'{HttpUtility.HtmlEncode(gd.Title)}'")
                .Aggregate((concat, title) => concat + $", {title}");

            return $"[{s}]";
        }

        public string GetGridModelDef(GridDef gridDefinition, string worksheetName, bool canEdit = true)
        {
            if (gridDefinition == null || gridDefinition.ColumnDefinitions == null || gridDefinition.ColumnDefinitions.Count == 0)
            {
                return "[]";
            }

            List<string> list = new List<string>();

            #region Cached values
            string chartOfAccountDropdown = string.Empty;
            #endregion Cached values

            IEnumerable<GridDef.GridColumnDefinition> sortedColumns = gridDefinition.ColumnDefinitions.OrderBy(x => x.ID);
			SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
            
            var isFourNineCRT = selection.CostReportType.StartsWith("4.9");

            foreach (GridDef.GridColumnDefinition x in sortedColumns)
            {
                // 4.9x Cost Reports have certain schedules as editable, all others are locked down
                bool isEditable = false;
                if (isFourNineCRT)
                    isEditable = (canEdit && !x.Editable.HasValue && IsEditableSchedule(worksheetName));
                else
                    isEditable = canEdit && (!x.Editable.HasValue || x.Editable.Value);
              
                bool isSortable = x.Sortable.HasValue && x.Sortable.Value;
                bool isFrozen = x.Frozen.HasValue && x.Frozen.Value;
                bool isBoolean = x.DataType.HasValue && x.DataType.Value == 7;
                bool isNumeric = x.DataType.HasValue &&
                    (
                        x.DataType.Value == 2 ||
                        x.DataType.Value == 3 ||
                        x.DataType.Value == 9 ||
                        x.DataType.Value == 10 ||
                        x.DataType.Value == 16
                    );
                bool hasDecimal = x.Precision.HasValue && x.Precision.Value > 0;

                List<string> s = new List<string>();
                s.Add($"{{name:'{x.Name}'");
                s.Add($"index:'{x.Index}'");
                s.Add($"width:{x.Width}");
                s.Add($"sortable:{(isSortable ? "true" : "false")}");
                s.Add($"hidden:{(x.Hidden ? "true" : "false")}");
                s.Add($"align:'{x.Align}'");
                s.Add($"frozen:{(isFrozen ? "true" : "false")}");
                s.Add($"resizable:false");
                s.Add($"editable:{(isEditable ? "true" : "false")}");
                // https://stackoverflow.com/questions/38495763/how-to-make-drop-down-inside-a-jqgrid)
                s.Add($"edittype: '{(x.EditOptions.Contains("value") || (x.EditOptions.Contains("dataUrl")) ? "select" : "text")}'");
                if (!string.IsNullOrWhiteSpace(x.FormatterName)) s.Add($"formatter:{x.FormatterName}");
                if (!string.IsNullOrWhiteSpace(x.UnFormatterName)) s.Add($"unformat:{x.UnFormatterName}");
                if (!string.IsNullOrWhiteSpace(x.EditRules)) s.Add($"editrules:{{custom:true,custom_func:{x.EditRules}}}");

                #region Add editOptions
                List<string> options = new List<string>();
                if (x.FieldLength.HasValue || x.DataType.HasValue) options.Add("autocomplete: 'off'");
                if (x.DataType.HasValue) options.Add("datatype:" + x.DataType.Value);
                //+ (x.FieldLength.HasValue ? $"|maxlength:{x.FieldLength.Value + (x.HasDecimal ? 1 : 0)}|" : "")
                if (x.FieldLength.HasValue) options.Add("maxlength:" + x.FieldLength.Value);
                if (isNumeric) options.Add("precision:" + x.Precision.Value);
                if (!string.IsNullOrEmpty(x.EditOptions))
                {
                    if (x.EditOptions.Contains("GetCorpOfficeCeilingPositionNumber"))
                    {
                        if (string.IsNullOrWhiteSpace(chartOfAccountDropdown)) chartOfAccountDropdown = GetPositionNumberValues();
                        options.Add(chartOfAccountDropdown);
                    }
                    else options.Add(x.EditOptions);
                }
                // This should be precise to the length/precision: 3.2f = 999.99, 8.1f = 99999999.9, etc.
                if (hasDecimal) options.Add($"max:{"9".Repeat(x.FieldLength.Value - x.Precision.Value)}.{"9".Repeat(x.Precision.Value)}");
                else if (isNumeric) options.Add($"max:{"9".Repeat(x.FieldLength.Value)}");

                s.Add($"editoptions:{{{string.Join(",", options)}}}");
                #endregion Add editOptions

                list.Add($"{string.Join(",", s)}}}");
            }

            return $"[{string.Join(", ", list)}]";
        }

        public string GetGridCellDef(GridDef gridDef, GridData gridData)
        {
            // Ensure that any drop downs match up in length so that values will not be lost
            // in the UI during editing - the "search" for the value will not match up "6060"
            // and "6060  " as the same value, for example.
            //List<GridDef.GridColumnDefinition> sizeColumns = gridDef.ColumnDefinitions.Where(x => x.EditOptions.Contains("value: {")).ToList();
            //foreach (var column in sizeColumns)
            //{
            //    List<GridData.GridStaticData> data = gridData.StaticData.Where(x => x.DisplayColumn == column.DisplayColumn).ToList();

            //    if (!column.FieldLength.HasValue) continue;
            //    int length = column.FieldLength.Value;

            //    foreach (var d in data)
            //    {
            //        if (!string.IsNullOrWhiteSpace(d.Data))
            //        {
            //            if (d.Data.Length < length) d.Data = d.Data.PadRight(length);
            //        }
            //    }
            //}

            IEnumerable<string> sDefinition = gridData.StaticData
                .OrderBy(sd => sd.DisplayLine)
                .ThenBy(sd => sd.DisplayColumn)
                .Select(sd => new
                {
                    cell = sd,
                    color = gridDef.ColorDefinitions
                        .Where(cd => cd.DisplayColumn == sd.DisplayColumn && (cd.DisplayLine == null || cd.DisplayLine == sd.DisplayLine))
                        .OrderByDescending(cd => (cd.DisplayLine == sd.DisplayLine && cd.EditSeverity.HasValue) ? cd.EditSeverity.Value : 0)
                        .ThenBy(cd => (cd.DisplayLine == sd.DisplayLine && !(cd.IsDefault ?? false)) ? 0 : 1)
                        .FirstOrDefault()
                })
                .Where(c => c.color != null)
                .Select(c => new
                {
                    name = c.color.Name,
                    displayLine = c.cell.DisplayLine,
                    foreColor = c.color.ForeColor,
                    backColor = c.color.BackColor,
                    canEdit = (!c.cell.TrialBalExists && !c.cell.IsNonEditable && !string.IsNullOrWhiteSpace(c.cell.RangeName)
                        && int.TryParse(c.cell.RangeName.Substring(0, 1), out int i)) ? "true" : "false",
                    align = c.color.Align,
                    isBold = c.color.IsBold.HasValue && c.color.IsBold.Value ? "true" : "false",
                    isItalic = c.color.IsItalic.HasValue && c.color.IsItalic.Value ? "true" : "false",
                    rowSpan = c.color.RowSpan,
                    colSpan = c.color.ColSpan,
                    rangeName = c.cell.RangeName,
                    toolTip = HttpUtility.JavaScriptStringEncode(c.color.ToolTip)
                })
                .Select(d => $"['{d.name}','{d.displayLine}','{d.foreColor}','{d.backColor}',{d.canEdit},'{d.align}',{d.isBold},{d.isItalic},{d.rowSpan},{d.colSpan},'{d.rangeName}','{d.toolTip}']");

            string sCombinedDef = string.Join(",", sDefinition.ToArray());

            return $"[{sCombinedDef}]";
        }
        #endregion

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult Index()
        {
            try
            {
                string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new ArgumentException("There is no selected tab.");
                }

                string[] token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new ArgumentException("There is no selected tab.");
                }

                string main = token[0];
                string worksheet = token[1];

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                Tab tab = ScheduleBL.GetScheduleList(providerCostReportId, main, worksheet)
                    .FirstOrDefault();
                if (tab == null)
                {
                    tab = new Tab
                    {
                        Style = "paged:false"
                    };
                }

                ScheduleForm scheduleGridStyles = ScheduleBL.GetScheduleForm(providerCostReportId, worksheet);
                string instruction = "";
                if (worksheet == "edits")
                {
                    //TODO Needs to get Edits instruction message from database
                    instruction = "The following required(missing)/errors/warnings have been found in the current cost report. Before the cost report file can be created in order to submit it, the missing(required) fields must be completed and the errors must be resolved.";
                }
                else
                {
                    instruction = scheduleGridStyles.Instructions;
                }

                object value = Session[SESSION_KEYS_OCRT.SCHED_EXPAND_COLLAPSE];

                #region Paged Schedule Considerations
                // Handle paging considerations - check if this is a paged schedule being requested
                if (tab.Style.Contains("paged:true"))
                {
                    // We are serving a paged schedule - is it the same one they were just on?
                    if (ScheduleLastVisited != worksheet)
                    {
                        // it is not, so we need to reset to page 1
                        SchedulePageCounter = 1;

                        // reset the page mask to first page
                        SchedulePageMask = SchedulePagedSupport.GetSchedulePageMaskCharFromCounter(SchedulePageCounter, SchedulePageCountFirst, SchedulePageCountLast);
                    }
                    // NOTE: if it is the same, we leave the page counter and page mask.
                }
                else
                {
                    // We are NOT serving a paged schedule - clear out all paged info.
                    SchedulePageCounter = 0;
                    SchedulePageMask = null;
                }

                // New page or reloading; clear cached grid definition (and edit highlighting).
                Session[SESSION_KEYS_OCRT.GRID_DEFINITION] = null;

                // update last schedule visited to this one.
                ScheduleLastVisited = worksheet;
                #endregion

                // 4.9x Cost Reports have certain schedules as editable, all others are locked down
                bool isEditable = false;
                bool isAduitAdjustmentEditable = false;
                bool isAuditCR = selection.CostReportType.StartsWith("4.9");
                if (isAuditCR)
                {
                    if (selection.CanEditCostReport)
                    {
                        isEditable = IsEditableSchedule(worksheet);
                        isAduitAdjustmentEditable = true;
                    }
                    else
                        isEditable = selection.CanEditCostReport;
                }
                else
                    isEditable = selection.CanEditCostReport;

                    ScheduleViewModel model = new ScheduleViewModel
                    {
                        Schedule = worksheet,
                        Title = scheduleGridStyles.Title,
                        Instructions = instruction,
                        GridDefinitionURL = Url.AbsoluteAction("GetGridDef", "Schedule"),
                        GridDataURL = Url.AbsoluteAction("GetData", "Schedule"),
                        GridUpdateURL = Url.AbsoluteAction("Update", "Schedule"),
                        UpdatePageCounterURL = Url.AbsoluteAction("UpdatePageCounter", "Schedule"),
                        DeletePageURL = Url.AbsoluteAction("DeleteC1Page", "Schedule"),
                        GotoPageURL = Url.AbsoluteAction("GotoC1Page", "Schedule"),
                        SummaryGridDiv = @"",
                        SummaryGridCreateFunction = @"",
                        SummaryGridCreateFunctionCall = @"",
                        SummaryGridRefreshCall = @"",
                        CostReportIsSubmitted = false,
                        CanEditCostReport = isEditable,
                        Style = tab.Style,
                        Collapsed = value != null && value.ToString() == "collapsed",
                        SchedulePageCounter = SchedulePageCounter,
                        GridLineControlURL = Url.AbsoluteAction("GridLineControl", "Schedule"),
                        CanEditAuditAdjustment = isAduitAdjustmentEditable,
                        IsAuditCostReport = isAuditCR
                    };

                if (providerCostReportId > 0)
                {
                    model.CostReportIsSubmitted = CostReportHistoryBL.CheckSubmissionStatus(providerCostReportId) != CostReportStatusEnum.Open;
                    model.LockedOn = CostReportHistoryBL.CheckCostReportLocked(providerCostReportId);

                    if (worksheet == "TrialBal")
                    {
                        // div to contain the summary grid
                        model.SummaryGridDiv = @"<div id='GridSummarySeparator'><br /></div>" +
                            @"<div id='GridSummaryContainer'><table id='GridSummary'></table></div>";

                        // to hide all grid headers instead
                        //@"$('.ui-jqgrid-hdiv').hide();" +

                        // create function to create/configure the summary grid
                        model.SummaryGridCreateFunction = @"
function createSummaryGrid() {
    $('#GridSummary').jqGrid({
        url: '" + Url.AbsoluteAction("GetSummaryData", "Schedule") + @"',
        datatype: 'json',
        colModel: [
            { name: 'sc1', label: 'sc1', width: 250 },
            { name: 'sc2', label: 'sc2', width: 100, align: 'right' },
            { name: 'sc3', label: 'sc3', width: 250 },
            { name: 'sc4', label: 'sc4', width: 100, align: 'right' },
        ],
        idPrefix: 'sg_',
        rownumbers: false,
        gridComplete: function() {
            $('#GridSummary').closest('div.ui-jqgrid-view').children('div.ui-jqgrid-hdiv').hide();
            $('#GridSummary').jqGrid('setCell', 'sg_1', 'sc1', '', { 'font-weight': 'bold'});
            $('#GridSummary').jqGrid('setCell', 'sg_1', 'sc2', '', { 'font-weight': 'bold'});
            $('#GridSummary').jqGrid('setCell', 'sg_1', 'sc3', '', { 'font-weight': 'bold'});
            $('#GridSummary').jqGrid('setCell', 'sg_1', 'sc4', '', { 'font-weight': 'bold'});
        }
    });}";

                        // call to function to create/configure summary grid
                        model.SummaryGridCreateFunctionCall = @"createSummaryGrid();";

                        // call to refresh the summary grid (done after a cell edit)
                        model.SummaryGridRefreshCall = @"
afterSaveCell: function (rowid, cellname, value, iRow, iCol) {
    if (!(window.useLocalCalculations ?? true)) $('#GridSummary').trigger('reloadGrid');
},";
                    }

                    if (!string.IsNullOrWhiteSpace(selection?.PeriodCode ?? string.Empty))
                    {
                        //Get period code based on cost report document template
                        string periodCode = ScheduleBL.GetCostReportPeriodCode(providerCostReportId, selection.PeriodCode, worksheet);
                        if (worksheet == "TrialBal") model.InRuleEngineURL = ARUMConfigurationOHFI.InRuleEngineTbFileName(RetrieveSession.Classification, periodCode);
                        else model.InRuleEngineURL = ARUMConfigurationOHFI.InRuleEngineFileName(RetrieveSession.Classification, periodCode);
                    }
                }

                return PartialView(model);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetSummaryData()
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
                if (myUser == null || !myUser.Roles.Any())
                {
                    throw new ArgumentException(@"Invalid request.");
                }

                string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new ArgumentException("Invalid request.");
                }

                string[] token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new ArgumentException("Invalid request.");
                }

                string main = token[0];
                string worksheetName = token[1];

                if (main != "schedule")
                {
                    throw new ArgumentException("Invalid request.");
                }

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                // build the data result
                if (worksheetName == "TrialBal")
                {
                    List<GridValue> val1 = ScheduleBL.ReadValues(providerCostReportId, "TrialBalSummary");

                    string tbTotal = "0";
                    string tbTotalAssets = "0";
                    string tbTotalRevenues = "0";
                    string tbTotalLiabilities = "0";
                    string tbTrialExpenses = "0";
                    string tbTotalEquity = "0";
                    string tbNetIncomeLoss = "0";
                    string tbTotalLiabilitiesAndEquity = "0";

                    if (val1.Count != 0)
                    {
                        GridValue val = val1.FirstOrDefault(p => p.RangeName == "ITBTOT");
                        if (val != null)
                            tbTotal = val.Value;

                        val = val1.FirstOrDefault(p => p.RangeName == "ITBASS");
                        if (val != null)
                            tbTotalAssets = val.Value;

                        val = val1.FirstOrDefault(p => p.RangeName == "ITBREV");
                        if (val != null)
                            tbTotalRevenues = val.Value;

                        val = val1.FirstOrDefault(p => p.RangeName == "ITBLIA");
                        if (val != null)
                            tbTotalLiabilities = val.Value;

                        val = val1.FirstOrDefault(p => p.RangeName == "ITBEXP");
                        if (val != null)
                            tbTrialExpenses = val.Value;

                        val = val1.FirstOrDefault(p => p.RangeName == "ITBEQU");
                        if (val != null)
                            tbTotalEquity = val.Value;

                        val = val1.FirstOrDefault(p => p.RangeName == "ITBINC");
                        if (val != null)
                            tbNetIncomeLoss = val.Value;

                        val = val1.FirstOrDefault(p => p.RangeName == "ITBSUB");
                        if (val != null)
                            tbTotalLiabilitiesAndEquity = val.Value;
                    }

                    //TODO: Need to implement the actual calculation of the numbers

                    List<Models.jqGrid.RowModel> rows = new List<Models.jqGrid.RowModel>()
                    {
                        new  Models.jqGrid.RowModel() { id = 1, cell = new Dictionary<string, string>() { { "ID", "1" }, { "sc1", "Trial Balance Total" }, { "sc2", tbTotal.ToString() }, { "sc3", "Total Assets" }, { "sc4", tbTotalAssets.ToString() } } },
                        new  Models.jqGrid.RowModel() { id = 2, cell = new Dictionary<string, string>() { { "ID", "2" }, { "sc1", "Total Revenues" }, { "sc2", tbTotalRevenues.ToString() }, { "sc3", "Total Liabilities" }, { "sc4", tbTotalLiabilities.ToString() } } },
                        new  Models.jqGrid.RowModel() { id = 3, cell = new Dictionary<string, string>() { { "ID", "3" }, { "sc1", "Trial Expenses" }, { "sc2", tbTrialExpenses.ToString() }, { "sc3", "Total Equity" }, { "sc4", tbTotalEquity.ToString() } } },
                        new  Models.jqGrid.RowModel() { id = 4, cell = new Dictionary<string, string>() { { "ID", "4" }, { "sc1", "Net Income (Loss)" }, { "sc2", tbNetIncomeLoss.ToString() }, { "sc3", "Total Liabilities and Equity" }, { "sc4", tbTotalLiabilitiesAndEquity.ToString() } } }
                    };

                    Models.jqGrid.ResultModel results = new Models.jqGrid.ResultModel()
                    {
                        page = 1,
                        total = 1,
                        records = 4,
                        rows = rows.ToArray()
                    };

                    val1.Clear();
                    return Json(results, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(@"{}", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Invalid Request"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult UpdatePageCounter(int counter, int first = 1, int last = 26)
        {
            try
            {
                // validate user input
                counter = SchedulePagedSupport.ConfirmValueSchedulePageCounter(counter, first, last);

                SchedulePageCounter = counter;
                SchedulePageCountFirst = first;
                SchedulePageCountLast = last;

                // set valid page mask text
                SchedulePageMask = SchedulePagedSupport.GetSchedulePageMaskCharFromCounter(counter, first, last);

                // return the validated counter value to the client to show if it was accepted or not.
                return Json(counter, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(-1, JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult DeleteC1Page(int counter, int first = 1, int last = 26)
        {
            int providerCostReportId = -1;

            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                string worksheetName = GetCurrentWorksheetName();

                // validate user input
                counter = SchedulePagedSupport.ConfirmValueSchedulePageCounter(counter, first, last);

                SchedulePageCounter = counter;
                SchedulePageCountFirst = first;
                SchedulePageCountLast = last;

                // set valid page mask text
                SchedulePageMask = SchedulePagedSupport.GetSchedulePageMaskCharFromCounter(counter, first, last);

                counter = ScheduleBL.DeletePageAndReorderSchedulePageMask(providerCostReportId, worksheetName, SchedulePageMask, counter);
                // Set the flag and clear current edits to force recalculation.
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "DeleteC1Page";
                Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] = null;

                // return the validated counter value to the client to show if it was accepted or not.
                return Json(new { Success = true, Data = counter },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false, Message = "Invalid Request." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GotoC1Page()
        {
            int providerCostReportId = -1;

            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                string worksheetName = GetCurrentWorksheetName();

                List<C1SubPagesPopup> c1SubPagesList = ScheduleBL.GetScheduleC1SubPages(providerCostReportId, worksheetName);

                // return the validated counter value to the client to show if it was accepted or not.
                return Json(new { Success = true, Data = c1SubPagesList },
                   JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false, Message = "Invalid Request." },
                    JsonRequestBehavior.AllowGet);
            }
        }


        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetData(bool skipEdits = false)
        {
            try
            {
                string worksheetName = GetCurrentWorksheetName();

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                GridDef gridDefinition = GetGridDefinition(providerCostReportId, selection.PeriodCode, worksheetName, skipEdits);

                // build the data result
                Models.jqGrid.ResultModel results = new Models.jqGrid.ResultModel();
                List<Models.jqGrid.RowModel> rows = new List<Models.jqGrid.RowModel>();

                Dictionary<int, string> dataRow = new Dictionary<int, string>();
                bool trimDataBaseValue = false;
                string gridCellDef = string.Empty;

                // get the grid data
                GridData gridData = ScheduleBL.GetGridData(providerCostReportId, worksheetName, SchedulePageMask, trimDataBaseValue);

                gridCellDef = GetGridCellDef(gridDefinition, gridData);

                // build the data row
                dataRow.Clear();
                foreach (int val in gridDefinition.ColumnMap.Keys)
                {
                    dataRow.Add(val, @"");
                }

                int currentDisplayLine = 0;
                bool hasRow = false;
                foreach (GridData.GridStaticData gsd in gridData.StaticData)
                {
                    if (currentDisplayLine == 0)
                    {
                        currentDisplayLine = gsd.DisplayLine;
                    }

                    if (currentDisplayLine != gsd.DisplayLine)
                    {
                        // add the row to the jqgrid results
                        rows.Add(
                            new Models.jqGrid.RowModel
                            {
                                id = currentDisplayLine,
                                cell = dataRow.Keys.ToDictionary(k => gridDefinition.ColumnMap[k], k => dataRow[k])
                            });

                        // reset for the next displayline
                        dataRow = gridDefinition.ColumnMap.Keys.ToDictionary(k => k, k => @"");
                        hasRow = false;
                        currentDisplayLine = gsd.DisplayLine;
                    }

                    // We need to ensure that DisplayColumn "<1" values are not sent to the client.
                    // Those values are an internal holder for default values.
                    if (currentDisplayLine == gsd.DisplayLine && gsd.DisplayColumn > 0)
                    {
                        hasRow = true;
                        //NOTE: sending as JSON and client grid already handle HTML escaping, etc.
                        dataRow[gsd.DisplayColumn] = string.IsNullOrEmpty(gsd.RangeName) ? gsd.FieldName : gsd.Data;
                    }
                }

                // pick up last line of data (if any)
                if (hasRow && gridDefinition.ColumnMap.Count > 0)
                {
                    rows.Add(
                        new Models.jqGrid.RowModel
                        {
                            id = currentDisplayLine,
                            cell = dataRow.Keys.ToDictionary(k => gridDefinition.ColumnMap[k], k => dataRow[k])
                        });
                }

                results.page = 1;
                results.total = 1;
                results.records = rows.Count;
                results.rows = rows.ToArray();
                results.GridCellDefArray = gridCellDef;

                //gd.StaticData.Clear();
                //gf.ColorDefinitions.Clear();
                //gf.ColumnDefinitions.Clear();
                //gf.ColumnMap.Clear();
                //gf.ColumnTitles.Clear();
                //rows.Clear();

                return Json(results, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Invalid Request"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetGridDef(bool skipEdits = false)
        {
            try
            {
                string worksheetName = GetCurrentWorksheetName();

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                GridDef gridDefinition = GetGridDefinition(providerCostReportId, selection.PeriodCode, worksheetName, skipEdits);

				Models.jqGrid.DefinitionModel gridDef = new Models.jqGrid.DefinitionModel
                {
                    GridTitle = gridDefinition.Title,
                    GridInstructions = gridDefinition.Instructions,
                    GridModelDef = GetGridModelDef(gridDefinition, worksheetName, selection.CanEditCostReport),
                    GridCellDef = string.Empty,
                    GridNameDef = GetGridNamedDef(gridDefinition),
                    GridHeaderDef = GetGridHeaderDef(gridDefinition)
                };

				Session[SESSION_KEYS_OCRT.CHART_OF_ACCTS_DROP_DOWN] = null;

                return Json(new { Success = true, Grid = gridDef });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        private GridDef GetGridDefinition(int providerCostReportId, string periodCode, string worksheetName, bool skipEdits = false)
        {
            try
            {
                if (Session[SESSION_KEYS_OCRT.GRID_DEFINITION] != null)
                {
                    return (GridDef)Session[SESSION_KEYS_OCRT.GRID_DEFINITION];
                }

                if (string.IsNullOrWhiteSpace(worksheetName))
                {
                    string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                    if (string.IsNullOrWhiteSpace(selectedTab))
                    {
                        throw new ArgumentException("Invalid request.");
                    }

                    string[] token = selectedTab.Split('|');
                    if (token.Length != 3)
                    {
                        throw new ArgumentException("Invalid request.");
                    }

                    worksheetName = token[1];
                }

                // build the grid definition 
                //ClassificationDTO classification = RetrieveSession.Classification;
                RateSettingPackage.ProviderPackage providerData = (RateSettingPackage.ProviderPackage)Session[SESSION_KEYS_OCRT.PROVIDER_DATA];
                SchedulePageMask = SchedulePagedSupport.GetSchedulePageMaskCharFromCounter(SchedulePageCounter);

                // Get the cost report edits
                List<EditResult> scheduleEditsResults = new List<EditResult>();
                if (!skipEdits)
                {
                    if (!string.IsNullOrWhiteSpace(Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED]?.ToString() ?? ""))
                    {
                        SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                        scheduleEditsResults = CostReportEditsBL.GetCostReportEdits(SimplifiedType, providerCostReportId, periodCode, providerData, selection.CostReportTypeId);
                        Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = null;

                        // Cache the edits for later data formatting.
                        Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] = scheduleEditsResults;
                    }
                    else if (Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] == null)
                    {
                        scheduleEditsResults = CostReportEditsBL.ReadCostReportEditsList(providerCostReportId);

                        // Cache the edits for later data formatting.
                        if (scheduleEditsResults.Any()) Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] = scheduleEditsResults;
                    }
                    else
                    {
                        scheduleEditsResults = (List<EditResult>)Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT];
                    }
                }

                // Get the definition
                GridDef gridDefinition = ScheduleBL.GetGridDef(SimplifiedType, providerCostReportId, worksheetName, periodCode, providerData, SchedulePageMask);

                // Use the edits for "grid" formatting.
                gridDefinition = ScheduleBL.MergeCostReportEditsWithGrid(gridDefinition, scheduleEditsResults, providerCostReportId, worksheetName, SchedulePageMask);

                // Set the cache
                Session[SESSION_KEYS_OCRT.GRID_DEFINITION] = gridDefinition;

                return gridDefinition;
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GridLineControl(GridLineAction action, int row, int min, int max)
        {
            ProjectSpecificDbContext db = ScheduleBL.GetContextInstance();
            System.Data.Entity.DbContextTransaction dbTransaction = db.Database.BeginTransaction();

            try
            {
                string worksheetName = GetCurrentWorksheetName();

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                string mask = SchedulePagedSupport.GetSchedulePageMaskCharFromCounter(SchedulePageCounter);

                // Use the same ordering of grid data - it is already sorted by row and column.
                List<GridData.GridStaticDataExtended> data = ScheduleBL.GetGridDataFull(providerCostReportId, worksheetName, mask);
                // group the data by display line
                List<IGrouping<int, GridData.GridStaticDataExtended>> groups = data.GroupBy(x => x.DisplayLine).ToList();

                // Setup common items for working with the data.
                List<GridData.GridStaticDataExtended> source = null;
                List<GridData.GridStaticDataExtended> swap = null;
                List<string> list = new List<string>();

                switch (action)
                {
                    case GridLineAction.ShiftUp:
                        if (row <= min)
                        {
                            dbTransaction.Rollback();
                            break;   // don't allow going past minimum
                        }

                        source = groups.FirstOrDefault(x => x.Key == row)?.ToList();
                        swap = groups.FirstOrDefault(x => x.Key == row - 1)?.ToList();

                        if (source == null || swap == null)
                        {
                            dbTransaction.Rollback();
                            break;  // stop if either line doesn't exist
                        }

                        // swap out the two lists and create SQL statements to handle the data move.
                        list = GridLineSwap(source, swap);
                        //If both lines are blank there's nothing to swap
                        if (list.Count == 0)
                        {
                            dbTransaction.Rollback();
                            break;  // stop if either line doesn't exist
                        }

                        db.Database.ExecuteSqlCommand(string.Join("\n", list));
                        dbTransaction.Commit();
                        break;

                    case GridLineAction.ShiftDown:
                        if (row >= max)
                        {
                            dbTransaction.Rollback();
                            break;   // don't allow going past maximum
                        }

                        source = groups.FirstOrDefault(x => x.Key == row)?.ToList();
                        swap = groups.FirstOrDefault(x => x.Key == row + 1)?.ToList();

                        if (source == null || swap == null)
                        {
                            dbTransaction.Rollback();
                            break;  // stop if either line doesn't exist
                        }

                        // swap out the two lists and create SQL statements to handle the data move.
                        list = GridLineSwap(source, swap);
                        //If both lines are blank there's nothing to swap
                        if (list.Count == 0)
                        {
                            dbTransaction.Rollback();
                            break;  // stop if either line doesn't exist
                        }

                        db.Database.ExecuteSqlCommand(string.Join("\n", list));
                        dbTransaction.Commit();
                        break;

                    case GridLineAction.AddRow:
                        if (row >= max)
                        {
                            dbTransaction.Rollback();
                            break;  // don't allow going past maximum
                        }

                        var gridmax = max;

                        // set the data into groups by line number and limit to after the specified row
                        groups = data.GroupBy(x => x.DisplayLine)
                            .Where(x => x.Key >= row && x.Key <= max).ToList();

                        if (groups.Count() < 1)
                        {
                            dbTransaction.Rollback();
                            break;
                        }

                        // prep the list to hold list of values for swapping in data
                        list = new List<string>();
                        source = groups[0].ToList();
                        for (i = 0; i < source.Count; i++) { list.Add(""); }

                        bool blankFlag = false;
                        // find the next blank row and swap until we get there
                        for (i = 0; i < groups.Count(); i++)
                        {
                            source = groups[i].ToList();

                            blankFlag = !source.Any(x => !string.IsNullOrWhiteSpace(x.RangeName) && !string.IsNullOrWhiteSpace(x.Data));

                            // swap the values
                            for (int j = 0; j < source.Count; j++)
                            {
                                GridData.GridStaticDataExtended s = source[j];
                                if (string.IsNullOrWhiteSpace(s.RangeName)) continue;

                                // swap out values
                                (list[j], s.Data) = (s.Data, list[j]);
                            }

                            // if the row we swapped to was blank, we are done
                            if (blankFlag)
                            {
                                max = i;    // use the max value as the last row we need to work with.
                                break;
                            }

                        }

                        // if we only looped once and that row was already blank there is no change
                        if (max == 0)
                        {
                            dbTransaction.Rollback();
                            break;
                        }
                        // if we reached the end of the grid and there's no room to insert a blank row, then we can't change
                        if (blankFlag && max == gridmax)
                        {
                            dbTransaction.Rollback();
                            break;
                        }

                        // clear the list to use for SQL statements.
                        list.Clear();

                        // Attempt to insert and shift data until we fill the next blank row.
                        for (i = 0; i < groups.Count; i++)
                        {
                            source = groups[i].ToList();
                            foreach (GridData.GridStaticDataExtended field in source)
                            {
                                if (string.IsNullOrWhiteSpace(field.RangeName)) continue;
                                string v = GridLineValueSQLforSwap(field.DocumentDataId, field.DocumentId, field.FieldTemplateId, field.Data);
                                if (!string.IsNullOrWhiteSpace(v)) list.Add(v);

                                // check if list needs to be flushed
                                if (list.Count > 20)
                                {
                                    db.Database.ExecuteSqlCommand(String.Join("\n", list));
                                    list.Clear();
                                }
                            }
                        }

                        // clear out any remaining sql statements
                        if (list.Count > 0)
                        {
                            db.Database.ExecuteSqlCommand(String.Join("\n", list));
                            list.Clear();
                        }
                        dbTransaction.Commit();
                        break;

                    case GridLineAction.DeleteRow:
                        // set the data into groups by line number and limit to after the specified row
                        groups = data.GroupBy(x => x.DisplayLine)
                            .Where(x => x.Key >= row && x.Key <= max).ToList();

                        if (groups.Count() < 1) break;

                        // if there is only one row - the row we are on, 
                        // then this edge case should just be deleted.
                        if (groups.Count() == 1)
                        {
                            source = groups[0].ToList();

                            // clear out the data for this row
                            for (int j = 0; j < source.Count; j++)
                            {
                                GridData.GridStaticDataExtended s = source[j];
                                if (string.IsNullOrWhiteSpace(s.RangeName)) continue;
                                s.Data = string.Empty;
                            }

                        }
                        else
                        {
                            // shift data up for all the rows - do not retain the first one
                            // find the next blank row and swap until we get there
                            for (i = 0; i < groups.Count(); i++)
                            {
                                source = groups[i].ToList();
                                if ((i + 1) == groups.Count())
                                {
                                    // clear out the data for this row
                                    for (int j = 0; j < source.Count; j++)
                                    {
                                        GridData.GridStaticDataExtended s = source[j];
                                        if (string.IsNullOrWhiteSpace(s.RangeName)) continue;
                                        s.Data = string.Empty;
                                    }
                                    break;
                                }
                                else
                                {
                                    swap = groups[i + 1].ToList();

                                    // swap the values
                                    for (int j = 0; j < source.Count; j++)
                                    {
                                        GridData.GridStaticDataExtended s = source[j];
                                        if (string.IsNullOrWhiteSpace(s.RangeName)) continue;

                                        GridData.GridStaticDataExtended t = swap[j];

                                        // pull up the value from the next row
                                        s.Data = t.Data;
                                    }
                                }

                            }
                        }

                        list.Clear();

                        // update all the rows so the data is actually shifted up
                        for (i = 0; i < groups.Count(); i++)
                        {
                            source = groups[i].ToList();
                            foreach (GridData.GridStaticDataExtended field in source)
                            {
                                if (string.IsNullOrWhiteSpace(field.RangeName)) continue;
                                string v = GridLineValueSQLforSwap(field.DocumentDataId, field.DocumentId, field.FieldTemplateId, field.Data, true);
                                if (!string.IsNullOrWhiteSpace(v)) list.Add(v);

                                // check if list needs to be flushed
                                if (list.Count > 20)
                                {
                                    db.Database.ExecuteSqlCommand(String.Join("\n", list));
                                    list.Clear();
                                }
                            }
                        }

                        // clear out any remaining sql statements
                        if (list.Count > 0)
                        {
                            db.Database.ExecuteSqlCommand(String.Join("\n", list));
                            list.Clear();
                        }
                        dbTransaction.Commit();

                        //NOTE: deleting a row means deleting values, and those could cause need for recalculation or edit processing.
                        // when deleting a row, trigger that recalculation or edits should be checked.
                        if (!(bool)(Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] ?? true)) Session[SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED] = "Grid";
                        Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "Grid";
                        break;

                    default:
                        dbTransaction.Rollback();
                        throw new Exception("Invalid action");
                }

                return Json(new
                {
                    Success = true
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                dbTransaction.Rollback();

                _logger.Error(e, e.Message);

                return Json(new
                {
                    Success = false
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private List<string> GridLineSwap(List<GridData.GridStaticDataExtended> source,
            List<GridData.GridStaticDataExtended> target, bool allowDelete = false)
        {
            List<string> list = new List<string>();

            foreach (GridData.GridStaticDataExtended s in source)
            {
                // we can't copy any value that isn't really data.
                if (string.IsNullOrWhiteSpace(s.RangeName)) continue;

                // get the value from the other row to swap
                GridData.GridStaticDataExtended t = target.FirstOrDefault(x => x.DisplayColumn == s.DisplayColumn);
                if (t != null)
                {
                    // If there is no data to move, we can skip this field.
                    if (string.IsNullOrWhiteSpace(t.Data) && string.IsNullOrWhiteSpace(s.Data)) continue;

                    // swap the values for this field
                    string v = GridLineValueSQLforSwap(s.DocumentDataId, s.DocumentId, s.FieldTemplateId, t.Data, allowDelete);
                    if (!string.IsNullOrWhiteSpace(v)) list.Add(v);
                    v = GridLineValueSQLforSwap(t.DocumentDataId, t.DocumentId, t.FieldTemplateId, s.Data, allowDelete);
                    if (!string.IsNullOrWhiteSpace(v)) list.Add(v);
                }
            }

            return list;
        }

        private string GridLineValueSQLforSwap(int? documentDataId, int documentId, int fieldTemplateId, string data, bool allowDelete = false)
        {
            if (!documentDataId.HasValue)
            {
                if (allowDelete && string.IsNullOrWhiteSpace(data)) return string.Empty;
                return $"INSERT INTO [crdata].[DocumentData]([DocumentId], [FieldTemplateId], [Data]) SELECT {documentId}, {fieldTemplateId}, '{data.Replace("'", "''")}'";
            }
            else
            {
                if (allowDelete && string.IsNullOrWhiteSpace(data)) return $"DELETE FROM [crdata].[DocumentData] WHERE [DocumentDataId] = {documentDataId.Value}";
                return $"UPDATE [crdata].[DocumentData] SET [Data] = '{data.Replace("'", "''")}' WHERE [DocumentDataId] = {documentDataId.Value}";
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult ReadValues()
        {
            try
            {
                string worksheetName = GetCurrentWorksheetName();

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                // Get the edits cached when the "grid" was created; they were used for grid formatting.
                List<EditResult> editResults = (List<EditResult>)Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT];
                // Clear out the cached items to release data.
                //Session[OCRT_SESSION_KEYS.SCHED_EDITS_CURRENT] = null;

                // We are ready to format the data being returned.
                List<FormEditResults> formEditResults = new List<FormEditResults>();
                if (editResults != null && editResults.Count > 0)
                {
                    foreach (EditResult editResult in editResults)
                    {
                        FormEditResults formEditResult = new FormEditResults();
                        formEditResult.DataReference = !string.IsNullOrWhiteSpace(editResult.DataElementSuffix)
                                                        ? formEditResult.DataReference = editResult.DataElementNumber + editResult.DataElementSuffix
                                                        : formEditResult.DataReference = editResult.DataElementNumber;
                        formEditResult.Message = editResult.Message;
                        formEditResult.Type = editResult.Type.ToString();

                        switch (formEditResult.Type)
                        {
                            case "ERROR":
                            case "REQUIRED":
                                formEditResult.Color = "#ffc0c0";
                                break;
                            case "WARNING":
                            case "Perseus_WARNING":
                                formEditResult.Color = "#B0E0E6";
                                break;
                            case "Perseus_CRITICAL":
                            case "Perseus_ERROR":
                                formEditResult.Color = "#ffe0bf";
                                break;
                            case "REVIEW_WARNING":
                                formEditResult.Color = "#fdf3c9";
                                break;

                        }
                        formEditResults.Add(formEditResult);
                    }
                }

                string mask = SchedulePageMask;
                bool viewOnly = !selection.CanEditCostReport;
                return Json(new
                {
                    Success = true,
                    ViewOnly = viewOnly,
                    Index = ScheduleBL.ReadValues(providerCostReportId, worksheetName, mask),
                    EditResult = formEditResults
                });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false });
            }
        }

        public JsonResult Update(int? rowId, int? colId, string rangeName, string colValue)
        {
            int providerCostReportId = -1;
            string prevValue = null;

            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request to update cost report."
                    }, JsonRequestBehavior.AllowGet);
                }

                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                // This is to prevent data being ovewritten by HTML control content.
                if (colValue == null || colValue.Contains("<input"))
                {
                    prevValue = ScheduleBL.GetSingleRangeNameValue(providerCostReportId, rangeName);

                    return Json(new
                    {
                        Success = false,
                        RestoreValue = prevValue
                    }, JsonRequestBehavior.AllowGet);
                }
                // If no HTML content is found in colValue, still fetch the previous value for AuditAdjustmentHistory.
                if (selection.CostReportType.StartsWith("4.9"))
                {
                    prevValue = ScheduleBL.GetSingleRangeNameValue(providerCostReportId, rangeName);
                }
                string worksheetName = GetCurrentWorksheetName();

                // Only update the value if this cost report is allowed to be updated.
                if (selection.CanEditCostReport)
                {
                    HtmlSanitizer sanitizer = new HtmlSanitizer();

                    string value = colValue?.Trim() ?? null;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (value.Length > 1024) value = value.Substring(0, 1024);
                        value = sanitizer.Sanitize(value);
                    }
                    // If the value is empty string, it should be saved as null.
                    else value = null;

                    ScheduleBL.UpdateGrid(providerCostReportId, rowId, colId, rangeName, value, worksheetName);
                    if (!(bool)(Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] ?? true)) Session[SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED] = "Grid";
                    Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "Grid";

                    // AuditAdjustmentHistory
                    if (selection.CostReportType.StartsWith("4.9"))
                    {
                        var fieldTemplate = ScheduleBL.GetFieldTemplates(providerCostReportId, worksheetName).Where(t => t.RangeName == rangeName).FirstOrDefault();
                        var myUser = RetrieveSession.ARUMUser.Email;
                        var auditAdjustmentHistory = new AuditAdjustmentHistory()
                        {
                            ProviderCostReportId = providerCostReportId,
                            FieldTemplateId = fieldTemplate.FieldTemplateId,
                            OriginalValue = prevValue,
                            NewValue = value,
                            CreatedBy = myUser,
                            CreatedDate = DateTime.Now
                        };

                        ScheduleBL.SaveAuditAdjustmentHistory(auditAdjustmentHistory);
                    }
                }

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, RestoreValue = prevValue, Message = "Value appears invalid; value not saved." }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult UpdateBatch(List<SaveValueViewModel> list, string overrideWorksheetName = "")
        {
            if (list == null) return Json(new { Success = true }, JsonRequestBehavior.AllowGet);

            int providerCostReportId = -1;
            string prevValue = null;

            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request to update cost report."
                    }, JsonRequestBehavior.AllowGet);
                }

                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                string worksheetName = overrideWorksheetName;
                if (string.IsNullOrWhiteSpace(overrideWorksheetName))
                {
                    string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                    if (string.IsNullOrWhiteSpace(selectedTab))
                    {
                        throw new ArgumentException("Invalid request.");
                    }

                    string[] token = selectedTab.Split('|');
                    if (token.Length != 3)
                    {
                        throw new ArgumentException("Invalid request.");
                    }

                    worksheetName = token[1];
                }
                // Only update the value if this cost report is allowed to be updated.
                if (selection.CanEditCostReport)
                {
                    List<string> rangeNames = new List<string>();
                    List<string> values = new List<string>();
                    HtmlSanitizer sanitizer = new HtmlSanitizer();

                    foreach (SaveValueViewModel item in list)
                    {
                        rangeNames.Add(item.RangeName);
                        string value = item?.ColValue?.Trim() ?? null;
                        // Sanitize if necessary.
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            if ((value?.Length ?? 0) > 1024) value = value.Substring(0, 1024);
                            value = sanitizer.Sanitize(value);
                        }
                        // If the value is empty string, it should be saved as null.
                        else value = null;
                        values.Add(value);
                    }

                    ScheduleBL.UpdateGridBatch(providerCostReportId, rangeNames, values, worksheetName);

                    if (!(bool)(Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] ?? true)) Session[SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED] = "Grid";
                    Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "Grid";
                }

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, RestoreValue = prevValue, Message = "Value appears invalid; value not saved." }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult DeleteTrialBalance()
        {
            try
            {
                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for Delete Trail Balance."
                    }, JsonRequestBehavior.AllowGet);
                }

                string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new ArgumentException("Invalid request.");
                }

                string[] token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new ArgumentException("Invalid request.");
                }

                string main = token[0];

                if (main != "schedule")
                {
                    throw new ArgumentException("Invalid request.");
                }

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                TrialBalanceBL.DeleteTrialBalance(providerCostReportId);
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "ClearData";

                return Json(new { Success = true, Message = @"" });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = @"Error clearing trial balance; not all data may be removed." });
            }
        }

        [HttpPost]
        public ActionResult ImportTrialBalance()
        {
            try
            {
                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for Import Trail Balance."
                    }, JsonRequestBehavior.AllowGet);
                }

                string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                // Ensure we have a selected tab.
                if (string.IsNullOrWhiteSpace(selectedTab)) throw new ArgumentException("Invalid request.");

                string[] token = selectedTab.Split('|');
                // Ensure we have the expected section type of tab selected.
                if (token.Length != 3) throw new ArgumentException("Invalid request.");

                // Ensure that we are in the schedule section.
                if ((token[0] ?? "") != "schedule") throw new ArgumentException("Invalid request.");

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                if (CostReportHistoryBL.CheckLockedStatus(providerCostReportId))
                {
                    _logger.Error("Unable to import schedule {0} for cost report ID {1}", "Trial Balance", providerCostReportId);
                    return Json(new { Success = false, Message = @"Cost Report is locked." });
                }

                //Get all files from Request object  
                HttpPostedFileBase file = Request.Files[0];

                CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false
                };

                List<TrialBalanceResult> resultRecords = new List<TrialBalanceResult>();
                bool isValidText = true;
                bool isValidFile = true;
                bool isDataToImport = true;

                using (StreamReader reader = new StreamReader(file.InputStream))
                {
                    using (CsvReader csv = new CsvReader(reader, config))
                    {
                        XmlDocument xml = new XmlDocument();
                        XmlElement root = xml.CreateElement("TrialBalance");
                        xml.AppendChild(root);

                        IEnumerable<TrialBalanceImportModel> csvFileRecords = csv.GetRecords<TrialBalanceImportModel>();

                        ICostReportUtils costReportUtils = CostReportBL.GetInstanceCostReportUtils(RetrieveSession.Classification);
                        List<TrialBalanceImportModel> importRecords = costReportUtils.ParseTrialBalanceImportData(csvFileRecords, resultRecords, out isValidText, out isValidFile, out isDataToImport);

                        isDataToImport = importRecords.Count > 0;
                        if (isDataToImport)
                        {
                            resultRecords.AddRange(TrialBalanceBL.ImportTrialBalanceData(providerCostReportId, importRecords));
                            Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "Import";
                        }
                    }
                }

                if (!isValidFile)
                {
                    return Json(new { Success = true, Message = @"Import completed. One or more values were invalidated and invalid characters removed. Please review your entries to ensure the correct data was provided from the file.", Data = resultRecords }, JsonRequestBehavior.AllowGet);
                }
                else if (!isDataToImport)
                {
                    return Json(new { Success = true, Message = @"There was no data to import. Please check the file.", Data = resultRecords }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { Success = true, Message = @"Import completed successfully.", Data = resultRecords }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return Json(new { Success = false, Message = "Error occurred importing the file. Please check that headers are correct and this is the correct file type." }, JsonRequestBehavior.AllowGet);
            }
        }


        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        [HttpPost]
        public ActionResult SchA1Import()
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int costReportId))
            {
                providerCostReportId = costReportId;
            }

            string worksheetName = GetCurrentWorksheetName();

            if (CostReportHistoryBL.CheckLockedStatus(providerCostReportId))
            {
                _logger.Error("Unable to import schedule {0} for cost report ID {1}", worksheetName, providerCostReportId);
                return Json(new { Success = false, Message = @"Cost Report is locked." });
            }

            //Get all files from Request object
            List<ScheduleImportResultModel> result = new List<ScheduleImportResultModel>();
            IWorkbook workbook;
            IWorksheet worksheet;

            try
            {
                ICostReportUtils costReportUtils = CostReportBL.GetInstanceCostReportUtils(RetrieveSession.Classification, DateTime.Now);

                HttpPostedFileBase file = Request.Files[0];
                if (file == null) throw new ArgumentException("You must select a file to upload.");
                if (!file.FileName.EndsWith(".xlsx")) throw new ArgumentException("Please upload a report file in *.xlsx format.");

                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    try
                    {
                        workbook = excelEngine.Excel.Workbooks.Open(file.InputStream);
                    }
                    catch
                    {
                        throw new FileNotFoundException("There was an issue opening the file.");
                    }

                    try
                    {
                        worksheet = workbook.Worksheets[0];
                    }
                    catch
                    {
                        throw new FormatException("The uploaded workbook is empty.");
                    }

                    List<ScheduleImportDataModel> data = new List<ScheduleImportDataModel>();

                    for (int iLine = 1; iLine < 30; iLine++)
                    {
                        for (int iCol = 1; iCol < 30; iCol++)
                        {
                            string value = worksheet[iLine, iCol].Value.Trim();
                            if (string.IsNullOrWhiteSpace(value)) continue;
                            data.Add(new ScheduleImportDataModel
                            {
                                ImportColumn = iCol,
                                ImportLine = iLine,
                                Value = value
                            });
                        }
                    }

                    List<ScheduleImportResultModel> list = costReportUtils.ParseScheduleImportData(selection.PeriodCode, "A-1", data);

                    if (list.Count > 0)
                    {
                        result = ScheduleBL.ImportScheduleData(providerCostReportId, worksheetName, list);
                        Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "Import";
                    }
                    else
                    {
                        return Json(new { Success = true, Message = @"There was no data to import. Please check the file.", Data = result }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Success = true, Message = "Schedule A-1 Import completed successfully.", Data = result }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeleteScheduleData()
        {
            try
            {
                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request Delete Schedule Data."
                    }, JsonRequestBehavior.AllowGet);
                }

                string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new ArgumentException("Invalid request.");
                }

                string[] token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new ArgumentException("Invalid request.");
                }

                string main = token[0];
                string worksheetName = token[1];

                if (main != "schedule")
                {
                    throw new ArgumentException("Invalid request.");
                }

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                ScheduleBL.DeleteScheduleData(providerCostReportId, worksheetName);
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "ClearSchedule";

                return Json(new { Success = true, Message = @"" });
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false, Message = "Unable to clear Schedule." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        [HttpPost]
        public ActionResult Edits()
        {
            try
            {
                string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
                if (string.IsNullOrWhiteSpace(selectedTab))
                {
                    throw new ArgumentException("Invalid request.");
                }

                string[] token = selectedTab.Split('|');
                if (token.Length != 3)
                {
                    throw new ArgumentException("Invalid request.");
                }

                string main = token[0];

                if (main != "schedule")
                {
                    throw new ArgumentException("Invalid request.");
                }

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }

                //ClassificationDTO classification = RetrieveSession.Classification;
                RateSettingPackage.ProviderPackage providerData = (RateSettingPackage.ProviderPackage)Session[SESSION_KEYS_OCRT.PROVIDER_DATA];
                List<EditResult> editList = CostReportEditsBL.GetCostReportEdits(SimplifiedType, providerCostReportId, selection.PeriodCode, providerData, selection.CostReportTypeId);

                // Reprocessed, so cache them.
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = null;
                Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] = editList;

                return Json(new { Success = true, Data = editList },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false, Message = "An error occurred while retrieving the results." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Recalculate(bool force = false)
        {
            try
            {
                var selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();

                if (!CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportTypeId))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Invalid request for Recalculate."
                    }, JsonRequestBehavior.AllowGet);
                }

                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }
                if (CostReportHistoryBL.CheckSubmissionStatus(providerCostReportId) != CostReportStatusEnum.Open)
                {
                    // If submitted, then we should NOT be recalculating!
                    return Json(new { Success = true, Message = "Cost Report has already been submitted; cannot process calculations." }, JsonRequestBehavior.AllowGet);
                }

                // If using local calculations, then skip the server calc engine.
                if (force || !(bool)(Session[SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS] ?? true)) ScheduleBL.CalculateAllSchedules(providerCostReportId);
                Session[SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED] = "";
                Session[SESSION_KEYS_OCRT.GRID_DEFINITION] = null;

                // Set flag to ensure that edits are recalculated.
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "RECALC";
                Session[SESSION_KEYS_OCRT.SCHED_EDITS_CURRENT] = null;
                return Json(new { Success = true, Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(
                    new { Success = false, Message = "An exception occurred attempting recalculation." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult Expand()
        {
            Session[SESSION_KEYS_OCRT.SCHED_EXPAND_COLLAPSE] = "expanded";
            return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
        }

        [SelectedProviderFeatureFilter("CostReport:View")]
        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult Collapse()
        {
            Session[SESSION_KEYS_OCRT.SCHED_EXPAND_COLLAPSE] = "collapsed";
            return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCorpOfficeSalaryCeilingBedSize()
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                var year = selection.PeriodCode.Substring(2);
                int.TryParse(year, out int currentYear_FromPeriodCode);
                var data = GetCodeList(currentYear_FromPeriodCode);
                return Json(new { Success = true, Data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);

                return Json(new { Success = false, Message = "An error occurred while retrieving the results." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult SpecialNarratives() => PartialView("_SpecialNarratives", new SpecialNarrativePerms());

        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public JsonResult GetCostReportSpecialNarrativesModel()
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
                if (selection == null)
                    //if (selection == null || !selection.CostReportType.StartsWith("4.9"))
                    throw new Exception("Invalid Response");

                SpecialNarrativesViewModel model = new SpecialNarrativesViewModel()
                {
                    Records = ScheduleBL.GetSpecialNarratives(int.Parse(selection.ProviderCostReportId))
                        .Select(x => new SpecialNarrativeViewItem(x)).ToList(),
                       CanEdit = selection.CanEditCostReport
               };

                return Json(new
                {
                    Success = true,
                    Data = model
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Failed to retrieve Cost Report's Special Narratives"
                });
            }
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        public JsonResult SaveCostReportSpecialNarrativeModel(SpecialNarrativeViewItem model)
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
                if (selection == null || !selection.CostReportType.StartsWith("4.9"))
                    throw new Exception("Invalid Response");

                int costReportId = 0;
                int.TryParse(selection.ProviderCostReportId, out costReportId);

                string message = ScheduleBL.SaveSpecialNarrative(model.CreateEntity(), costReportId);

                return Json(new
                {
                    Success = message == string.Empty,
                    Data = ScheduleBL.GetSpecialNarratives(costReportId)
                        .Select(x => new SpecialNarrativeViewItem(x)).ToList(),
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Failed to save Cost Report's Special Narrative"
                });
            }
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        public JsonResult DeleteCostReportSpecialNarrativeModel(SpecialNarrativeViewItem model)
        {
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
                if (selection == null || !selection.CostReportType.StartsWith("4.9"))
                    throw new Exception("Invalid Response");

                int costReportId = 0;
                int.TryParse(selection.ProviderCostReportId, out costReportId);

                string message = ScheduleBL.DeleteSpecialNarrative(model.CreateEntity(), costReportId);

                return Json(new
                {
                    Success = message == string.Empty,
                    Data = ScheduleBL.GetSpecialNarratives(int.Parse(selection.ProviderCostReportId))
                        .Select(x => new SpecialNarrativeViewItem(x)).ToList(),
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Failed to delete Cost Report's Special Narrative"
                });
            }
        }

        [ARUMFeatureActionFilterAttribute("CostReport:View")]
        public ActionResult AuditComments() => PartialView("_AuditComments", new AuditCommentPerms());

        [ARUMFeatureActionFilter("CostReport:View")]
        public ActionResult GetCostReportAuditCommentsModel()
        {
            try
            {
				SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
				if (selection == null)
					throw new Exception("Invalid Response");

                CostReportAuditCommentsViewModel model = new CostReportAuditCommentsViewModel()
                {
                    GridItems = GetAuditGridItems(),
                    AuditComments = ScheduleBL.GetAuditComments(int.Parse(selection.ProviderCostReportId))
                        .Select(x => new AuditCommentItem(x)).ToList(),
                    ExclusiveGroups = GetExclusiveGroups(),
                    SpecialNarratives = ScheduleBL.GetSpecialNarratives(int.Parse(selection.ProviderCostReportId))
						.Select(x => new SpecialNarrativeViewItem(x)).ToList(),
                    AuditCommentPerms = new AuditCommentPerms() { CanEdit = CostReportPermsissionsBL.CheckPermissionCostReportEdit(selection.CostReportType) },
				};

                return Json(new
                {
                    Success = true,
                    Data = model
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Failed to retrieve Cost Report's Audit Comments."
                });
            }
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        public JsonResult SaveCostReportAuditCommentModel(AuditCommentItem model)
        {
            try
            {
				SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
				if (selection == null)
					throw new Exception("Invalid Response");

				int costReportId = 0;
				int.TryParse(selection.ProviderCostReportId, out costReportId);

				string message = ScheduleBL.SaveAuditComment(model.CreateEntity(), costReportId);
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "SaveAuditComment";
                return Json(new
				{
					Success = message == string.Empty,
					Data = ScheduleBL.GetAuditComments(costReportId)
						.Select(x => new AuditCommentItem(x)).ToList(),
					Message = message
				});
			} catch (Exception ex)
            {
				_logger.Error(ex, ex.Message);
				return Json(new
				{
					Success = false,
					Message = "Failed to save Cost Report's Audit Comment"
				});
			}
        }

		[ARUMFeatureActionFilter("CostReport:View")]
		public JsonResult DeleteCostReportAuditCommentModel(AuditCommentItem model)
		{
			try
			{
				SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
				if (selection == null)
					throw new Exception("Invalid Response");

				string message = ScheduleBL.DeleteAuditComment(model.CreateEntity());
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "DeleteAuditComment";
                return Json(new
				{
					Success = message == string.Empty,
					Data = ScheduleBL.GetAuditComments(int.Parse(selection.ProviderCostReportId))
						.Select(x => new AuditCommentItem(x)).ToList(),
					Message = message
				});
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				return Json(new
				{
					Success = false,
					Message = "Failed to delete Cost Report's Audit Comment"
				});
			}
		}

		private string GetCurrentWorksheetName()
        {
            string selectedTab = Session[SESSION_KEYS_OCRT.SELECTED_TAB].ToString();
            if (string.IsNullOrWhiteSpace(selectedTab))
            {
                throw new ArgumentException("Invalid request.");
            }

            string[] token = selectedTab.Split('|');
            if (token.Length != 3)
            {
                throw new ArgumentException("Invalid request.");
            }

            return token[1];
        }

        private static string GetCodeList(int currentYear)
        {
            List<ChartOfCorpPosition> list_chartCorpPositions = new List<ChartOfCorpPosition>();
            string jsonString = string.Empty;

            list_chartCorpPositions = ScheduleBL.GetChartOfCorporatePositions(new DateTime(currentYear, 1, 1)).OrderBy(x => x.PositionNum).ToList();

            var final_chartCorpPositions = list_chartCorpPositions;
            if (list_chartCorpPositions != null && list_chartCorpPositions.Count > 0)
            {
                for (int i = 0; i < list_chartCorpPositions.Count; i++)
                {
                    var current_PositionNum = list_chartCorpPositions[i].PositionNum;
                    var current_BedSizeMin = list_chartCorpPositions[i].BedSizeMin;
                    string current_BedSize = string.Empty;
                    if (list_chartCorpPositions.Count - 1 != i)
                    {
                        var next_PositionNum = list_chartCorpPositions[i + 1].PositionNum;
                        var current_PositionCode = current_PositionNum.Substring(0, 2);
                        var next_PositionCode = next_PositionNum.Substring(0, 2);

                        if (current_PositionCode == next_PositionCode)
                        {
                            var current_BedSizeMax = list_chartCorpPositions[i + 1].BedSizeMin - 1;
                            current_BedSize = current_BedSizeMin.ToString() + "-" + current_BedSizeMax.ToString();
                        }
                        else
                        {
                            current_BedSize = current_BedSizeMin.ToString() + "-" + "Plus";
                        }
                    }
                    else
                    {
                        current_BedSize = current_BedSizeMin.ToString() + "-" + "Plus";
                    }
                    final_chartCorpPositions[i].BedSize = current_BedSize;
                }
            }

            var classification = RetrieveSession.Classification;
            var providerType = classification?.DisplayName;
            var facilityType = string.IsNullOrWhiteSpace(providerType) ? "" : providerType.Contains("ICF") ? "I" : "N";

            List<ChartOfAccount> positions = ChartOfAccountSeed_OH.LoadChartOfAccounts()
                .Where(x => x.ExpenseFlag
                    && (!string.IsNullOrWhiteSpace(x.PositionCode) && x.PositionCode.Substring(0, 3) == "WH0")
                    && (x.FacilityType == "A" || x.FacilityType == facilityType)
                    && x.YearBegin <= currentYear && x.YearEnd >= currentYear)
                .OrderBy(o => o.PositionCode)
                .ToList();

            List<ChartOfCorpPosition> lsit_COP = new List<ChartOfCorpPosition>();

            foreach (ChartOfAccount account in positions)
            {
                ChartOfCorpPosition crPosition = new ChartOfCorpPosition();
                {
                    crPosition.PositionNum = account.PositionCode;
                    crPosition.AccountNumber = account.AccountNumber;
                    crPosition.ClassificationTitle = account.Description;
                }
                ;
                lsit_COP.Add(crPosition);
            }
            final_chartCorpPositions.AddRange(lsit_COP);

            int j = 0;
            string[,] dataCOA = new string[final_chartCorpPositions.Count, 4];
            foreach (var chartOfAccount in final_chartCorpPositions)
            {
                if (chartOfAccount.AccountNumber == null)
                {
                    dataCOA[j, 0] = chartOfAccount.PositionNum;
                    dataCOA[j, 1] = chartOfAccount.AccountNumber;
                    dataCOA[j, 2] = chartOfAccount.ClassificationTitle;
                    dataCOA[j, 3] = chartOfAccount.BedSize;
                }
                else
                {
                    dataCOA[j, 0] = chartOfAccount.PositionNum;
                    dataCOA[j, 1] = chartOfAccount.AccountNumber;
                    dataCOA[j, 2] = chartOfAccount.ClassificationTitle;
                    dataCOA[j, 3] = string.Empty;
                }
                j++;
            }
            jsonString = JsonConvert.SerializeObject(new PopUpWindowModel
            {
                Width = 600,
                Height = 600,
                Title = "Position",
                Headers = new string[] { "Code", "Account", "Description", "Bed Size" },
                Data = dataCOA
            });
            return jsonString;
        }

        private string GetPositionNumberValues()
        {
            List<string> positions;
            string dd = string.Empty;
            try
            {
                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                if (int.TryParse(selection.PeriodCode.Substring(2), out int year))
                {
                    positions = ChartOfAccountSeed_OH.LoadChartOfAccounts()
                        .Where(x => x.ExpenseFlag
                            && (!string.IsNullOrWhiteSpace(x.PositionCode) && x.PositionCode.Substring(0, 3) == "WH0")
                            && (x.FacilityType == "A" || x.FacilityType == "I")
                            && x.YearBegin <= year && x.YearEnd >= year)
                        .Select(g => g.PositionCode)
                        .Distinct()
                        .ToList();
                    positions.AddRange(ScheduleBL.GetChartOfCorporatePositions(new DateTime(year, 1, 1)).Select(x => x.PositionNum));

                    List<string> list = positions.OrderBy(x => x).ToList();

                    List<string> s = new List<string> { "\"\": \"\"" };

                    foreach (string position in list)
                    {
                        s.Add($"\"{position}\": \"{position}\"");
                    }
                    dd = $"value: {{{string.Join(",", s)}}}";
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                throw;
            }
            return dd;
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        public JsonResult GetAuditAdjustments(int page, int rows, string sidx, string sord)
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int i))
            {
                providerCostReportId = i;
            }
            string costReportTypeId = selection.CostReportTypeId;

            var allData = ScheduleBL.GetAuditAdjustment(providerCostReportId, costReportTypeId);

            int totalRecords = allData.Count;
            int totalPages = (int)Math.Ceiling((double)totalRecords / rows);
            var pagedData = allData.Skip((page - 1) * rows).Take(rows).ToList();
            var jsonData = new
            {
                total = totalPages,
                page,
                records = totalRecords,
                rows = pagedData.Select(r => new
                {
                    id = r.AuditAdjustmentId,
                    cell = new object[]
                    {
                        r.AuditAdjustmentId,
                        r.LineNo,
                        r.AccNum,
                        r.WorkPaperReference,
                        r.Narr1,
                        r.Narr2,
                        r.Narr3,
                        r.AccountTypeDisplay,
                        r.Col,
                        r.RatioAlloc,
                        r.ReportedOrAdjusted,
                        r.Adjustment,
                        r.Adjusted,
                        r.AdjustmentPairId,
                        r.WorkSheetName,
                        r.AccountType,
                        r.ProviderCostReportId,
                        r.CostReportTypeId
                        //r.OrgRatioAlloc,
                        //r.OrgReportedOrAdjusted,
                        //r.OrgAdjustment,
                        //r.OrgAdjusted,
                    }
                })
            };

            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Save a new adjustment pair (line 1 + auto-generated line 2).
        /// </summary>
        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public JsonResult SaveAuditAdjustment(SaveAuditAdjustmentGridViewModel request)
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }
                string costReportTypeId = selection.CostReportTypeId;

                // Validate account number
                var validAcct = ValidateAccNum(request.AccNum);
                if (validAcct == null)
                    return Json(new { success = false, message = "Invalid Account Number" });

                // Validate at least one narrative
                if (string.IsNullOrWhiteSpace(request.Narr1) &&
                    string.IsNullOrWhiteSpace(request.Narr2) &&
                    string.IsNullOrWhiteSpace(request.Narr3))
                {
                    return Json(new { success = false, message = "At least one narrative code is required." });
                }

                // Validate WorkPaperReference length
                if (!string.IsNullOrEmpty(request.WorkPaperReference) && request.WorkPaperReference.Length > 7)
                {
                    return Json(new { success = false, message = "Work Paper Reference cannot exceed 7 characters." });
                }

                string userName = $"{myUser.FirstName} {myUser.LastName}";
                bool isScheduleE = validAcct.Schedule == "E";
                bool isAtt1 = validAcct.Schedule.StartsWith("AT1");
                string line1Col = isScheduleE ? request.Col : validAcct.ColumnLine1;
                string line2Col = isAtt1 ? "4" : (isScheduleE ? null : validAcct.ColumnLine2);

                var orgRatioAlloc = "1.0000";
                var orgReportedOrAdjusted = "";
                var orgAdjustment = "";
                var orgAdjusted = "";

                bool isAuditAdjustmentExists = ScheduleBL.IsAuditAdjustmentExists(providerCostReportId, costReportTypeId,
                    request.WorkSheetName, request.AccNum);
                if (!isAuditAdjustmentExists)
                {
                    AuditAdjustment auditAdjustmentNotActiveRecord = ScheduleBL.AuditAdjustmentNotActiveRecord(providerCostReportId, costReportTypeId,
                    request.WorkSheetName, request.AccNum);
                    if (auditAdjustmentNotActiveRecord != null)
                    {
                        if (isScheduleE)
                        {
                            orgAdjusted = auditAdjustmentNotActiveRecord.OrgAdjusted;
                            orgReportedOrAdjusted = auditAdjustmentNotActiveRecord.OrgAdjusted;
                        }
                        else if (isAtt1)
                        {
                            orgReportedOrAdjusted = auditAdjustmentNotActiveRecord.OrgReportedOrAdjusted;
                            orgAdjustment = auditAdjustmentNotActiveRecord.OrgAdjustment;
                            orgAdjusted = auditAdjustmentNotActiveRecord.OrgAdjusted;
                        }
                        else
                        {
                            orgRatioAlloc = auditAdjustmentNotActiveRecord.OrgRatioAlloc;
                            orgReportedOrAdjusted = auditAdjustmentNotActiveRecord.OrgReportedOrAdjusted;
                            orgAdjustment = auditAdjustmentNotActiveRecord.OrgAdjustment;
                            orgAdjusted = auditAdjustmentNotActiveRecord.OrgAdjusted;
                        }

                        // Delete the existing non-active records
                        ScheduleBL.DeleteAdjustment(providerCostReportId,
                            Convert.ToInt32(auditAdjustmentNotActiveRecord.AdjustmentPairId),
                            auditAdjustmentNotActiveRecord.AccNum);
                    }
                    else
                    {
                        if (isScheduleE)
                        {
                            orgAdjusted = request.ReportedOrAdjusted;
                            orgReportedOrAdjusted = request.ReportedOrAdjusted;
                        }
                        else if (isAtt1)
                        {
                            orgReportedOrAdjusted = validAcct.ReportedOrAdjusted;
                            orgAdjustment = validAcct.Adjustment;
                            orgAdjusted = validAcct.Adjusted;
                        }
                        else
                        {
                            orgRatioAlloc = validAcct.Line2RatioAlloc;
                            orgReportedOrAdjusted = validAcct.ReportedOrAdjusted;
                            orgAdjustment = validAcct.Adjustment;
                            orgAdjusted = validAcct.Adjusted;
                        }
                    }
                }

                // Build Line 1
                var line1 = new AuditAdjustment
                {
                    ProviderCostReportId = providerCostReportId,
                    CostReportTypeId = costReportTypeId,
                    LineNo = 1,
                    WorkSheetName = request.WorkSheetName,
                    AccNum = request.AccNum,
                    WorkPaperReference = request.WorkPaperReference,
                    Narr1 = request.Narr1?.Replace('x', 'X'),
                    Narr2 = request.Narr2?.Replace('x', 'X'),
                    Narr3 = request.Narr3?.Replace('x', 'X'),
                    Col = isScheduleE ? request.Col : validAcct.ColumnLine1,
                    AccountType = request.AccountType,
                    RatioAlloc = "",
                    ReportedOrAdjusted = request.ReportedOrAdjusted,
                    Adjustment = request.Adjustment == null ? "0" : request.Adjustment,
                    Adjusted = request.Adjusted,
                    OrgRatioAlloc = orgRatioAlloc,
                    OrgReportedOrAdjusted = orgReportedOrAdjusted,
                    OrgAdjustment = orgAdjustment,
                    OrgAdjusted = orgAdjusted
                };

                // Build Line 2
                int adjustmentAmt = 0;
                int.TryParse(request.Adjustment, out adjustmentAmt);

                int reportedLine1 = 0;
                int.TryParse(request.ReportedOrAdjusted, out reportedLine1);

                decimal ratioAlloc = 1.0000m;
                if (!string.IsNullOrEmpty(request.Line2RatioAlloc))
                    decimal.TryParse(request.Line2RatioAlloc, out ratioAlloc);
                else if (!isScheduleE && !isAtt1)
                    ratioAlloc = 1.0000m; //GetOriginalAllocationRatio(request.ProviderCostReportId, request.WorkSheetName, request.AccNum);

                var line2 = new AuditAdjustment
                {
                    ProviderCostReportId = providerCostReportId,
                    CostReportTypeId = costReportTypeId,
                    LineNo = 2,
                    WorkSheetName = request.WorkSheetName,
                    AccNum = "",//request.AccNum,
                    WorkPaperReference = "", // Will display account description
                    Narr2 = request.Line2Narr2,
                    Col = isAtt1 ? "4" : (isScheduleE ? null : validAcct.ColumnLine2),
                    RatioAlloc = ratioAlloc.ToString("F4")
                };

                if (isScheduleE || isAtt1)
                {
                    // Line 2 is entirely disabled/blank for Schedule E and Att 1
                    line2.Col = isAtt1 ? "4" : (isScheduleE ? null : validAcct.ColumnLine2);
                    line2.RatioAlloc = "";
                    line2.ReportedOrAdjusted = "";
                    line2.Adjustment = "";
                    line2.Adjusted = "";
                }
                else
                {
                    // Line 2 calculations for B-1, B-2, C, D schedules
                    int line2Reported = (int)Math.Round(reportedLine1 * ratioAlloc, MidpointRounding.AwayFromZero);
                    int line2Adjustment = (int)Math.Round(adjustmentAmt * ratioAlloc, MidpointRounding.AwayFromZero);
                    int line2Adjusted = line2Reported + line2Adjustment;

                    line2.ReportedOrAdjusted = line2Reported.ToString();
                    line2.Adjustment = line2Adjustment.ToString();
                    line2.Adjusted = line2Adjusted.ToString();
                }

                int pairId = ScheduleBL.SaveAuditAdjustmentPair(line1, line2, userName);

                ScheduleBL.RecalculateAuditAdjustmentChain(
                      providerCostReportId,
                      costReportTypeId,
                      request.AccNum,
                      ratioAlloc.ToString("F4"), // NEW ratio from UI
                      userName
                     );
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "SaveAuditAdjustment";
                return Json(new { success = true, pairId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving adjustment: " + ex.Message });
            }
        }

        /// <summary>
        /// Update an existing adjustment.
        /// </summary>
        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public JsonResult UpdateAuditAdjustment(SaveAuditAdjustmentGridViewModel model)
        {
            try
            {
                // Only line 1 fields can be updated by user; line 2 auto-calculates
                // (except RatioAlloc on line 2 for B/C/D schedules)
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;


                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }
                string costReportTypeId = selection.CostReportTypeId;

                // Validate account number
                var validAcct = ValidateAccNum(model.AccNum);
                if (validAcct == null)
                    return Json(new { success = false, message = "Invalid Account Number" });

                // Validate at least one narrative
                if (string.IsNullOrWhiteSpace(model.Narr1) &&
                    string.IsNullOrWhiteSpace(model.Narr2) &&
                    string.IsNullOrWhiteSpace(model.Narr3))
                {
                    return Json(new { success = false, message = "At least one narrative code is required." });
                }

                string userName = $"{myUser.FirstName} {myUser.LastName}";
                bool isScheduleE = validAcct.Schedule == "E";
                bool isAtt1 = validAcct.Schedule.StartsWith("AT1");
                decimal ratio = 1.0000m;
                var orgRatioAlloc = "1.0000";
                var orgReportedOrAdjusted = "";
                var orgAdjustment = "";
                var orgAdjusted = "";

                // Fetch both lines of the pair
                var adjustments = ScheduleBL.GetAuditAdjustment(providerCostReportId, costReportTypeId);
                var line1 = adjustments.FirstOrDefault(a => a.AdjustmentPairId == model.AdjustmentPairId && a.LineNo == 1);
                var line2 = adjustments.FirstOrDefault(a => a.AdjustmentPairId == model.AdjustmentPairId && a.LineNo == 2);

                if (line1 == null)
                    return Json(new { success = false, message = "Adjustment not found." });

                if (line1.AccNum != model.AccNum)
                {
                    ScheduleBL.CopyOrgCRValuesToNextAdjustment(providerCostReportId, (int)line1.AdjustmentPairId, line1.AccNum);

                    bool isAuditAdjustmentExists = ScheduleBL.IsAuditAdjustmentExists(providerCostReportId, costReportTypeId,
                    model.WorkSheetName, model.AccNum);
                    if (!isAuditAdjustmentExists)
                    {
                        if (isScheduleE)
                        {
                            orgAdjusted = model.ReportedOrAdjusted;
                            orgReportedOrAdjusted = model.ReportedOrAdjusted;
                        }
                        else if (isAtt1)
                        {
                            orgReportedOrAdjusted = validAcct.ReportedOrAdjusted;
                            orgAdjustment = validAcct.Adjustment;
                            orgAdjusted = validAcct.Adjusted;
                        }
                        else
                        {
                            orgRatioAlloc = validAcct.Line2RatioAlloc;
                            orgReportedOrAdjusted = validAcct.ReportedOrAdjusted;
                            orgAdjustment = validAcct.Adjustment;
                            orgAdjusted = validAcct.Adjusted;
                        }
                    }
                }
                

                // Apply updates to line 1
                var updLine1 = new AuditAdjustment
                {
                    AuditAdjustmentId = line1.AuditAdjustmentId,
                    ProviderCostReportId = providerCostReportId,
                    CostReportTypeId = costReportTypeId,
                    LineNo = 1,
                    WorkSheetName = model.WorkSheetName,
                    AccNum = model.AccNum,
                    WorkPaperReference = model.WorkPaperReference,
                    Narr1 = model.Narr1,
                    Narr2 = model.Narr2,
                    Narr3 = model.Narr3,
                    Col = isScheduleE ? model.Col : validAcct.ColumnLine1, 
                    AccountType = model.AccountType,
                    RatioAlloc = "",
                    ReportedOrAdjusted = model.ReportedOrAdjusted,
                    Adjustment = model.Adjustment == null ? "0" : model.Adjustment,
                    Adjusted = model.Adjusted,
                    OrgRatioAlloc = orgRatioAlloc,
                    OrgReportedOrAdjusted = orgReportedOrAdjusted,
                    OrgAdjustment = orgAdjustment,
                    OrgAdjusted = orgAdjusted
                };

                // Recalculate line 2 if present
                AuditAdjustment updLine2 = null;
                if (line2 != null)
                {
                    
                    decimal.TryParse(model.Line2RatioAlloc ?? "1.0000", out ratio);
                    int reportedLine1Val = 0;
                    int.TryParse(model.ReportedOrAdjusted, out reportedLine1Val);

                    int adjAmt = 0;
                    int.TryParse(model.Adjustment, out adjAmt);

                    int line2Reported = (int)Math.Round(reportedLine1Val * ratio, MidpointRounding.AwayFromZero);
                    int line2Adjustment = (int)Math.Round(adjAmt * ratio, MidpointRounding.AwayFromZero);
                    int line2Adjusted = line2Reported + line2Adjustment;

                    updLine2 = new AuditAdjustment
                    {
                        AuditAdjustmentId = line2.AuditAdjustmentId,
                        ProviderCostReportId = providerCostReportId,
                        CostReportTypeId = costReportTypeId,
                        LineNo = 2,
                        WorkSheetName = model.WorkSheetName,
                        AccNum = "",
                        Narr2 = model.Line2Narr2,
                        Col = isAtt1 ? "4" : (isScheduleE ? null : validAcct.ColumnLine2), 
                        RatioAlloc = model.Line2RatioAlloc,
                        ReportedOrAdjusted = line2Reported.ToString(),
                        Adjustment = line2Adjustment.ToString(),
                        Adjusted = line2Adjusted.ToString()
                    };
                    if (isScheduleE || isAtt1)
                    {
                        // Line 2 is entirely disabled/blank for Schedule E and Att 1
                        updLine2.RatioAlloc = "";
                        updLine2.ReportedOrAdjusted = "";
                        updLine2.Adjustment = "";
                        updLine2.Adjusted = "";
                    }
                }

                if (updLine2 != null)
                {
                    ScheduleBL.UpdateAuditAdjustmentPair(updLine1, updLine2, userName);

                    if (line1.AccNum != updLine1.AccNum)
                    {
                        ScheduleBL.RecalculateAuditAdjustmentChain(
                          providerCostReportId,
                          costReportTypeId,
                          line1.AccNum,
                          string.IsNullOrEmpty(line2.RatioAlloc) ? null : line2.RatioAlloc,
                          userName);
                    }
                    ScheduleBL.RecalculateAuditAdjustmentChain(
                      providerCostReportId,
                      costReportTypeId,
                      model.AccNum,
                      ratio.ToString("F4"),   // NEW ratio from UI
                      userName);
                }
                Session[SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED] = "UpdateAuditAdjustment";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating adjustment: " + ex.Message });
            }
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public JsonResult DeleteAdjustment(string adjustmentPairId, 
            string accNum, 
            string line2RatioAlloc)
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
                string userName = $"{myUser.FirstName} {myUser.LastName}";

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }
                string costReportTypeId = selection.CostReportTypeId;

                List<AuditAdjustment> auditAdjustments = ScheduleBL.GetAuditAdjustmentsByAdjustmentPairId(providerCostReportId, Convert.ToInt32(adjustmentPairId));
                AuditAdjustment adj1 = auditAdjustments.FirstOrDefault(x => x.LineNo == 1);
                AuditAdjustment adj2 = auditAdjustments.FirstOrDefault(x => x.LineNo == 2);

                ScheduleBL.DeleteAdjustment(providerCostReportId, 
                    Convert.ToInt32(adjustmentPairId), 
                    accNum);

                ScheduleBL.RecalculateAuditAdjustmentChain(
                     providerCostReportId,
                     costReportTypeId,
                     accNum,
                     string.IsNullOrWhiteSpace(line2RatioAlloc) ? "1.0000" : line2RatioAlloc, //line2RatioAlloc,   // NEW ratio from UI
                     userName
                    );

                return Json(new { success = true, Message = @"" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting adjustment: " + ex.Message });
            }
        }

        /// <summary>
        /// Validate an account number against the valid audit account list.
        /// </summary>
        /// 
        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public JsonResult ValidateAccountNumber(string accNum, string worksheetName)
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int i))

            {
                providerCostReportId = i;
            }
            string costReportTypeId = selection.CostReportTypeId;

                var validAccount = ValidateAccNum(accNum);

            if (validAccount == null)
            {
                return Json(new { success = false, message = "Invalid Account Number" });
            }

            // Determine AccountType options
            var accountType = GetAccountTypeOptions(validAccount.SalaryOtherPeriod);

            return Json(new
            {
                success = true,
                data = new
                {
                    accountNumber = validAccount.AccountNumber,
                    colLine1 = validAccount.ColumnLine1,
                    colLine2 = validAccount.ColumnLine2,
                    salaryOtherPeriod = validAccount.SalaryOtherPeriod,
                    line1RatioAlloc = "",
                    line2RatioAlloc = validAccount.Line2RatioAlloc,
                    line2Narr2 = validAccount.Line2Desc,
                    accountTypeOptions = accountType,
                    schedule = validAccount.Schedule,
                    isScheduleE = validAccount.Schedule == "E",
                    isAttachment1 = validAccount.Schedule.StartsWith("AT1")
                }
            });
        }

        /// <summary>
        /// Validate account number against Audit Account List
        /// </summary>
        /// 
        [ARUMFeatureActionFilter("CostReport:View")]
        public AuditAdjustmentPanelRecordViewModel ValidateAccNum(string accNum)
        {
            if (string.IsNullOrWhiteSpace(accNum))
                throw new ArgumentException("Account number cannot be null or empty.", nameof(accNum));

            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int i))

            {
                providerCostReportId = i;
            }

            var costReportInfo = ScheduleBL.GetCostReportInfo(providerCostReportId);
            return ValidateCostReportAccNum(costReportInfo.documentTemplateId, costReportInfo.documentId, accNum);
        }

        public AuditAdjustmentPanelRecordViewModel ValidateCostReportAccNum(int? documentTemplateId, int? documentId, string accNum)
        {
            if (documentTemplateId == null || documentId == null || string.IsNullOrEmpty(accNum))
                return null;

            var fieldTemplates = ScheduleBL.GetOriginalCostReportRecords(documentTemplateId, documentId, accNum);
            if (fieldTemplates == null || fieldTemplates.Count == 0)
                return null;

            var auditAdjustmentPanelRecord = new AuditAdjustmentPanelRecordViewModel();

            // Helper method to find the column
            string GetColumn(string fieldName, string[] excludedPrefixes) =>
                !string.IsNullOrEmpty(fieldName)
                    ? fieldTemplates
                        .FirstOrDefault(x => !string.IsNullOrEmpty(x.FieldName) &&
                                             x.FieldName.Contains(fieldName) &&
                                             !string.IsNullOrEmpty(x.RangeAddress) &&
                                             !excludedPrefixes.Any(prefix => x.RangeAddress.StartsWith(prefix)))
                        ?.Column
                    : null;

            // Helper method to fetch original cost report value
            string GetOriginalCostReportValue(string ws, string column) =>
                ScheduleBL.GetOriginalCostReportValue(documentTemplateId, documentId, accNum, ws, column);

            string worksheetName = fieldTemplates.FirstOrDefault(x => x.DisplayColumn == 3)?.WorksheetName;
            auditAdjustmentPanelRecord.Schedule = worksheetName;

            if (string.IsNullOrEmpty(worksheetName))
                return auditAdjustmentPanelRecord;

            switch (worksheetName)
            {
                case "E":
                    auditAdjustmentPanelRecord.ColumnLine1 = GetColumn("Beginning Of Period", new[] { "S", "P", "G" });
                    auditAdjustmentPanelRecord.ColumnLine2 = GetColumn("End Of Period", new[] { "S", "P", "G" });
                    auditAdjustmentPanelRecord.SalaryOtherPeriod = "Begin Period, End Period";
                    break;

                case string s when s.StartsWith("AT1"):
                    var adjustmentCol = GetColumn("Adjustments Increases (Decreases)", new[] { "S", "P", "T", "G" });
                    auditAdjustmentPanelRecord.ColumnLine1 = GetColumn("Total", new[] { "P", "T", "G" });
                    auditAdjustmentPanelRecord.SalaryOtherPeriod = "Revenue";
                    auditAdjustmentPanelRecord.ReportedOrAdjusted = GetOriginalCostReportValue(worksheetName, auditAdjustmentPanelRecord.ColumnLine1);
                    auditAdjustmentPanelRecord.Adjustment = GetOriginalCostReportValue(worksheetName, adjustmentCol);
                    break;

                default:
                    var allocRatioCol = GetColumn("Alloc", new[] { "S", "P", "T", "G" });
                    adjustmentCol = GetColumn("Adjustments Increases (Decreases)", new[] { "S", "P", "T", "G" });

                    auditAdjustmentPanelRecord.ColumnLine1 = GetColumn("Adjusted Total", new[] { "P", "T", "G" });
                    auditAdjustmentPanelRecord.ColumnLine2 = GetColumn("Allocated Adjust", new[] { "S", "P", "G" });

                    bool salaryValue = !string.IsNullOrEmpty(GetColumn("Salary Facility Employed", new[] { "S", "P", "G" }));
                    bool otherValue = !string.IsNullOrEmpty(GetColumn("Other/Contract Wages", new[] { "S", "P", "G" }));

                    auditAdjustmentPanelRecord.SalaryOtherPeriod = worksheetName == "D"
                        ? "Other"
                        : salaryValue && otherValue ? "Salary, Other" :
                          salaryValue ? "Salary" : otherValue ? "Other" : string.Empty;

                    auditAdjustmentPanelRecord.Line2RatioAlloc = GetOriginalCostReportValue(worksheetName, allocRatioCol);
                    auditAdjustmentPanelRecord.ReportedOrAdjusted = GetOriginalCostReportValue(worksheetName, auditAdjustmentPanelRecord.ColumnLine1);
                    auditAdjustmentPanelRecord.Adjustment = GetOriginalCostReportValue(worksheetName, adjustmentCol);
                    auditAdjustmentPanelRecord.Adjusted = GetOriginalCostReportValue(worksheetName, auditAdjustmentPanelRecord.ColumnLine2);
                    break;
            }

            auditAdjustmentPanelRecord.AccountNumber = accNum;
            auditAdjustmentPanelRecord.Line2Desc = fieldTemplates.FirstOrDefault(x => x.DisplayColumn == 2)?.FieldName;
            auditAdjustmentPanelRecord.Line1RatioAlloc = "";

            return auditAdjustmentPanelRecord;
        }

        private List<object> GetAccountTypeOptions(string salaryOtherPeriod)
        {
            var options = new List<object>();
            if (string.IsNullOrEmpty(salaryOtherPeriod)) return options;

            var types = salaryOtherPeriod.Split(',').Select(s => s.Trim()).ToList();
            foreach (var t in types)
            {
                switch (t)
                {
                    case "Salary": options.Add(new { value = 1, text = "Salary" }); break;
                    case "Other": options.Add(new { value = 2, text = "Other" }); break;
                    case "Begin Period": options.Add(new { value = 3, text = "Begin Period" }); break;
                    case "End Period": options.Add(new { value = 4, text = "End Period" }); break;
                    case "Revenue": options.Add(new { value = 5, text = "Revenue" }); break;
                }
            }
            return options;
        }

        /// <summary>
        /// Get the Reported/Adjusted value for an account (from original CR or most recent adjustment).
        /// </summary>
        /// 
        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public JsonResult GetReportedAdjustedValue(string accNum, string col, string schedule)
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int i))
            {
                providerCostReportId = i;
            }

            string costReportTypeId = selection.CostReportTypeId;

            var costReportInfo = ScheduleBL.GetCostReportInfo(providerCostReportId);
            var auditAdjustmentPanelRecord = ValidateCostReportAccNum(costReportInfo.documentTemplateId, 
                costReportInfo.documentId, accNum);

            // Check for most recent adjusted value first
            var mostRecent = ScheduleBL.GetMostRecentAdjustedValue(
                providerCostReportId, costReportTypeId, schedule, accNum, col, 1);

            int value = 0;
            decimal ratio = 1.0000m;
            string line2Desc = "";

            if (mostRecent.adjusted.HasValue && mostRecent.ratio.HasValue)
            {
                value = mostRecent.adjusted.Value;
                ratio = mostRecent.ratio.Value;
                line2Desc = mostRecent.desc;
            }
            else
            {
                var adjusted = ScheduleBL.GetOriginalCostReportValue(costReportInfo.documentTemplateId, 
                    costReportInfo.documentId, accNum, schedule, col);

                if (!string.IsNullOrWhiteSpace(adjusted) && int.TryParse(adjusted, out int parsedAdjusted))
                {
                    value = parsedAdjusted;
                }
                // Extract the Original RatioAlloc value and Line2 Desc for LineNo 2
                if (!string.IsNullOrWhiteSpace(auditAdjustmentPanelRecord.Line2RatioAlloc) &&
                decimal.TryParse(auditAdjustmentPanelRecord.Line2RatioAlloc, out decimal parsedRatio))
                {
                    ratio = parsedRatio;
                }
            }

            // Determine AccountType options
            var accountType = GetAccountTypeOptions(auditAdjustmentPanelRecord.SalaryOtherPeriod);

            return Json(new
            {
                success = true,
                data = new
                {
                    accountNumber = auditAdjustmentPanelRecord.AccountNumber,
                    schedule = auditAdjustmentPanelRecord.Schedule,
                    line2Narr2 = auditAdjustmentPanelRecord.Line2Desc,
                    colLine1 = auditAdjustmentPanelRecord.ColumnLine1,
                    colLine2 = auditAdjustmentPanelRecord.ColumnLine2,
                    salaryOtherPeriod = auditAdjustmentPanelRecord.SalaryOtherPeriod,
                    line1RatioAlloc = "", 
                    line2RatioAlloc = ratio,
                    accountTypeOptions = accountType,
                    reportedAdj = value,
                    orgRatioAlloc = ratio,
                    orgReportedAdj = value,
                    isScheduleE = auditAdjustmentPanelRecord.Schedule == "E",
                    isAttachment1 = auditAdjustmentPanelRecord.Schedule.StartsWith("AT1")
                }
            });

        }

        ///// <summary>
        ///// Get the original allocation ratio from cost report column 6.
        ///// </summary>
        ///// 
        //[ARUMFeatureActionFilter("CostReport:View")]
        //[HttpPost]
        //public JsonResult GetAllocationRatio(string accNum, string col, string schedule)
        //{
        //    // Set default ratio
        //    decimal ratio = 1.0000m;

        //    // If schedule matches specific cases, return the default ratio immediately
        //    if (schedule == "E" || schedule.StartsWith("At1", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return Json(new { success = true, ratio = "" });
        //        //ratio = ratio.ToString("F4")
        //    }

        //    // Retrieve selection model from session
        //    var selection = Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] as SelectionModel ?? new SelectionModel();

        //    // Parse providerCostReportId from selection and default to 0 if invalid
        //    if (!int.TryParse(selection.ProviderCostReportId, out int providerCostReportId))
        //    {
        //        providerCostReportId = 0;
        //    }
        //    string costReportTypeId = selection.CostReportTypeId;

        //    /// Check for most recent ratioAlloc value first
        //    var mostRecent = ScheduleBL.GetMostRecentAdjustedValue(
        //        providerCostReportId, costReportTypeId, schedule, accNum, col, 1);

        //    if (mostRecent.ratio.HasValue)
        //    {
        //        ratio = mostRecent.ratio.Value;
        //    }
        //    else
        //    {
        //        // Get the allocation ratio
        //        string ratioAlloc = ScheduleBL.GetOriginalCostReportValue(providerCostReportId, accNum, schedule, "6");

        //        // Update ratio only if ratioAlloc is not null or whitespace
        //        if (!string.IsNullOrWhiteSpace(ratioAlloc) && decimal.TryParse(ratioAlloc, out decimal parsedRatio))
        //        {
        //            ratio = parsedRatio;
        //        }
        //    }
        //    return Json(new { success = true, ratio = ratio.ToString("F4") });
        //}

        ///// <summary>
        ///// Get valid account numbers for dropdown/autocomplete.
        ///// </summary>
        //[HttpGet]
        //public JsonResult GetValidAccountNumbers(string worksheetName)
        //{
        //    ICostReportUtils utils = CostReportBL.GetInstanceCostReportUtils(RetrieveSession.Classification)
        //                ?? throw new ArgumentException("Unknown provider type found.");

        //    var accounts = utils.GetAuditAdjustmentPanelRecord()
        //                .Where(x => x.Schedule == worksheetName)
        //                .ToList()
        //                ?? throw new InvalidOperationException("Schedule Name not found.");

        //    return Json(accounts.Select(a => new { value = a.AccountNumber, label = a.AccountNumber + " - " + a.Schedule }),
        //        JsonRequestBehavior.AllowGet);
        //}

        /// <summary>
        /// Validate a narrative reference number.
        /// </summary>
        /// 
        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public JsonResult ValidateNarrative(string referenceNumber)
        {
            SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
            int providerCostReportId = 0;
            if (int.TryParse(selection.ProviderCostReportId, out int i))
            {
                providerCostReportId = i;
            }

            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                return Json(new { success = false, message = "Reference number is required." });
            }

            // Check if special narrative (starts with X, number <= 999)
            if (ScheduleBL.IsSpecialNarrative(referenceNumber))
            {
                bool exists = ScheduleBL.SpecialNarrativeExists(providerCostReportId, referenceNumber);
                return Json(new
                {
                    success = true,
                    isSpecial = true,
                    needsDescription = !exists,
                    message = ""
                });
            }
            else if (int.TryParse(referenceNumber, out int refNum))
            {
                DateTime? periodEndDate = ScheduleBL.GetCostReportPeriodEndDate(providerCostReportId);
                bool isValid = ScheduleBL.ValidateNarrative(referenceNumber, periodEndDate);

                return Json(new
                {
                    success = isValid,
                    isSpecial = false,
                    message = isValid ? "" : "Invalid standard narrative reference number."
                });
            }
            return Json(new { success = false, message = "Invalid narrative reference. Use standard (refer to Standard Narrative Tab for options) or special (X0-X999)." });
        }

        /// <summary>
        /// Save a special narrative description.
        /// </summary>
        /// 
        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public JsonResult SaveSpecialNarrative(string referenceNumber, string description)
        {
            try
            {
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;

                string userName = $"{myUser.FirstName} {myUser.LastName}";
                referenceNumber = referenceNumber.Replace('x', 'X');

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT];
                if (selection == null)
                    throw new Exception("Invalid Response");

                int providerCostReportId = 0;
                int.TryParse(selection.ProviderCostReportId, out providerCostReportId);

                // Build Narrative
                var narr = new SpecialNarrativeViewItem
                {
                    NarrativeNumber = referenceNumber,
                    Description = description
                };

                string message = ScheduleBL.SaveSpecialNarrative(narr.CreateEntity(), providerCostReportId);

                return Json(new
                {
                    Success = message == string.Empty,
                    Data = ScheduleBL.GetSpecialNarratives(providerCostReportId)
                        .Select(x => new SpecialNarrativeViewItem(x)).ToList(),
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return Json(new
                {
                    Success = false,
                    Message = "Failed to save Cost Report's Special Narrative"
                });
            }
        }

        [ARUMFeatureActionFilter("CostReport:View")]
        [HttpPost]
        public JsonResult RecalculateAuditAdjustment()
        {
            try
            {
                // Only line 1 fields can be updated by user; line 2 auto-calculates
                // (except RatioAlloc on line 2 for B/C/D schedules)
                MSLC.ArumProxy.Models.ARUMProxy_User myUser = RetrieveSession.ARUMUser;
                string userName = $"{myUser.FirstName} {myUser.LastName}";

                SelectionModel selection = (SelectionModel)Session[SESSION_KEYS_OCRT.SELECTED_COST_REPORT] ?? new SelectionModel();
                int providerCostReportId = 0;
                if (int.TryParse(selection.ProviderCostReportId, out int i))
                {
                    providerCostReportId = i;
                }
                string costReportTypeId = selection.CostReportTypeId;
                List<AuditAdjustment> allData = ScheduleBL.GetAllAuditAdjustments(providerCostReportId, costReportTypeId);

                ScheduleBL.HandleWorksheetRemoveLines(allData, providerCostReportId);

                ScheduleBL.HandleWorksheetAddAdjustments(allData, providerCostReportId);

                ScheduleBL.DeleteNonActiveAuditAdjustments(providerCostReportId, costReportTypeId);

                //var summary = ScheduleBL.RecalculateAuditAdjustmentSummary(allData);

                //ScheduleBL.UpdateRecalculateAuditAdjustmentValue(summary);

                return Json(new { Success = true, Message = @"" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error recalculating adjustment: " + ex.Message });
            }
        }

		private List<AuditCommentGridItem> GetAuditGridItems()
        {
            switch (RetrieveSession.Classification.Type)
            {
                case "NF":
                {
                    string[] narrativeNumbersForGrid = {
                        "A", "B", "C", "D", "E",
                        "F", "G", "H", "I", "J",
                        "K", "L", "M", "N", "O",
                        "P", "Q", "", "", "T",
                        "UA", "UB", "UC", "UD", "UE",
                        "V", "W", "X", "Y", "Z",
                        "SchA", "A1", "C1", "C2-1", "C2-2", "E-1",
                        "At6-1", "At6-2",
                        "", "", "",
                        "Special Comments", "", "", ""
                    };

                    return FillGrid(narrativeNumbersForGrid);
                }
                case "ICF":
                {
                    string[] narrativeNumbersForGrid = {
                        "A", "B", "C", "D", "E",
                        "F", "G", "H", "I", "J",
                        "K", "L", "M", "N", "O",
                        "P", "Q", "", "", "T",
                        "UA", "UB", "UC", "UD", "UE",
                        "V", "W", "X", "Y", "Z",
                        "SchA", "A1", "C1", "C2-1", "C2-2",
                        "E1-1", "E1-2", "At6-1", "At6-2", "At6-3",
                        "At9-1-1", "At9-1-2", "At9-1-3", "At9-2", "", "",
                        "Special Comments", "", "", ""
                    };

                    return FillGrid(narrativeNumbersForGrid);
                }
                default:
                {
                    string[] narrativeNumbersForGrid = {
                        "A", "B", "C", "D", "E",
                        "F", "G", "H", "I", "J",
                        "K", "L", "M", "N", "O",
                        "P", "Q", "", "", "T",
                        "UA", "UB", "UC", "UD", "UE",
                        "V", "W", "X", "Y", "Z",
                        "SchA", "A1", "C1", "C2-1", "C2-2", "E-1",
                        "E1-1", "E1-2", "At6-1", "At6-2", "At6-3",
                        "At9-1-1", "At9-1-2", "At9-1-3", "At9-2", "",
                        "Special Comments", "", "", ""
                    };

                    return FillGrid(narrativeNumbersForGrid);
                }
            }
        }

        private List<AuditCommentGridItem> FillGrid(string[] narrativeNumbersForGrid)
        {
            List<AuditCommentGridItem> gridItems = new List<AuditCommentGridItem>();

            string[] arrowNumbers = { "B", "UB", "UC", "UE", "X", "SchA", "A1", "C1", "C2-1", "C2-2",
                "E-1", "E1-1", "E1-2", "At6-1", "At6-2", "At6-3", "At9-1-1",
                "At9-1-2", "At9-1-3", "At9-2", "Special Comments"
            };

            var narrativeTableRecords =
                CostReportAuditBL.GetNarrativeTableRecordsByNarrativeNumbers(narrativeNumbersForGrid);
            gridItems.AddRange(narrativeNumbersForGrid.Select(narrativeNumber => new AuditCommentGridItem
            {
                NarrativeNumber = narrativeNumber,
                HasIcon = arrowNumbers.Contains(narrativeNumber) ? true : false,
                Description = narrativeTableRecords.Where(r => r.NarrativeNumber == narrativeNumber)
                    .Select(x => x.Description)
                    .FirstOrDefault()
            }));

            return gridItems;
        }

        private Dictionary<string, List<string>> GetExclusiveGroups()
		{
			Dictionary<string, List<string>> exclusiveGroups = new Dictionary<string, List<string>>()
			{
				{ "groupA", new List<string>{ "A", "B" } },
				{ "groupB", new List<string>{ "C", "D" } },
				{ "groupC", new List<string>{ "E", "F", "G", "H", "I" } },
				{ "groupD", new List<string>{ "J", "K"} },
				{ "groupE", new List<string>{ "L", "M", "X", "Z"} },
				{ "groupF", new List<string>{ "N", "O", "P", "Q"} },
				{ "groupG", new List<string>{ "T", "UA", "UB", "UC", "UD", "UE" } },
				{ "groupH", new List<string>{ "V", "W", "Y"} }
			};

			return exclusiveGroups;
		}

        private bool IsEditableSchedule(string worksheetName)
        {
            // 4.9x editable schedules
            var editableSchedules = new List<string>()
            {
                "A", "A-1", "C-1", "C-2 Pt1", "C-2 Pt2",
                "E-1", "E-1 Pt1", "E-1 Pt2", "AT6 Pt1", "AT6 Pt2", "AT6 Pt3",
                "AT9 Pt1 Pg1", "AT9 Pt1 Pg2", "AT9 Pt1 Pg3", "AT9 Pt2"
            };

            if (editableSchedules.Contains(worksheetName))
                return true;

            return false;
        }
    }
}