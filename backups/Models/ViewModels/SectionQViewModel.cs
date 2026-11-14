using System;
using System.Collections.Generic;
using MSLC.BulkProcessing.DTO;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class SectionQViewModel
    {
        public string ID { get; set; }
        public string JobName { get; set; }
        public string ReportVersion { get; set; }
        public string ReportType { get; set; }
        public string CLSProvGrouping { get; set; }
        public DateTime? ReportEndDate { get; set; }
        public string ReportEndDateString { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }
        public bool Downloadable { get; set; }
        public bool Error { get; set; }
    }
}