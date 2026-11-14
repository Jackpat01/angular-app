using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
	public class MDSResidentListingReport
	{
		public class View
		{
			/// <summary>
			/// Gets or sets the file.
			/// </summary>
			public HttpPostedFileBase UploadedFile { get; set; }
			public int UserId { get; set; }
			public int ApplicationId { get; set; }
			public List<ProviderSelection> providerList { get; set; }
		}

		public class Save
		{
			public string ProviderIDListString { get; set; }

        }

		public class ProviderSelection
		{
			public string Name { get; set; }
			public string MCAID { get; set; }
			public string MedicareID { get; set; }
			public string NPI { get; set; }
			public string ARUMProviderID { get; set; }
			public bool Selected { get; set; }
		}
    }
}