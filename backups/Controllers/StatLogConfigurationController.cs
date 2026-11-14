using MSLC.ExceptionAlert;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.StatLog.MVC.ViewModels;
using Serilog;
using System;
using System.Web.Mvc;

namespace MSLC.StatLog.UI.Controllers
{
    public class StatLogConfigurationController : Controller
    {
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Index()
        {
            var viewModel = new StatLogConfigurationViewModel.View
            {
                IsLoggingSetUp = Log.IsEnabled(Serilog.Events.LogEventLevel.Error)
            };
            return View(viewModel);
        }
    }
}