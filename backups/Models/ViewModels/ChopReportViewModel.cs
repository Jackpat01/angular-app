using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class ChopReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SelectedTransactionType { get; set; }
        public List<SelectListItem> TransactionTypes { get; set; }
    }
}