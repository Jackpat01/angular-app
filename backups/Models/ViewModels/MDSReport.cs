using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
	public class MDSReport
	{
		public class View
		{
			/// <summary>
			/// Gets or sets the file.
			/// </summary>
			public HttpPostedFileBase UploadedFile { get; set; }
			public List<SelectListItem> ApplicationPeriods { get; set; }
			public int ApplicationPeriodId { get; set; }
			public int UserId { get; set; }
			public int ApplicationId { get; set; }
			public DateTime EndingDate { get; set; }
			public ReportPeriodOptions ReportPeriod { get; set; }
			public ReportRateVersion ReportRateVersion { get; set; }
			public ReportTypeOptions ReportType { get; set; }
			public List<ProviderSelection> providerList { get; set; }
		}

		public class Save
		{
			public int ApplicationPeriodId { get; set; }
			public DateTime EndingDate { get; set; }
			public string ReportPeriod { get; set; }
			public string ReportType { get; set; }
			public string ReportExtension { get; set; }
			public string ProviderIDListString { get; set; }
			public string RateVersion { get; set; }

        }

		public class ProviderSelection
		{
			public string Name { get; set; }
			public string MCAID { get; set; }
			public string Number { get; set; }
			public string ARUMProviderID { get; set; }
			public bool Selected { get; set; }
		}

        public enum ReportPeriodOptions
        {
            Weekly,
            Quarterly,
            SemiAnnual,
            Annual
        }
        public enum ReportTypeOptions
        {
            Quarterly,
            SemiAnnual,
            Annual
        }
        public enum ReportRateVersion
        {
            RUG,
            PDPM
        }
    }
}