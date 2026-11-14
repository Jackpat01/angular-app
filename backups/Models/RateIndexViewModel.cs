using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using WebPortal_OH_FI.BL;

namespace WebPortal_OH_FI.Models
{
    public class RateIndexViewModel
    {
        public bool CanViewAnnualRateSetting = RateSettingPermissionBL.AnnualRateSettingEdit();
        public bool CanViewAdminLicense = RateSettingPermissionBL.AdminLicenseView();
        public bool CanViewCRStaff = RateSettingPermissionBL.CRStaffView();
        public bool CanViewAuditStaff = RateSettingPermissionBL.CRAuditStaffView();
        public bool CanViewStandardNarrative = RateSettingPermissionBL.StandardNarrativeView();
        public bool CanViewAuditComment = RateSettingPermissionBL.AuditCommentView();
        public bool CanViewAuditDetail = RateSettingPermissionBL.AuditDetailView();
        public bool CanViewNarrativeCode = RateSettingPermissionBL.NarrativeCodeView();
        public bool CanViewReasonForChange = RateSettingPermissionBL.ReasonForChangeView();
        public bool CanViewOperationCode = RateSettingPermissionBL.OperationCodeView();
    }
}