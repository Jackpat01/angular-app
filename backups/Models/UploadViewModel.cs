using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models
{
    /// <summary>
    /// Upload View Model
    /// </summary>
    public class UploadViewModel
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
        public SelectList Providers { get; set; }
        public int ProviderId { get; set; }
        public int UserId { get; set; }
        public int ApplicationId { get; set; }
        public bool HideVersion { get; set; }
        public bool HidePeriod { get; set; }
        public bool HideUploadButton { get; set; }
    }
}