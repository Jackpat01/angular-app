using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models
{
    public class LicenseAdminViewModel
    {
        public string LicenseAdminId { get; set; }

        public string AdminUserNumber { get; set; } // SSN

        public string AdminLicenseNumber { get; set; }

        public string AdminFirstName { get; set; }
        public string AdminLastName { get; set; }

    }

    public class LicenseAdminHistoryViewModel
    {
        public string LicenseAdminHistoryId { get; set; }
        public string LicenseAdminId { get; set; }
        public string HistoryAction { get; set; }
        public string HistoryActionUser { get; set; }
        public string HistoryActionDate { get; set; }
        public string AdminUserNumber { get; set; }
        public string AdminLicenseNumber { get; set; }
        public string AdminFirstName { get; set; }
        public string AdminLastName { get; set; }
    }
}