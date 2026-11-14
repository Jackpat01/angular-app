using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models
{
	public class AuditCommentGridItem
	{
		public string NarrativeNumber { get; set; }
		public bool HasIcon { get; set; } = false;
		public string Description { get; set; } = string.Empty;

		public AuditCommentGridItem() { }
		public AuditCommentGridItem(string narrativeNumber, bool hasIcon, bool isChecked, string description)
		{
			NarrativeNumber = narrativeNumber;
			HasIcon = hasIcon;
			Description = description;
		}
	}
}