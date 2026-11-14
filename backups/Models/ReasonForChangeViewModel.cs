using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using MSLC.Portal.MVC.Utility;
using Syncfusion.JavaScript.Models;
using Syncfusion.JavaScript;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;
using System.Web.Mvc;
using WebPortal_OH_FI.BL;

namespace WebPortal_OH_FI.Models
{
    public class ReasonForChangePermissionsModel
    {
        public bool CanEdit = RateSettingPermissionBL.ReasonForChangeEdit();
    }

    public class ReasonForChangeViewModel
    {
        public bool IsAuditManager { get; set; }
        public List<ReasonForChangeViewItem> Records { get; set; }
        public List<Column> Columns { get; set; }

        public ReasonForChangeViewModel(List<AuditType> auditTypes, List<OperationCode> opCodes)
        {
            DropDownListProperties auditTypeDropDown = new DropDownListProperties();
            auditTypeDropDown.DataSource = auditTypes.Where(x => x.Type != "UA") //Remove the unknown type
                .Select(x => new SelectListItem() { Text = x.Type, Value = x.Type });
            DropDownListProperties opTypeDropDown = new DropDownListProperties();
            opTypeDropDown.DataSource = opCodes.Select(x => new SelectListItem() { Text = x.Code, Value = x.Code });

            Columns = new List<Column>()
                {
                    new Column() { Field = "RecordId", HeaderText = "RecordId", IsPrimaryKey = true, IsIdentity = true, Visible = false },
                    new Column() { Field = "IsSeeded", HeaderText = "IsSeeded", Visible = false },
                    new Column() { Field = "Code", HeaderText = "Reason Code" },
                    new Column() { Field = "Name", HeaderText = "Reason Name" },
                    new Column() { Field = "Description", HeaderText = "Reason Description", Width = 325 },
                    new Column() { Field = "OPCode", HeaderText = "Operation Code", EditType = EditingType.DropdownEdit, DropDownEditOptions = opTypeDropDown },
                    new Column() { Field = "AuditType", HeaderText = "Audit Type", EditType = EditingType.DropdownEdit, DropDownEditOptions = auditTypeDropDown },
                    new Column() { Field = "RYBeginYear", HeaderText = "RY Begin Year" },
                    new Column() { Field = "RYEndYear", HeaderText = "RY End Year" },
                    new Column() { Field = "Reason", HeaderText = "Reason for Change" },
                    new Column() { Field = "History", HeaderText = "History", Template = "#reasonForChangeTableHistory", AllowEditing = false, AllowFiltering = false, AllowSorting = false, TextAlign = TextAlign.Center, Width = 75 }
                };
        }
    }

    public class ReasonForChangeViewItem
    {
        public string RecordId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string OPCode { get; set; }
        public string AuditType { get; set; }
        public int RYBeginYear { get; set; }
        public int RYEndYear { get; set; }
        public string Reason { get; set; }
        public bool IsSeeded { get; set; }

        public ReasonForChangeViewItem() { }
        public ReasonForChangeViewItem(ReasonForChange entity)
        {
            RecordId = ClientMapping.MapInternalIdToClientId(entity.ReasonForChangeId.ToString(), "reasonForChange");
            Code = entity.Code ?? string.Empty;
            Name = entity.Name ?? string.Empty;
            Description = entity.Description ?? string.Empty;
            OPCode = entity.OperationCode?.Code ?? string.Empty;
            AuditType = entity.AuditType?.Type ?? string.Empty;
            RYBeginYear = entity.RYBeginYear;
            RYEndYear = entity.RYEndYear;
            Reason = entity.Reason ?? string.Empty;
            IsSeeded = entity.IsSeededRecord;
        }

        public ReasonForChange CreateEntity()
        {
            string email = RetrieveSession.ARUMUser.Email;
            int dbId = 0;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(RecordId, "reasonForChange"), out int i))
                dbId = i;

            return new ReasonForChange()
            {
                ReasonForChangeId = dbId,
                Code = Code,
                Name = Name,
                Reason = Reason,
                Description = Description,
                RYBeginYear = RYBeginYear,
                RYEndYear = RYEndYear,
                CreatedBy = email,
                LastUpdateBy = email,
                CreatedOn = DateTime.UtcNow,
                LastUpdatedOn = DateTime.UtcNow
            };
        }
    }

    public class ReasonForChangeHistoryViewItem
    {
        public string RecordId { get; set; }
        public string Action { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string OPCode { get; set; }
        public string AuditType { get; set; }
        public int RYBeginYear { get; set; }
        public int RYEndYear { get; set; }
        public string Reason { get; set; }
        public string UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }

        public ReasonForChangeHistoryViewItem(ReasonForChangeHistory entity)
        {
            RecordId = ClientMapping.MapInternalIdToClientId(entity.ReasonForChangeHistoryId.ToString(), "reasonForChange");
            Action = entity.Action;
            Name = entity.Name;
            Description = entity.Description;
            OPCode = entity.OperationCode?.Code;
            AuditType = entity.AuditType?.Type;
            RYBeginYear = entity.RYBeginYear;
            RYEndYear = entity.RYEndYear;
            Reason = entity.Reason;
            UpdatedOn = entity.LastUpdatedOn.ToString("yyyy/dd/MM");
            UpdatedBy = entity.LastUpdatedBy;
        }
    }

    public class ReasonForChangeOptionsModel
    {
        public List<SelectListItem> Reasons { get; set; }
        public string SelectedReason { get; set; }

        private const string ReasonMap = "reasonForChangeId";

        public ReasonForChangeOptionsModel(AuditorInfo info, List<ReasonForChange> reasons)
        {
            Reasons = reasons.Select(x => new SelectListItem()
            {
                Text = x.Description,
                Value = ClientMapping.MapInternalIdToClientId(x.ReasonForChangeId.ToString(), ReasonMap)
            }).ToList();
            SelectedReason = ClientMapping.MapInternalIdToClientId(info.ReasonforChange?.ToString(), ReasonMap);
        }
    }
}