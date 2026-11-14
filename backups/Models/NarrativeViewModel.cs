using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using MSLC.Portal.MVC.Utility;
using Syncfusion.JavaScript;
using Syncfusion.JavaScript.Models;

using WebPortal_OH_FI.BL;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;

namespace WebPortal_OH_FI.Models
{
    public class NarrativeViewModel
    {
        public bool CanEdit { get; set; }
        public List<SelectListItem> ValidNarrativeCodes { get; set; }
        public List<NarrativeViewItem> Records { get; set; }
        public List<Column> Columns { get; set; }
        private string Type { get; set; }
        private string TemplateId { get; set; }

        // ^ These columns are syncfusion.javascript.models.columns objects

        public NarrativeViewModel() { }
        public NarrativeViewModel(string[] narrativeTypes)
        {
            HandleType(narrativeTypes);
            CreateColumns();
        }

        private void HandleType(string[] narrativeTypes)
        {
            if (narrativeTypes.Contains("S"))
            {
                TemplateId = "#StandardNarrativeTableHistory";
                Type = "Narrative";
                CanEdit = RateSettingPermissionBL.StandardNarrativeEdit();
            }
            else if (narrativeTypes.Contains("C"))
            {
                TemplateId = "#commentTableHistory";
                Type = "Comment";
                CanEdit = RateSettingPermissionBL.AuditCommentEdit();
            }
            else
            {
                TemplateId = "#detailTableHistory";
                Type = "Detail";
                CanEdit = RateSettingPermissionBL.AuditDetailEdit();
            }
        }

        private void CreateColumns()
        {
            Columns = new List<Column>()
            {
                new Column() { Field = "RecordId", HeaderText = "RecordId", IsPrimaryKey = true, IsIdentity = true, Visible = false},
                new Column() { Field = "IsRestricted", HeaderText = "IsRestricted", Visible = false},
                new Column() { Field = "NarrativeNumber", HeaderText = $"{Type} Number", Priority = 1, Type = Type == "Narrative" ? "number" : "string" },
                new Column() { Field = "NarrativeType", HeaderText = $"{Type} Type", Type = "string", AllowEditing = false },
                new Column() { Field = "NarrativeCode", HeaderText = $"{Type} Code",  Type = "string", AllowEditing = Type == "Narrative" },
                new Column() { Field = "RYBeginYear", HeaderText = "RY Begin Year", Type = "number" },
                new Column() { Field = "RYEndYear", HeaderText = "RY End Year", Type = "number" },
                new Column() { Field = "Description", HeaderText = "Description", Type = "string", Width = 360 },
                new Column() { Field = "ReasonForChange", HeaderText = "Reason for Change" },
                new Column() { Field = "History", HeaderText = "History", AllowEditing = false, AllowFiltering = false, AllowSorting = false, TextAlign = TextAlign.Center, Template = TemplateId }
            };
        }
    }

    public class NarrativeViewItem
    {
        public string RecordId { get; set; }
        public string NarrativeNumber { get; set; }
        public string NarrativeType { get; set; }
        public string NarrativeCode { get; set; }
        public int RYBeginYear { get; set; }
        public int RYEndYear { get; set; }
        public string Description { get; set; }
        public string ReasonForChange { get; set; }
        public bool IsRestricted { get; set; }

        //Paramterless Constructor for when model is sent from client to server
        public NarrativeViewItem() { }

        public NarrativeViewItem(NarrativeTable model)
        {
            RecordId = ClientMapping.MapInternalIdToClientId(model.NarrativeId.ToString(), "narratives");
            NarrativeNumber = model.NarrativeNumber ?? string.Empty;
            NarrativeType = model.NarrativeType?.Type ?? string.Empty;
            NarrativeCode = model.NarrativeCode?.Code ?? string.Empty;
            RYBeginYear = model.CostReportBeginYear;
            RYEndYear = model.CostReportEndYear;
            Description = model.Description ?? string.Empty;
            ReasonForChange = model.ReasonForChange ?? string.Empty;
            IsRestricted = model.IsRestricted;
        }

        public NarrativeTable CreateNarrativeTableEntity()
        {
            string email = RetrieveSession.ARUMUser.Email;
            int dbNarrativeId = 0;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(RecordId, "narratives"), out int i))
                dbNarrativeId = i;

            return new NarrativeTable()
            {
                NarrativeId = dbNarrativeId,
                NarrativeNumber = NarrativeNumber,
                CostReportBeginYear = RYBeginYear,
                CostReportEndYear = RYEndYear,
                Description = Description,
                ReasonForChange = ReasonForChange,
                CreatedBy = email,
                CreatedOn = DateTime.UtcNow,
                LastUpdateBy = email,
                LastUpdatedOn = DateTime.UtcNow
            };
        }
    }

    public class NarrativeHistoryViewItem
    {
        public string RecordId { get; set; }
        public string Action { get; set; }
        public string NarrativeNumber { get; set; }
        public string NarrativeType { get; set; }
        public string NarrativeCode { get; set; }
        public int RYBeginYear { get; set; }
        public int RYEndYear { get; set; }
        public string Description { get; set; }
        public string ReasonForChange { get; set; }
        public string LastUpdatedBy { get; set; }
        public string LastUpdatedOn { get; set; }

        public NarrativeHistoryViewItem(NarrativeTableHistory history)
        {
            RecordId = ClientMapping.MapInternalIdToClientId(history.NarrativeHistoryId.ToString(), "narratives");
            Action = history.Action;
            NarrativeNumber = history.NarrativeNumber;
            NarrativeType = history.NarrativeType?.Type;
            NarrativeCode = history.NarrativeCode?.Code;
            RYBeginYear = history.CostReportBeginYear;
            RYEndYear = history.CostReportEndYear;
            Description = history.Description;
            ReasonForChange = history.ReasonForChange;
            LastUpdatedBy = history.LastUpdatedBy;
            LastUpdatedOn = history.LastUpdatedOn.DateTime.ToString("MM/dd/yyyy");
        }
    }
}