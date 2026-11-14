using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class StatLogUploadDownloadReport
    {
        public List<SelectListItem> ProviderIdentifiers = new List<SelectListItem>();
    }
    public class StatLogMassUploadReport
    {
        public List<SelectListItem> ProviderIdentifiers = new List<SelectListItem>();
    }

    public class ReportPayload
    {
        public string FileName;
        public string FileType;
        public MemoryStream File;
    }
}