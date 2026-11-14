using System;
using System.Collections.Generic;
using MSLC.BulkProcessing.DTO;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class BulkProcessingViewModel
    {
        public Guid ID { get; set; }
        public string JobName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }
    }
}