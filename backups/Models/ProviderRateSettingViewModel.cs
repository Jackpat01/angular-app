using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.EntityFrameworkCore.Internal;

using WebPortal_OH_FI.BL;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;

namespace WebPortal_OH_FI.Models
{
    public class ProviderRateSettingViewModel
    {
        public PeriodViewModel Period { get; set; }

        #region Permissions
        public bool CanCreateRates = RateSettingProviderRatesPermissionBL.Create();
        public bool CanFinalizeRates = RateSettingProviderRatesPermissionBL.Finalize(nameof(RateTypeEnumExtended.Standard)); //Used to check if users have access to non-audit finalize perm
        public bool CanFinalizeAuditRates = RateSettingProviderRatesPermissionBL.Finalize(RateSettingProviderRatesPermissionBL.AuditInitialDesc); //Used to check if users have access to audit finalize permi
        public bool CanGenerateRatePackages = RateSettingProviderRatesPermissionBL.GenRatePackage();
        public bool CanGenerateFinalSettlement = RateSettingProviderRatesPermissionBL.GenFinalSettlement();
        public bool CanDelete => RateTypes.Any(x => RateSettingProviderRatesPermissionBL.Delete(x));
        public bool CanCalculate => RateTypes.Any(x => RateSettingProviderRatesPermissionBL.Calculate(x));
        public Dictionary<string, bool> SelectPermissions = RateSettingProviderRatesPermissionBL.GetAllRateTypeSelectPermissions();
        public Dictionary<string, bool> DeletePermissions = RateSettingProviderRatesPermissionBL.GetAllRateTypeDeletePermissions();
        public Dictionary<string, bool> CalculatePermissions = RateSettingProviderRatesPermissionBL.GetAllRateTypeCalculatePermissions();
        public string[] AuditTypes = new string[]
        {
            RateSettingProviderRatesPermissionBL.AuditInitialDesc,
            RateSettingProviderRatesPermissionBL.AuditRevisedDesc,
            RateSettingProviderRatesPermissionBL.AuditFinalDesc
        };

        public string GetRateTypeCheckForFinalizeTemplate(bool invert = false)
        {
            StringBuilder sb = new StringBuilder("(");
            foreach (var permission in SelectPermissions)
            {
                string inverseChar = invert ? "!" : string.Empty;
                bool canFinalize = permission.Value && RateSettingProviderRatesPermissionBL.Finalize(permission.Key);
                sb.Append($"(RateType.replaceAll(' ', '') == '{permission.Key}' && {inverseChar}{canFinalize.ToString().ToLower()})");
                sb.Append(" || ");
            }
            sb.Remove(sb.Length - 4, 4); //This will remove last instance of ' || '
            sb.Append(")");
            return sb.ToString();
        }
        #endregion

        #region Rate Types
        private static List<string> RateTypes = new List<string>()
        {
            RateTypeEnumExtended.GetDescription(RateTypeEnumExtended.NewFacility),
            RateTypeEnumExtended.GetDescription(RateTypeEnumExtended.Standard),
            RateTypeEnumExtended.GetDescription(RateTypeEnumExtended.CHOP),
            RateTypeEnumExtended.GetDescription(RateTypeEnumExtended.CHOP_SPEC),
            RateTypeEnumExtended.GetDescription(RateTypeEnumExtended.AUDIT_INITIAL),
            RateTypeEnumExtended.GetDescription(RateTypeEnumExtended.AUDIT_REVISED),
            RateTypeEnumExtended.GetDescription(RateTypeEnumExtended.AUDIT_FINAL)
        };
        #endregion
    }
}