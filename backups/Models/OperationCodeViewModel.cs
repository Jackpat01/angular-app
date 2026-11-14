using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using MSLC.Portal.MVC.Utility;

using Syncfusion.JavaScript.Models;

using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;

namespace WebPortal_OH_FI.Models
{
    public class OperationCodeViewModel
    {
        public List<OperationCodeViewItem> Records { get; set; }
        public List<Column> Columns => new List<Column>()
        {
            new Column() { Field = "RecordId", IsIdentity = true, IsPrimaryKey = true, Visible = false},
            new Column() { Field = "Code", HeaderText = "Operation Code" },
            new Column() { Field = "Description", HeaderText = "Description" }
        };
    }

    public class OperationCodeViewItem
    {
        public string RecordId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }

        public OperationCodeViewItem() { }
        public OperationCodeViewItem(OperationCode entity)
        {
            RecordId = ClientMapping.MapInternalIdToClientId(entity.OperationCodeId.ToString(), "operationCodes");
            Code = entity.Code;
            Description = entity.Description;
        }
    }
}