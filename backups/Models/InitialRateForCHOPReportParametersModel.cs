using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using WebPortal_OH_FI.Common;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class InitialRateForCHOPReportParametersModel
    {
        [ExcelStyling(IgnoreProperty = true)]
        public int ProviderId { get; set; }
        [ExcelStyling(HeaderName = "Entering Provider Number", ColumnWidth = 11, NumberFormat = "0000000")]
        public string ProviderNumber { get; set; }
        [ExcelStyling(HeaderName = "Current NPI", ColumnWidth = 10.29f, AlignWithCenter = true)]
        public string NPI { get; set; }
        [ExcelStyling(HeaderName = "Initial Cert Date", ColumnWidth = 9.71f)]
        public DateTime? InitCertDate { get; set; }
        [ExcelStyling(HeaderName = "Provider Name", ColumnWidth = 60.14f, AlignWithCenter = true)]
        public string ProviderName { get; set; }
        [ExcelStyling(HeaderName ="CHOW Quality Override", ColumnWidth =12.43f, AlignWithCenter = true)]
        public string QualityOverride { get; set; }
        [ExcelStyling(HeaderName = "Entering Provider Total Rate", ColumnWidth = 8.43f, NumberFormat = "0.00")]
        public decimal? TotalRate { get; set; }
        [ExcelStyling(HeaderName = "Entering Provider Issuance Date", ColumnWidth = 9.71f)]
        public DateTime? IssuanceDate { get; set; }
        [ExcelStyling(IgnoreProperty = true)]
        public int ExitingProviderId { get; set; }
        [ExcelStyling(HeaderName = "Exiting Provider Number", ColumnWidth = 8.43f, NumberFormat = "0000000")]
        public string ExitingProviderNumber { get; set; }
        [ExcelStyling(HeaderName = "Exiting NPI", ColumnWidth = 10.29f, AlignWithCenter = true)]
        public string ExitingNPI { get; set; }
        [ExcelStyling(HeaderName = "RED of Exiting Provider", ColumnWidth = 9.71f)]
        public DateTime? ExitingRED { get; set; }
        [ExcelStyling(HeaderName = "Exiting Provider Total Rate", ColumnWidth = 8.43f, NumberFormat ="0.00")]
        public decimal? ExitingTotalRate { get; set; }
        [ExcelStyling(HeaderName = "Exiting Provider Quality Payment", ColumnWidth = 11.86f)]
        public decimal? ExitingQualityPayment { get; set; }
        [ExcelStyling(HeaderName = "Exiting Provider Issuance Date", ColumnWidth = 9.71f)]
        public DateTime? ExitingIssuanceDate { get; set; }
    }
}