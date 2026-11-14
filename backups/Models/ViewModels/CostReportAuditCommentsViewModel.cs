using MSLC.Portal.MVC.Utility;
using System.Collections.Generic;
using WebPortal_OH_FI.Models.CostReport;
using WebPortal_OH_FI.ProjectSpecific.Data.Models.crdata;


namespace WebPortal_OH_FI.Models.ViewModels
{
	public class CostReportAuditCommentsViewModel
	{
		public List<AuditCommentGridItem> GridItems { get; set; } = new List<AuditCommentGridItem>();

		public List<AuditCommentItem> AuditComments { get; set; } = new List<AuditCommentItem>();

		public Dictionary<string, List<string>> ExclusiveGroups { get; set; } = null;
		public List<SpecialNarrativeViewItem> SpecialNarratives { get; set; } = new List<SpecialNarrativeViewItem>();
		public AuditCommentPerms AuditCommentPerms { get; set; }

		public CostReportAuditCommentsViewModel() { }
		public CostReportAuditCommentsViewModel(List<AuditCommentGridItem> gridItems, List<AuditCommentItem> auditComments, Dictionary<string, List<string>> exclusiveGroups, List<SpecialNarrativeViewItem> specialNarratives, AuditCommentPerms auditCommentPerms)
		{
			GridItems = gridItems;
			AuditComments = auditComments;
			ExclusiveGroups = exclusiveGroups;
			SpecialNarratives = specialNarratives;
			AuditCommentPerms = auditCommentPerms;
		}
	}

	public class AuditCommentItem
	{
		public string AuditCommentId { get; set; }
		public int ProviderCostReportId { get; set; }
		public string SpecialNarrativeId { get; set; } = string.Empty;
		public string NarrativeNumber { get; set; }
		public string CommentText { get; set; }
		public const string ClientMapScope = "auditComments";
		public const string SpecialNarrativeMapScope = "specialNarrative";

		public AuditCommentItem() { }

		public AuditCommentItem(CostReportAuditComment entity)
		{
			AuditCommentId = ClientMapping.MapInternalIdToClientId(entity.AuditCommentId.ToString(), ClientMapScope);
			ProviderCostReportId = entity.ProviderCostReportId;
			SpecialNarrativeId = (entity.SpecialNarrativeId != null) ? ClientMapping.MapInternalIdToClientId(entity.SpecialNarrativeId.ToString(), SpecialNarrativeMapScope) : string.Empty;
			NarrativeNumber = entity.AuditCommentCode;
			CommentText = entity.CommentText;
		}

		public CostReportAuditComment CreateEntity()
		{
			int id = 0;
			int.TryParse(ClientMapping.GetInternalIdFromClientId(AuditCommentId, ClientMapScope), out id);
			int specialNarrativeId = 0;
			if(!string.IsNullOrEmpty(SpecialNarrativeId))
			{
				int.TryParse(ClientMapping.GetInternalIdFromClientId(SpecialNarrativeId, SpecialNarrativeMapScope), out specialNarrativeId);
			}
			return new CostReportAuditComment()
			{
				AuditCommentId = id,
				ProviderCostReportId = ProviderCostReportId,
				SpecialNarrativeId = specialNarrativeId,
				AuditCommentCode = NarrativeNumber,
				CommentText = CommentText,
			};
		}
	}

	public class AuditCommentPerms
	{
		public bool CanEdit = true;
	}
}