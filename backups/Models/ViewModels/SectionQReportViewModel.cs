using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
	public class SectionQReportViewModel
	{
		public class View
		{
			public DateTime EndingDate { get; set; }
			public List<SelectListItem> CLSProvGroupingList { get; set; }
			public string CLSProvGrouping { get; set; }
			public List<SelectListItem> ReportTypeList { get; set; }
			public string ReportType { get; set; }
			public List<SelectListItem> ReportVersionList { get; set; }
			public string ReportVersion { get; set; }
		}

		public class Save
		{
			public DateTime EndingDate { get; set; }
			public string CLSProvGrouping { get; set; }
			public string ReportType { get; set; }
			public string ReportVersion { get; set; }
		}
	}
}