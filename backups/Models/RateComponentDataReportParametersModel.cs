using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;

using Syncfusion.XlsIO;

using WebPortal_OH_FI.Common;

namespace WebPortal_OH_FI.Models
{
    public class RateComponentDataReportParametersModelICF
    {
        public string NPI { get; set; } = string.Empty;
        public string ProviderNumber { get; set; } = string.Empty;
        public string MedicareId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public DateTime? InitialCertDate{ get; set; }
        public string RateType { get; set; }

        public string FacilityCountyName { get; set; } = null;
        public int? CertifiedBedsTotal { get; set; }
        public string PeerGroup { get; set; } = string.Empty;
        public DateTime CostReportYearEnd { get; set; }
        public double? AverageCaseMix { get; set; } = null;
        public double? TotalsOtherProtected { get; set; } = null;
        public double? TotalsTotalPerDiemRate { get; set; } = null;
        public double? TotalsDirectSupportPersonnel { get; set; } = null;
        public double? TotalsProvQualityIncentivePayment { get; set; } = null;
        public double? EfficiencyIncentive { get; set; } = null;
        public double? TotalsProfessionalWorkforceDevelopmentPayment { get; set; } = null;
        public double? TotalsIndirectCare { get; set; } = null;
        public double? PerDiem { get; set; } = null;
        public double? TotalIndirectPerDiem { get; set; } = null;
        public double? TotalSecondaryBuildingFRVPerDiem { get; set; } = null;
        public double? EquipmentPerDiem { get; set; } = null;
        public double? AllowableNonExtensiveCost { get; set; } = null;
        public double? TotalsCapital { get; set; } = null;
        public double? IndirectCareCostOrLimit { get; set; } = null;
        public double? TotalsDirectCare { get; set; } = null;
        public DateTime? IssuanceDate { get; set; } 
    }

    public class RateComponentDataReportParametersModelNF
    {
        //-- All of the property names are identical to the columns from GetNFRateComponentData.sql, if one of these changes, please update the sproc

        [ExcelStyling(IgnoreProperty = true)]
        public int RatePackageId { get; set; }
        [ExcelStyling(IgnoreProperty = true)]
        public string MedicaidStatus { get; set; }
        [ExcelStyling(IgnoreProperty = true)]
        public int ProviderId { get; set; }
        [ExcelStyling(HeaderName = "Cnt", ColumnWidth = 5)]
        public int Cnt { get; set; } //not worried about it
        [ExcelStyling(HeaderName = "NPI Number", ColumnWidth = 10, AlignWithCenter = true)]
        public string NPI { get; set; } //ARUM
        [ExcelStyling(HeaderName = "Provider Number", ColumnWidth = 9, NumberFormat = "0000000")]
        public string ProviderNumber { get; set; }
        [ExcelStyling(HeaderName = "Medicare ID", ColumnWidth = 9)]
        public string MedicareId { get; set; } //ARUM
        [ExcelStyling(HeaderName = "Provider Name", ColumnWidth = 60, AlignWithCenter = true)]
        public string ProviderName { get; set; }
        [ExcelStyling(HeaderName = "Initial Cert Date", ColumnWidth = 10)]
        public DateTime? IntiialCertDate { get; set; }
        [ExcelStyling(HeaderName = "Rate Type", ColumnWidth = 10, AlignWithCenter = true)]
        public string RateType { get; set; }
        [ExcelStyling(HeaderName = "County", ColumnWidth = 9, AlignWithCenter = true)]
        public string County { get; set; } //ARUM
        [ExcelStyling(HeaderName = "Lic Beds", ColumnWidth = 9)]
        public int? LicensedBeds { get; set; }
        [ExcelStyling(HeaderName = "Ancillary/Support Peer Group", ColumnWidth = 9, AlignWithCenter = true)]
        public string AncillaryPeerGroup { get; set; }
        [ExcelStyling(HeaderName = "Direct Care Peer Group", ColumnWidth = 9, AlignWithCenter = true)]
        public string DirectCarePeerGroup { get; set; }
        [ExcelStyling(HeaderName = "Case Mix Score", ColumnWidth = 9, NumberFormat = "0.0000", AlignWithCenter = true)]
        public decimal? CaseMixScore { get; set; } //Which scodecimale?
        [ExcelStyling(HeaderName = "Direct Rate", ColumnWidth = 9, NumberFormat = "0.00")]
        public decimal? DirectRate { get; set; }
        [ExcelStyling(HeaderName = "Ancillary/Support Rate", ColumnWidth = 9, NumberFormat = "0.00")]
        public decimal? AncillaryRate { get; set; }
        [ExcelStyling(HeaderName = "Capital Rate", ColumnWidth = 9, NumberFormat = "0.00")]
        public decimal? CapitalRate { get; set; }
        [ExcelStyling(HeaderName = "Tax Rate", ColumnWidth = 9, NumberFormat = "0.00")]
        public decimal? TaxRate { get; set; }
        [ExcelStyling(HeaderName = "Critical Access Nursing Home Rate Add-on", ColumnWidth = 9, NumberFormat = "0.00")]
        public decimal? CriticalAccessAddOn { get; set; }
        [ExcelStyling(HeaderName = "Add-on for ORC 5165.15(B)", ColumnWidth = 7, NumberFormat = "0.00")]
        public decimal? ORCAddOn { get; set; }
        [ExcelStyling(HeaderName = "Deduction per ORC 5165.15 ©", ColumnWidth = 10, NumberFormat = "0.00")]
        public decimal? ORCDeduction { get; set; }
        [ExcelStyling(HeaderName = "Quality Payment Rate per ORC 5165.26", ColumnWidth = 9, NumberFormat = "0.00")]
        public decimal? ORCPayment { get; set; }
        [ExcelStyling(HeaderName = "Occupancy Percentage", ColumnWidth = 14, NumberFormat = "0.00")]
        public decimal? OccupancyPercentage { get; set; } 
        [ExcelStyling(HeaderName = "Deduction for Occupancy <65% (5%)", ColumnWidth = 11, NumberFormat = "0.00")]
        public decimal? OccupancyDeduction { get; set; }
        [ExcelStyling(HeaderName = "Total Rate", ColumnWidth = 7, NumberFormat = "0.00")]
        public decimal? TotalRate { get; set; }
        [ExcelStyling(HeaderName = "High LOA Rate", ColumnWidth = 9, NumberFormat = "0.00")]
        public decimal? LOAHigh { get; set; }
        [ExcelStyling(HeaderName = "Low LOA Rate", ColumnWidth = 9, NumberFormat = "0.00")]
        public decimal? LOALow { get; set; }
        [ExcelStyling(HeaderName = "Issuance Date", ColumnWidth = 12)]
        public DateTime? IssuanceDate { get; set; }
    }
}