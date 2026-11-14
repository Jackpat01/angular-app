using MSLC.Portal.MVC.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using Syncfusion.JavaScript;
using Syncfusion.JavaScript.Models;


using WebPortal_OH_FI.BL;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.crdata;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.user;
using WebPortal_OH_FI.ProjectSpecific.Data.Seed;

namespace WebPortal_OH_FI.Models
{
    public class ProviderViewModel  // Parent for all tabs
    {
        public List<SelectListItem> ProviderFilterType { get; set; }
        public int FilterTypeId { get; set; }
        public string ProviderType { get; set; }
        public DateTime ProviderDate { get; set; }
        public int TimeZoneOffset { get; set; }
        public List<ProviderInfoViewModel> ProviderInfos { get; set; }

        public List<ProviderRatesViewModel> ProviderRatesViewModel { get; set; }
    }

    public class ProviderInfoViewModel
    {
        public string ProviderName { get; set; }
        public string ProviderNumber { get; set; }
        public string ProviderMedicaidNumber { get; set; }
        public string ProviderMedicareNumber { get; set; }
        public string NPINumber { get; set; }
        public string FiscalYearEnd { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ClosureDate { get; set; }
        public bool CanViewRates = RateSettingProviderRatesPermissionBL.View();
    }

    public class ProviderGeneralInformationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProviderNumber { get; set; }
        public string NPINumber { get; set; }
        public string StateIdNumber { get; set; }
        public string Status { get; set; }
        public string EffectiveDate { get; set; }
        public string MedicaidID { get; set; }
        public string MedicareID { get; set; }
        public string FacilityType { get; set; }
        public string FacilitySubType { get; set; }
        public string FederalTaxId { get; set; }
        public string County { get; set; }
        public string ODHDistrict { get; set; }
        public string ODHID { get; set; }
        public string EntityID { get; set; }
        public string ODJFSDistrict { get; set; }
        public string MRDDNumber { get; set; }
        public string HomeMedicaidId { get; set; }
        public string ControlType { get; set; }
        public string RiskStatus { get; set; }
        public string PracticeType { get; set; }
        public string DirectCarePeerGroup { get; set; }
        public string IndirectCarePeerGroup { get; set; }
        public string CapitalPeerGroup { get; set; }
        public int CertifiedBeds { get; set; }
        public int LicensedBeds { get; set; }
        public ICollection<ProviderAttributeViewModel> ProviderAttributes { get; set; } = new HashSet<ProviderAttributeViewModel>();
        public string PeerGroup { get; set; }
        public string AncillaryPeerGroup { get; set; }
        public string Ownership { get; set; }
        public string CHOPFromProviderNumber { get; set; }
        public string CHOPToProviderNumber { get; set; }
        public string ClosureDate { get; set; }
    }


    public class ProviderAttributeViewModel
    {
        public string ProviderAttributeId { get; set; }
        public string Name { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ProviderId { get; set; }

        public string _dateRange;
        public string DateRange
        {
            get
            {
                if (StartDate != DateTime.MinValue && EndDate != DateTime.MinValue)
                    _dateRange = StartDate?.ToShortDateString() + " - " + EndDate?.ToShortDateString();
                else if (StartDate != DateTime.MinValue && EndDate == DateTime.MinValue)
                    _dateRange = StartDate?.ToShortDateString();
                else
                    _dateRange = string.Empty;
                return _dateRange;
            }
            set
            {
                _dateRange = value;
            }
        }
        public bool IsEnabled { get; set; }

    }

    public class ProviderRatesViewModel //Rates
    {
        public int ProviderID { get; set; }
        public string EffectiveDate { get; set; }
        public string RunDate { get; set; }
        public List<SelectListItem> RateEffectiveDateList { get; set; }
        public List<ProviderRatesInfoViewModel> ProviderRatesInfoViewModel { get; set; }
    }

    public class ProviderRatesInfoViewModel
    {
        //public string DocumentID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string DisplayName { get; set; }
        public int Sequence { get; set; }
    }

    public class ProviderCostReportViewModel
    {
        public int ProviderCostReportId { get; set; }

        public bool IsCostReportLocked { get; set; }
        public List<SelectListItem> CostReportPeriod { get; set; }
        public List<SelectListItem> CostReportVersion { get; set; }
        //public List<ProviderFiscalYearViewModel> ProviderFiscalYear { get; set; }
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
        public string DueDate { get; set; }
        public string CostReportStatusId { get; set; }
        public string ExaminerUserId { get; set; }
        public string ReviewerUserId { get; set; }
        public List<SelectListItem> CostReportStatus { get; set; }
        public PeriodViewModel PeriodViewModel { get; set; }
        public UsersViewModel UsersViewModel { get; set; }
        public List<SelectListItem> ExaminerListItem { get; set; }
        public List<SelectListItem> ReviewerListItem { get; set; }
        public List<ProviderCostReportStatlogViewModel> ProviderCostReportStatlogList { get; set; }
        public bool CanViewStatlog { get; set; }
        public bool CanEditStatlog { get; set; }
        public bool CanViewAudit = RateSettingProviderCostReportPermissionBL.AuditView();
        public bool CanViewInfo = RateSettingProviderCostReportPermissionBL.InfoView();
        public bool CanEditInfo = RateSettingProviderCostReportPermissionBL.InfoEdit();

        public void SetPermissions(string crType)
        {
            CanViewStatlog = RateSettingProviderCostReportPermissionBL.StatlogView(crType);
            CanEditStatlog = RateSettingProviderCostReportPermissionBL.StatlogEdit(crType);
        }
    }

    public class AuditCostReportViewModel
    {
        public bool CanEditAudit { get; private set; }
        public bool CanViewStatlog { get; private set; }
        public bool CanEditStatlog { get; private set; }
        public bool IsCostReportLocked { get; set; }
        public bool IsContractAudit { get; set; }

        public string CompletionDate { get; set; }
        
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
        public string ContractorName { get; set; }
        public string CostReportStatusId { get; set; }
        public string CostReportStatusName { get; set; }
        public string ProviderCostReportId { get; set; }
        public string AuditTypeId { get; set; }
        public string ReasonForChangeId { get; set; }

        public string Auditor1Id { get; set; }
        public string Auditor2Id { get; set; }
        public string Auditor1Name { get; set; }
        public string Auditor2Name { get; set; }
        public string PeerAuditorId { get; set; }
        public string PeerAuditorName { get; set; }
        public string FieldAuditor1Id { get; set; }
        public string FieldAuditor2Id { get; set; }
        public string FieldAuditor3Id { get; set; }
        public string FieldAuditor4Id { get; set; }
        public string FieldAuditor5Id { get; set; }
        public string FieldAuditor6Id { get; set; }
        public string FieldAuditor1Name { get; set; }
        public string FieldAuditor2Name { get; set; }
        public string FieldAuditor3Name { get; set; }
        public string FieldAuditor4Name { get; set; }
        public string FieldAuditor5Name { get; set; }
        public string FieldAuditor6Name { get; set; }
        public string ReasonForChangeComment { get; set; }

        public List<SelectListItem> CostReportStatus { get; set; }
        public List<SelectListItem> ActiveAuditUsers { get; set; }
        public List<SelectListItem> AuditTypes { get; set; }
        public List<SelectListItem> ReasonsForChange { get; set; }

        public List<ProviderCostReportStatlogViewModel> ProviderCostReportStatlogList { get; set; }

        private static string CRStatusMap = "costreportstatus";
        private static string CRIdMap = "costreport";
        private static string AuditUserMap = "auditUserId";
        private static string AuditTypeMap = "auditTypeId";
        private static string ReasonMap = "reasonForChangeId";

        public AuditCostReportViewModel() 
        {
            CostReportStatus = new List<SelectListItem>();
            ActiveAuditUsers = new List<SelectListItem>();
            AuditTypes = new List<SelectListItem>();
            ReasonsForChange = new List<SelectListItem>();
        }
        public AuditCostReportViewModel(AuditorInfo entity, ReasonForChangeComment comm, ProviderCostReport cr, List<AuditUser> users, List<AuditType> types, List<ReasonForChange> reasons, List<ProviderCostReportStatlog> statlogs)
        {
            IsCostReportLocked = cr.LockedOn != null;
            IsContractAudit = entity.IsContractAudit ?? false;

            BeginDate = cr.PeriodBegin.ToShortDateString();
            EndDate = cr.PeriodEnd.ToShortDateString();
            ContractorName = entity.ContractorName;
            CostReportStatusId = ClientMapping.MapInternalIdToClientId(cr.CostReportStatusId?.ToString(), CRStatusMap);
            CostReportStatusName = cr?.CostReportStatus?.Status;
            ProviderCostReportId = ClientMapping.MapInternalIdToClientId(cr.ProviderCostReportId.ToString(), CRIdMap);
            AuditTypeId = ClientMapping.MapInternalIdToClientId(entity.AuditTypeId?.ToString(), AuditTypeMap);
            ReasonForChangeId = ClientMapping.MapInternalIdToClientId(entity.ReasonforChange?.ToString(), ReasonMap);
            Auditor1Id = ClientMapping.MapInternalIdToClientId(entity.AuditorAssigned1?.ToString(), AuditUserMap);
            Auditor2Id = ClientMapping.MapInternalIdToClientId(entity.AuditorAssigned2?.ToString(), AuditUserMap);
            PeerAuditorId = ClientMapping.MapInternalIdToClientId(entity.PeerReviewAuditorId?.ToString(), AuditUserMap);
            FieldAuditor1Id = ClientMapping.MapInternalIdToClientId(entity.FieldAuditorAssigned1?.ToString(), AuditUserMap);
            FieldAuditor2Id = ClientMapping.MapInternalIdToClientId(entity.FieldAuditorAssigned2?.ToString(), AuditUserMap);
            FieldAuditor3Id = ClientMapping.MapInternalIdToClientId(entity.FieldAuditorAssigned3?.ToString(), AuditUserMap);
            FieldAuditor4Id = ClientMapping.MapInternalIdToClientId(entity.FieldAuditorAssigned4?.ToString(), AuditUserMap);
            FieldAuditor5Id = ClientMapping.MapInternalIdToClientId(entity.FieldAuditorAssigned5?.ToString(), AuditUserMap);
            FieldAuditor6Id = ClientMapping.MapInternalIdToClientId(entity.FieldAuditorAssigned6?.ToString(), AuditUserMap);

            CompletionDate = entity.CompletionDate?.ToString("yyyy-MM-dd");
            Auditor1Name = $"{entity.Auditor1?.FirstName} {entity.Auditor1?.LastName}";  
            Auditor2Name = $"{entity.Auditor2?.FirstName} {entity.Auditor2?.LastName}";
            PeerAuditorName = $"{entity.PeerReviewAuditor?.FirstName} {entity.PeerReviewAuditor?.LastName}";
            FieldAuditor1Name = $"{entity.FieldAuditor1?.FirstName} {entity.FieldAuditor1?.LastName}";
            FieldAuditor2Name = $"{entity.FieldAuditor2?.FirstName} {entity.FieldAuditor2?.LastName}";
            FieldAuditor3Name = $"{entity.FieldAuditor3?.FirstName} {entity.FieldAuditor3?.LastName}";
            FieldAuditor4Name = $"{entity.FieldAuditor4?.FirstName} {entity.FieldAuditor4?.LastName}";
            FieldAuditor5Name = $"{entity.FieldAuditor5?.FirstName} {entity.FieldAuditor5?.LastName}";
            FieldAuditor6Name = $"{entity.FieldAuditor6?.FirstName} {entity.FieldAuditor6?.LastName}";
            ReasonForChangeComment = comm != null ? comm.Comment : string.Empty;

            CanEditAudit = RateSettingProviderCostReportPermissionBL.AuditEdit();
            CanViewStatlog = RateSettingProviderCostReportPermissionBL.StatlogView(cr.CostReportTypeCode);
            CanEditStatlog = RateSettingProviderCostReportPermissionBL.StatlogEdit(cr.CostReportTypeCode);

            InitializeLists(users, types, reasons, statlogs);
        }

        public void InitializeLists(List<AuditUser> users, List<AuditType> types, List<ReasonForChange> reasons, List<ProviderCostReportStatlog> statlogs)
        {
            CostReportStatus = new List<SelectListItem>(); // This is populated elsewhere

            ActiveAuditUsers = users.Select(x => new SelectListItem()
            {
                Text = $"{x.FirstName} {x.LastName}",
                Value = ClientMapping.MapInternalIdToClientId(x.AuditUserId.ToString(), AuditUserMap)
            }).ToList();
            AuditTypes = types.Select(x => new SelectListItem()
            {
                Text = x.Description,
                Value = ClientMapping.MapInternalIdToClientId(x.AuditTypeId.ToString(), AuditTypeMap)
            }).ToList();
            ReasonsForChange = reasons.Select(x => new SelectListItem()
            {
                Text = x.Description,
                Value = ClientMapping.MapInternalIdToClientId(x.ReasonForChangeId.ToString(), ReasonMap)
            }).ToList();
            ProviderCostReportStatlogList = statlogs.Select(x => new ProviderCostReportStatlogViewModel(x, CanEditStatlog)).ToList();
        }

        public AuditorInfo GetEntity()
        {
            int dbproviderCostReportId = 0;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(ProviderCostReportId, CRIdMap), out int i))
                dbproviderCostReportId = i;
            
            int? reasonForChangeId = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(ReasonForChangeId, ReasonMap), out int changeId))
                reasonForChangeId = changeId;

            int? auditTypeId = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(AuditTypeId, AuditTypeMap), out int a1))
                auditTypeId = a1;

            int? audit1Id = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(Auditor1Id, AuditUserMap), out int a2))
                audit1Id = a2;
            int? audit2Id = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(Auditor2Id, AuditUserMap), out int p))
                audit2Id = p;
            int? peerId = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(PeerAuditorId, AuditUserMap), out int f1))
                peerId = f1;
            int? field1Id = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(FieldAuditor1Id, AuditUserMap), out int f2))
                field1Id = f2;
            int? field2Id = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(FieldAuditor2Id, AuditUserMap), out int f3))
                field2Id = f3;
            int? field3Id = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(FieldAuditor3Id, AuditUserMap), out int f4))
                field3Id = f4;
            int? field4Id = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(FieldAuditor4Id, AuditUserMap), out int f5))
                field4Id = f5;
            int? field5Id = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(FieldAuditor5Id, AuditUserMap), out int f6))
                field5Id = f6;
            int? field6Id = null;
            if (int.TryParse(ClientMapping.GetInternalIdFromClientId(FieldAuditor6Id, AuditUserMap), out int f7))
                field6Id = f7;

            DateTime? completionDate = null;
            if (DateTime.TryParse(CompletionDate, out DateTime date))
                completionDate = date;

            return new AuditorInfo()
            {
                CRID = dbproviderCostReportId,
                IsContractAudit = IsContractAudit,
                ContractorName = ContractorName,
                AuditorAssigned1 = audit1Id,
                AuditorAssigned2 = audit2Id,
                PeerReviewAuditorId = peerId,
                FieldAuditorAssigned1 = field1Id,
                FieldAuditorAssigned2 = field2Id,
                FieldAuditorAssigned3 = field3Id,
                FieldAuditorAssigned4 = field4Id,
                FieldAuditorAssigned5 = field5Id,
                FieldAuditorAssigned6 = field6Id,
                CompletionDate = completionDate,
                AuditTypeId = auditTypeId,
                ReasonforChange = reasonForChangeId
            };
        }
    }

    public class ProviderCostReportStatlogViewModel
    {
        public string ProviderCostReportStatlogId { get; set; }
        public string CRID { get; set; }
        public string EventDate { get; set; }
        public DateTime Stamp { get; set; }
        public string EventName { get; set; }
        public string Notes { get; set; }
        public string UserId { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string LastUpdatedOn { get; set; }
        public string LastUpdateBy { get; set; }
        public bool CanEdit { get; set; } //This lets the template know how to render each record out

        private static string StatlogMap = "costreportstatlog";
        private static string CRMap = "costreport";

        public ProviderCostReportStatlogViewModel()
        {

        }

        public ProviderCostReportStatlogViewModel(ProviderCostReportStatlog statlog, bool canEdit)
        {
            ProviderCostReportStatlogId = ClientMapping.MapInternalIdToClientId(statlog.ProviderCostReportStatlogId.ToString(), StatlogMap);
            CRID = ClientMapping.MapInternalIdToClientId(statlog.CRID.ToString(), CRMap);
            EventDate = statlog.EventDate.ToString("MM/dd/yyyy");
            EventName = statlog.EventName;
            UserId = statlog.UserId;
            Notes = statlog.Notes;
            CanEdit = canEdit;
        }

    }

    public class ProviderCostReportReviewViewModel
    {
        public string ProviderCostReportReviewId { get; set; }
        public string ProviderCostReportStatlogId { get; set; }
        public string RevDate { get; set; }
        public string RevNotes { get; set; }
        public string RevUserId { get; set; }
        public DateTime RevStamp { get; set; }
    }

    //public class ProviderFiscalYearViewModel
    //{
    //    public int Id { get; set; }
    //    public DateTime BeginDate { get; set; }
    //    public DateTime EndDate { get; set; }

    //    public string _dateRange;
    //    public string DateRange
    //    {
    //        get
    //        {
    //            if (BeginDate != DateTime.MinValue && EndDate != DateTime.MinValue)
    //                _dateRange = BeginDate.ToShortDateString() + " - " + EndDate.ToShortDateString();
    //            else if (BeginDate != DateTime.MinValue && EndDate == DateTime.MinValue)
    //                _dateRange = BeginDate.ToShortDateString();
    //            else
    //                _dateRange = string.Empty;
    //            return _dateRange;
    //        }
    //        set
    //        {
    //            _dateRange = value;
    //        }
    //    }
    //}

    public class UserViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Initials { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }

    public class DatabaseLookupGroupViewModel
    {
        public object DataModel { get; set; }
        public List<Column> GridColumns { get; set; }
        public List<ColumnValidations> GridColumnValidations { get; set; }
        public DateTime LookupGroupDate { get; set; }
        public int LookupGroupYear { get; set; }
        public List<SelectListItem> LookupGroupFiscalYears { get; set; }
        public int LookupGroupFiscalYear { get; set; }
        public string AnnualRateSettingEnum { get; set; }

        public string HeaderText { get; set; }
        public string DateText { get; set; }
        public bool IsUserAdmin { get; set; }

        #region Validation Constants
        public bool ProviderNumberRequired { get; set; } = true;
        public int ProviderNumberMinLength { get; set; } = 7;
        public int ProviderNumberMaxLength { get; set; } = 7;
        public int CommentsMaxLength { get; set; } = 100;
        #endregion
    }

    public class DatabaseLookupViewModel
    {
        public int FiscalYear { get; set; }

        public int RedDateYear { get; set; }

        public List<SelectListItem> RedDateList { get; set; }

        public List<DatabaseLookupGridViewModel> DatabaseLookupGridViewModelList { get; set; }

        public string AnnualRateSettingEnum { get; set; }

        public string HeaderText { get; set; }
        public string DateText { get; set; }

        public int McaidLength { get; set; } = 7;

    }

    // grid data
    public class DatabaseLookupGridViewModel
    {
        public string DatabaseLookupVariableId { get; set; }

        public int? VersionVGS { get; set; }

        public string ValueType { get; set; }

        public string DisplayName { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int RangeMinimum { get; set; }

        public int RangeMaximum { get; set; }

    }

    public class DatabaseLookupItemViewModel
    {
        public DateTime LookUpDate { get; set; }
        public string ValueType { get; set; }
        public string VariableName { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
    }

    public class QualityPointsRateSch6ResultViewModel
    {
        public string EffectiveDate { get; set; }
        public string ProviderNumber { get; set; }
        public string PressureUlcers { get; set; }

        public string UrinaryTractInfection { get; set; }

        public string IndependentMovement { get; set; }

        public string Catheter { get; set; }

        public string Reason { get; set; }

        public string Occupancy { get; set; }

        public string LSDI { get; set; }

        public string LSFI { get; set; }

        public string LSAM { get; set; }

        public string AdjustedStaffing { get; set; }

    }

    public class QualityPointsRateSch5ResultViewModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string QTY_POINT_1 { get; set; }

        public string QTY_POINT_2 { get; set; }

        public string QTY_POINT_3 { get; set; }

        public string QTY_POINT_4 { get; set; }

        public string QTY_POINT_5 { get; set; }
        public string QTY_POINT_6 { get; set; }
        public string QTY_POINT_7 { get; set; }

        public string Reason { get; set; }

    }

    public class QualityPointsResultViewModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string QualityPoints { get; set; }
        public string Reason { get; set; }

    }

    public class CaseMixScoresResultViewModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixValue { get; set; }
        public string MDSScoreType { get; set; }
        public string Comments { get; set; }
        public string Reason { get; set; }

    }

    public class QuarterlyCaseMixScoresPDPMResultViewModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixValue { get; set; }
        public string TotalFacilityCaseMixValue { get; set; }
        public string MDSScoreType { get; set; }
        public string Comments { get; set; }
        public string Reason { get; set; }

    }

    public class QuarterlyCaseMixScoresSFYResultViewModel
    {
        public string SFY { get; set; }
        public string ProviderNumber { get; set; }
        public string EffectiveDate { get; set; }
        public string CaseMixValue { get; set; }
        public string ScoreType { get; set; }
        public string Comments { get; set; }
        public string Reason { get; set; }

    }

    public class AnnualCaseMixScoresResultViewModel
    {
        public string CYE { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixValue { get; set; }
        public string MDSScoreType { get; set; }
        public string Comments { get; set; }
        public string Reason { get; set; }

    }

    public class DirectCareRateFreezeProvidersResultViewModel
    {
        public string EffectiveDate { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixScore { get; set; }
        public string DirectCareRate { get; set; }

        public string Reason { get; set; }
    }

    public class CHOWQualityPaymentOverrideResultViewModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string Comments { get; set; }
        public string Reason { get; set; }
    }

    public class ProviderLowOccupancyOverrideResultViewModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string Comments { get; set; }
        public string Reason { get; set; }
    }

    public class ProviderTaxOverrideResultViewModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string TaxRate { get; set; }
        public string Comments { get; set; }
        public string Reason { get; set; }
    }

    public class CorpOfficeSalaryCeilingTablesResultViewModel
    {
        public string CYE { get; set; }
        public string General_Classification { get; set; }
        public string Classification_Title { get; set; }
        public string Classification_Number { get; set; }
        public string Bed_Size_Min { get; set; }
        public string POSITION_NUM { get; set; }
        public string Step_1 { get; set; }
        public string Step_2 { get; set; }
        public string Step_3 { get; set; }
        public string Step_4 { get; set; }
        public string Step_5 { get; set; }
        public string Step_6 { get; set; }
        public string Step_7 { get; set; }
        public string Step_8 { get; set; }
        public string Step_9 { get; set; }
        public string Reason { get; set; }
    }

    public class DataBaseLookupGroupResultViewModel
    {
        public object GroupDataTable { get; set; }
        public List<Column> GridColumns { get; set; }
    }

    public class AddressFullViewModel
    {
        public string AddressId { get; set; }
        public string AddressType { get; set; }
        public string AddressName { get; set; }
        public string AddressLineOne { get; set; }
        public string AddressLineTwo { get; set; }

        public string AddressCity { get; set; }

        public string AddressState { get; set; }

        public string AddressZipCode { get; set; }

        public string AddressCounty { get; set; }

        public string AddressPhone { get; set; }

        public string AddressFax { get; set; }

        public string AddressEmail { get; set; }

        public string AddressContactPerson { get; set; }
    }

    public class ProviderAddressViewModel
    {
        public AddressFullViewModel CH119Address { get; set; }

        public string ProviderName { get; set; }

        public List<SelectListItem> AddressTypes { get; set; } = new List<SelectListItem>();

        public List<LetterAddressType> LetterAddressType { get; set; } = new List<LetterAddressType>();

        public int AddressType { get; set; }
    }

    // Grid
    public class ProviderLetterAddressViewModel
    {
        public int PrimaryKeyId { get; set; }
        public string ProviderLetterAddressId { get; set; }
        public string LetterType { get; set; }

        public string Name { get; set; }

        public AddressViewModel Address { get; set; }

        public string AddressType { get; set; }
    }

    public class LetterAddressProviderViewModel
    {
        public int ProviderLetterAddressId { get; set; }

        public int ProviderId { get; set; }

        public string LetterTypeId { get; set; }

        public string AddressTypeId { get; set; }
    }

    public class LetterAddressType
    {
        public string Text { get; set; }

        public string Value { get; set; }
    }

    public class LetterAddressViewModel
    {
        public List<SelectListItem> AddressTypes { get; set; }

        public string LetterAddress { get; set; }

        public string ProviderNumber { get; set; }

        public AddressViewModel AddressViewModel { get; set; }

    }

    public class AddressViewModel
    {
        public string AddressTypeId { get; set; }
        public string Name { get; set; }

        public string AddressLineOne { get; set; }

        public string AddressLineTwo { get; set; }

        public string AddressCity { get; set; }

        public string AddressState { get; set; }

        public string AddressZipCode { get; set; }

        public string AddressCounty { get; set; }

    }

    public class CertifiedBedTransfersViewModel
    {
        public string CertifiedBedTransfersId { get; set; }

        public string ProviderId { get; set; }

        public string ProviderFromNumber { get; set; }

        public string EffectiveDate { get; set; }

        public int CertifiedBedsTransferred { get; set; }

        public string ProviderFromName { get; set; }

        public string Comments { get; set; }

        public string BedsOnHold { get; set; }
    }
    public class CertifiedBedTransfersProvidersViewModel
    {
        public string ProviderId { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
    }
    
    public class LicensedBedsViewModel
    {
        public string LicensedBedsId { get; set; }
        public string ProviderId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Status { get; set; }
        public int BedCountNF { get; set; }
        public int BedCountRegisteredHospital { get; set; }
        public int BedCountOther { get; set; }
        public int BedCountICF { get; set; }
        public int TotalLicBeds { get; set; }
        public int TotalProjBeds { get; set; } // NF and ICF Database differences
        public int ResidentialCare { get; set; }

        public int OutOfService { get; set; }
        public int OnHold { get; set; }
        public string Comments { get; set; }
        public int TimeZoneOffset => TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Hours;
    }
    public class CertifiedBedsViewModel
    {
        public string CertifiedBedsId { get; set; }
        public string ProviderId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Status { get; set; }
        public int MedicaidNF { get; set; } // For NF and ICF Databases
        public int DualSNFNF { get; set; }
        public int Total { get; set; }
        public string Comments { get; set; }
        public int TimeZoneOffset => TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Hours;
    }

    public class CHOPViewModel
    {
        public int ChowReviewID { get; set; }

        public int ProviderId { get; set; }

        public string Sale { get; set; }

        public string EffectiveDate { get; set; }

        public string TransactionType { get; set; }

        public string PurchaseAmount { get; set; }

        public string MasterLeaseAmount { get; set; }

        public string SubLeaseAmount { get; set; }

        public string Comments { get; set; }

        public CHOPAddressViewModel EnteringAddress { get; set; }
        public CHOPAddressViewModel ExitingAddress { get; set; }
    }

    public class CHOPAddressViewModel
    {
        public string OtherProviderNumber { get; set; }
        public string OrganizationName { get; set; }
        public string ContactPerson { get; set; }
        public string AddressLineOne { get; set; }
        public string AddressLineTwo { get; set; }
        public string AddressCity { get; set; }
        public string AddressState { get; set; }
        public string AddressZip { get; set; }
        public string AddressCounty { get; set; }
        public string AddressPhone { get; set; }
        public string AddressFax { get; set; }
        public string AddressEmail { get; set; }
    }
    public class CostReportDataWizardViewModel
    {
        public List<DataWizardProviderViewModel> Providers { get; set; }
        public List<DataWizardProviderViewModel> SelectedProviders { get; set; }

        public string ReportName { get; set; }

        public List<SelectListItem> CostReportYears { get; set; }
        public int CostReportYear { get; set; }
        public List<SelectListItem> CostReportLengths { get; set; }
        public string CostReportLength { get; set; }
        public List<SelectListItem> CostReportTypes { get; set; }
        public int CostReportType { get; set; }
        public List<SelectListItem> CostReportStatuses { get; set; }
        public int CostReportStatus { get; set; }

        public List<DataPointsReportsViewModel> DataPointsReportsViewModel { get; set; }
    }

    public class DataWizardProviderViewModel
    {
        public string ProviderValue { get; set; }

        public string ProviderName { get; set; }
        // This could be deleted after the DP dev is complete
        // Might be required for other operations within the Grid
        public bool IsSelected { get; set; }

    }

    public class DataPointsManager : DataManager
    {
        public string Year { get; set; }
    }

    public class DataPointsViewModel
    {
        public string ID { get; set; }

        public string parentID { get; set; }

        public string Text { get; set; }

        public bool isChecked { get; set; }

        public string RangeName { get; set; }

        public bool hasChild;

        public int? ParentValue { get; set; }

        public int? DisplayColumn { get; set; }
        public bool isIndeterminate { get; set; }
        public string ScheduleName { get; set; }
    }

    public class SelectedDataPointsTreeViewModel
    {
        public int sequenceId { get; set; }
        public string id { get; set; }
        public string parentId { get; set; }
        public string text { get; set; }
        public bool? hasChild { get; set; }
        public bool? expanded { get; set; }
        public bool? ischecked { get; set; }
        public bool? selected { get; set; }
        public string spriteCss { get; set; }
    }

    public class BaseYearViewModel
    {
        public int FiscalYear { get; set; }

        public int NewYear { get; set; }

        public List<SelectListItem> FiscalYearList { get; set; }

        public string BaseYearTypeEnum { get; set; }

        public string HeaderText { get; set; }
        public bool AllowExcelExport { get; set; }
        public string DateType { get; set; }

    }

    public class BaseYearDGViewModel
    {
        public int FiscalYear { get; set; }
        public DateTime EffectiveDate { get; set; }
        public int NewYear { get; set; }

        public List<SelectListItem> FiscalYearList { get; set; }
        public object DataModel { get; set; }
        public List<Column> GridColumns { get; set; }
        public List<ColumnValidations> GridColumnValidations { get; set; }

        public string BaseYearTypeEnum { get; set; }

        public string HeaderText { get; set; }
        public bool AllowExcelExport { get; set; }
        public string DateType { get; set; }
    }

    public class BaseYearLookupViewModel
    {
        public int FiscalYear { get; set; }
        public int NewYear { get; set; }

        public List<SelectListItem> FiscalYearList { get; set; }
        public object DataModel { get; set; }
        public List<Column> GridColumns { get; set; }
        public List<ColumnValidations> GridColumnValidations { get; set; }

        public string BaseYearTypeEnum { get; set; }

        public string HeaderText { get; set; }
        public bool AllowExcelExport { get; set; }
        public string DateType { get; set; }
    }

    public class ColumnValidations
    {
        public int Index { get; set; }
        public int MaxLength { get; set; } = -1;
        public int MinLength { get; set; } = -1;
        public bool IsRequired { get; set; } = false;
        public bool ValidateOnEntry { get; set; } = false;
        public int DecimalPlaces { get; set; } = 0;
        public string Format { get; set; } = string.Empty;
    }

    // grid data
    public class BaseYearGridViewModel
    {
        public string MedianDataId { get; set; }
        public string PeerGroup { get; set; }
        public string MedianName { get; set; }
        public string MedianValue { get; set; }

    }

    public class QualityCalculationListViewModel
    {
        public DateTime EffectiveDate { get; set; }
        public List<SelectListItem> EffectiveDateList { get; set; }
        public string QualityCalculationTypeEnum { get; set; }
    }

    public class DataPointsReportsViewModel
    {
        public string ReportId { get; set; }
        public string ReportName { get; set; }
        public int CRYear { get; set; }
        public string CRLength { get; set; }
        public string CRType { get; set; }
        public string CRStatus { get; set; }
        public string Creator { get; set; }
        public string DateCreated { get; set; }
    }

    public class DataPointReportsViewModel
    {
        public bool IsOverrideReport  { get; set; }
        public string ReportName { get; set; }
        public int CostReportYear { get; set; }
        public string CostReportType { get; set; }
        public string CostReportStatus { get; set; }
        public List<string> ProviderData { get; set; }
        public List<string> RangeName { get; set; }
    }

    public class SelectedDataPointsViewModel
    {
        public List<string> SelectedDataPoints { get; set; }
        public List<DataWizardProviderViewModel> SelectedProviders { get; set; }
    }

    public class DataWizardReportViewModel
    {
        public int ProviderCostReportId { get; set; }
        public string ProviderNPI { get; set; }
        public string ProviderNumber { get; set; }
        public string ProviderName { get; set; }
        public int CostReportYear { get; set; }
        public string CostReportType { get; set; }
        public string CostReportStatus { get; set; }
        public DateTime? CostReportBeginDate {  get; set; }
        public DateTime? CostReportEndDate { get; set; }
        public string DocumentNumber { get; set; }
        public string FieldName { get; set; }
        public string DataValue { get; set; }
        public int PageNumber { get; set; }
        public int DisplayLine { get; set; }
        public int DisplayColumn { get; set; }
        public string Line { get; set; }
        public string WorksheetName { get; set; }
        public int DataType { get; set; }
        public string RangeName { get; set; }
    }

    public class SpecialFocusFacilitiesViewModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }

    }
}