using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using MSLC.Portal.MVC.Utility;

using Syncfusion.JavaScript.Models;

using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;

namespace WebPortal_OH_FI.Models
{
    public class NarrativeCodeViewModel
    {
        public List<NarrativeCodeViewItem> Records { get; set; }
        public List<Column> Columns
        {
            get
            {
                return new List<Column>()
                {
                    new Column() { Field = "RecordId", HeaderText = "RecordId", IsPrimaryKey = true, IsIdentity = true, Visible = false },
                    new Column() { Field = "Code", HeaderText = "Code" },
                    new Column() { Field = "Description", HeaderText = "Description", Width = 360 },
                    new Column() { Field = "SortOrder", HeaderText = "Sort Order" }
                };
            }
        }
    }

    public class NarrativeCodeViewItem
    {
        public string RecordId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }

        public NarrativeCodeViewItem(NarrativeCode entity)
        {
            RecordId = ClientMapping.MapInternalIdToClientId(entity.NarrativeCodeId.ToString(), "narrativeCode");
            Code = entity.Code;
            Description = entity.Description;
            SortOrder = entity.SortOrder;
        }
    }
}