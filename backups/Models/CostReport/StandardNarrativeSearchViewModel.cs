using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Syncfusion.JavaScript.Models;

using WebPortal_OH_FI.ProjectSpecific.Data.Models.ratesetting;

namespace WebPortal_OH_FI.Models.CostReport
{
    public class StandardNarrativeSearchViewModel
    {
        public List<StandardNarrativeSearchViewItem> Records { get; set; }

        public List<Column> Columns = new List<Column>()
        {
            new Column() { Field = "NarrativeNumber", HeaderText = "Narrative Number" },
            new Column() { Field = "Description", HeaderText = "Description" }
        };

        public StandardNarrativeSearchViewModel()
        {
            Records = new List<StandardNarrativeSearchViewItem>();
        }
        public StandardNarrativeSearchViewModel(List<StandardNarrativeSearchViewItem> records)
        {
            Records = records;
        }
    }

    public class StandardNarrativeSearchViewItem
    {
        public string NarrativeNumber { get; set; }
        public string Description { get; set; }

        public StandardNarrativeSearchViewItem() { }
        public StandardNarrativeSearchViewItem(NarrativeTable entity)
        {
            NarrativeNumber = entity.NarrativeNumber;
            Description = entity.Description;
        }
    }
}