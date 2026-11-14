using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using MSLC.Portal.MVC.Utility;

using Syncfusion.JavaScript;
using Syncfusion.JavaScript.Models;

using WebPortal_OH_FI.ProjectSpecific.Data.Models.user;

namespace WebPortal_OH_FI.Models
{
    public class UsersListViewModel
    {
        public List<UsersViewModel> Users { get; set; }
        public List<SelectItemList> UserTypes { get; set; }
        public string UserTypeId { get; set; }
        public bool CanEdit { get; set; }
        public List<Column> Columns
        {
            get
            {
                return new List<Column>()
                {
                    new Column() { Field = "UserId", HeaderText = "UsesrId", IsPrimaryKey = true, IsIdentity = true, Visible = false},
                    new Column() { Field = "Mnemonic", HeaderText = "User Type"},
                    new Column() { Field = "FirstName", HeaderText = "First Name"},
                    new Column() { Field = "LastName", HeaderText = "Last Name"},
                    new Column() { Field = "Email", HeaderText = "Email"},
                    new Column() { Field = "PhoneNumber", HeaderText = "Phone Number"},
                    new Column() { Field = "ActiveFlag", HeaderText = "Active Flag"},
                    new Column() { Field = "LegacyStaffId", HeaderText = "Auditor Staff Id", AllowEditing = false},
                    new Column() { Field = "CreatedBy", HeaderText = "Created By", AllowEditing = false},
                    new Column() { Field = "CreatedOn", HeaderText = "Created On", AllowEditing = false},
                    new Column() { Field = "History", HeaderText = "History", Template = "#auditStaffhistory", AllowEditing = false, AllowSorting = false, AllowFiltering = false, TextAlign = TextAlign.Center, }
                };
            }
        }
    }

    public class UsersViewModel
    {
        public string UserId { get; set; }
        public string Mnemonic { get; set; }
        public int LegacyStaffId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ActiveFlag { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }

        public UsersViewModel() { }

        public UsersViewModel(AuditUser user)
        {
            UserId = ClientMapping.MapInternalIdToClientId(user.AuditUserId.ToString(), "users");
            Mnemonic = user.UserType;
            LegacyStaffId = user.AuditUserId;
            Email = user.Email;
            PhoneNumber = user.PhoneNumber;
            FirstName = user.FirstName;
            LastName = user.LastName;
            ActiveFlag = user.ActiveFlag == true ? "Yes" : "No";
            LegacyStaffId = user.AuditorStaffId;
            CreatedBy = user.CreatedBy;
            CreatedOn = user.CreatedOn.ToString("MM/dd/yyyy");
        }

        public AuditUser CreateAuditUserEntity()
        {
            string email = RetrieveSession.ARUMUser.Email;
            int dbId = 0;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(UserId, "users"), out int i))
                dbId = i;

            return new AuditUser
            {
                AuditUserId = dbId,
                UserType = Mnemonic,
                FirstName = FirstName,
                LastName = LastName,
                PhoneNumber = PhoneNumber,
                Email = Email,
                AuditorStaffId = LegacyStaffId,
                ActiveFlag = ActiveFlag == "Yes" ? true : false,
                CreatedBy = email,
                CreatedOn = DateTime.UtcNow,
                LastUpdateBy = email,
                LastUpdatedOn = DateTime.UtcNow,
            };
        }
    }

    public class UsersHistoryViewModel
    {
        public string UserHistoryId { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public string Mnemonic { get; set; }
        public int LegacyStaffId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ActiveFlag { get; set; }
        public string LastUpdatedOn { get; set; }
        public string LastUpdateBy { get; set; }

        public UsersHistoryViewModel() { }

        public UsersHistoryViewModel(AuditUserHistory history)
        {
            UserHistoryId = ClientMapping.MapInternalIdToClientId(history.AuditUserHistoryId.ToString(), "users");
            UserId = ClientMapping.MapInternalIdToClientId(history.AuditUserId.ToString(), "users");
            Action = history.Action;
            Mnemonic = history.UserType;
            FirstName = history.FirstName;
            LastName = history.LastName;
            LegacyStaffId = history.AuditStaffId;
            Email = history.Email;
            PhoneNumber = history.PhoneNumber;
            ActiveFlag = history.ActiveFlag ? "Yes" : "No";
            LastUpdateBy = history.LastUpdateBy;
            LastUpdatedOn = history.LastUpdatedOn.ToString("MM/dd/yyyy");
        }
    }

    public class SelectItemList
    {
        public string value { get; set; }
        public string text { get; set; }
    }
}