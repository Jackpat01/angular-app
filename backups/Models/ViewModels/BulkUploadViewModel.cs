using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace MSLC.StatLog.Models.ViewModels
{
    public class BulkUploadViewModel
    {
        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        public int DeleteAfterDays { get; set; }
        public HttpPostedFileBase UploadedFile { get; set; }
        public List<SelectListItem> FileTypes { get; set; }
        public int FileTypeId { get; set; }
        public List<SelectListItem> FileSubTypes { get; set; }
        public int FileSubTypeId { get; set; }
        public List<SelectListItem> ApplicationPeriods { get; set; }
        public int ApplicationPeriodId { get; set; }
        public int UserId { get; set; }
        public int ApplicationId { get; set; }
        public bool HideVersion { get; set; }
        public bool HidePeriod { get; set; }
        public bool HideUploadButton { get; set; }

        public class FileNameValidation
        {
            public int Order { get; set; }
            public string FileName { get; set; }
            public bool Valid { get; set; }
            public string ValidationMessage { get; set; }
        }
    }
}