using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using MSLC.Portal.MVC.Utility;

using Syncfusion.JavaScript.Models;

using WebPortal_OH_FI.ProjectSpecific.Data.Models.crdata;

namespace WebPortal_OH_FI.Models.CostReport
{
    public class SpecialNarrativesViewModel
    {
        public bool CanEdit { get; set; } 
        public List<SpecialNarrativeViewItem> Records { get; set; }
        public List<Column> Columns
        {
            get
            {
                return new List<Column>()
                {
                    new Column() { Field = "RecordId", HeaderText = "RecordId", IsPrimaryKey = true, IsIdentity = true, Visible = false },
                    new Column() { Field = "NarrativeNumber", HeaderText = "Narr Num", Width = 25},
                    new Column() { Field = "Description", HeaderText = "Description", Width = 75}
                };
            }
        }
    }

    public class SpecialNarrativeViewItem
    {
        public string RecordId { get; set; }
        public string NarrativeNumber { get; set; }
        public string Description { get; set; }
        private const string ClientMapScope = "specialNarrative";

        public SpecialNarrativeViewItem() { }

        public SpecialNarrativeViewItem(SpecialNarratives entity)
        {
            RecordId = ClientMapping.MapInternalIdToClientId(entity.SpecialNarrativeId.ToString(), ClientMapScope);
            NarrativeNumber = entity.NarrativeNumber;
            Description = entity.Description;
        }

        public SpecialNarratives CreateEntity()
        {
            int id = 0;
            int.TryParse(ClientMapping.GetInternalIdFromClientId(RecordId, ClientMapScope), out id);
            return new SpecialNarratives()
            {
                SpecialNarrativeId = id,
                NarrativeNumber = NarrativeNumber?.Replace('x', 'X'),
                Description = Description
            };
        }
    }

    public class SpecialNarrativePerms
    {
        public bool CanEdit = true; //this will be set by Rate Perm BL when appropriate feature name is implemented
    }
}