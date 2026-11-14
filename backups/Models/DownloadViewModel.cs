using System;
using MSLC.FileProxy.Interfaces;
using System.Collections.Generic;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class DownloadViewModel
    {
        /// <summary>
        /// The Selected files for download by user.
        /// </summary>
        public string FileIDs { get; set; }

        public string sProviderID { get; set; }
        /// <summary>
        /// Gets or sets my providers_ select list item.
        /// </summary>
        /// <value>
        /// My providers_ select list item.
        /// </value>
        public List<DownloadDropDownProvider> Providers { get; set; }
        public string PrimaryIdentifier { get; set; }
        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        /// <value>
        /// The files.
        /// </value>
        public FileInformationViewModel[] Files { get; set; }
        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName { get; set; }
    }


    public class DownloadDropDownProvider
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public string ARUMProviderID { get; set; }
    }

    public class FileInformationViewModel
    {
        public string FileID { get; set; }
        public string ParentFileTypeName { get; set; }
        public string ChildFileTypeName { get; set; }
        public string PeriodText { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public string SourceType { get; set; }
        public DateTime CreateDate { get; set; }
        public string SizeInString { get; set; }
    }
}