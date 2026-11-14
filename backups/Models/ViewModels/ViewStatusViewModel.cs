using System;
using System.Collections.Generic;
using MSLC.BulkProcessing.DTO;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class ViewStatusViewModel
    {
        public string JobName { get; set; }
        public List<ItemViewModel> Items { get; set; }

        public class ItemViewModel
        {
            public Guid ID { get; set; }
            public Guid? ParentID { get; set; }
            public string FileName { get; set; }
            public string Status { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreateDate { get; set; }
            public string FirstMessage { get; set; }
        }
    }

    public class ViewResidentStatusViewModel
    {
        public string JobName { get; set; }
        public List<ItemViewModel> Items { get; set; }

        public class ItemViewModel
        {
            public Guid ID { get; set; }
            public Guid? ParentID { get; set; }
            public string ProviderName { get; set; }
            public string ProviderMedicareID { get; set; }
            public string Status { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreateDate { get; set; }
            public string FirstMessage { get; set; }
        }
    }
}