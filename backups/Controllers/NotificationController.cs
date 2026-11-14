using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebPortal_OH_FI.Controllers
{
    public class NotificationController : Controller
    {
        public PartialViewResult NotificationDialog(string headerText)
        {
            ViewBag.DialogHeaderText = headerText;
            return PartialView("_NotificationDialog");
        }
    }
}