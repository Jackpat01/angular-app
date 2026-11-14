// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System.Web.Mvc;
using Ganss.Xss;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.DTO;
using MSLC.StatLog.MVC.ViewModels;

namespace MSLC.StatLog.Controllers
{
    public class ProvTypeController : LoggedInBaseController
    {
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Index()
        {
            var myViewModel = new ProvTypeViewModel.View
            {
                ProvTypes = ProviderBL.GetAllProvTypes(RetrieveSession.Classification)
            };
            return View(myViewModel);
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Edit(string pProvType)
        {
            ProvTypeDTO ProvType = ProviderBL.GetProvTypeByProvType(RetrieveSession.Classification, pProvType);

            var myViewModel = ProvType != null ? new ProvTypeViewModel.Edit(ProvType) : new ProvTypeViewModel.Edit();

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ProvTypeViewModel.Edit viewModel, string command)
        {
            ProvTypeDTO existProvType = ProviderBL.GetProvTypeByProvType(RetrieveSession.Classification, viewModel.ProvType);
            var sanitizer = new HtmlSanitizer();
            switch (command)
            {
                case "Save":
                    var ProvTypeDTO = new ProvTypeDTO
                    {
                        ProvType1 = sanitizer.Sanitize(viewModel.ProvType),
                        Name = sanitizer.Sanitize(viewModel.Name),
                        IsActive = viewModel.IsActive
                    };
                    if (existProvType == null)
                    {
                        //insert                      
                        int insertResponse = ProviderBL.InsertProvType(RetrieveSession.Classification, ProvTypeDTO);
                        if (insertResponse == -1)
                        {
                            TempData["Message"] = $"Provider Type was not saved, please contact support.";
                        }
                        else if (insertResponse == 0)
                        {
                            TempData["Message"] = $"Provider Type with the key '{sanitizer.Sanitize(viewModel.ProvType)}' already exists.";
                        }
                        else
                        {
                            TempData["Message"] = $"Provider Type has been successfully saved.";
                        }
                    }
                    else
                    {
                        //update
                        ProviderBL.UpdateProvType(RetrieveSession.Classification, ProvTypeDTO, existProvType.ProvType1);
                        TempData["Message"] = "Saved Successfully";
                    }
                    break;
                case "Deactivate":
                    ProviderBL.DeactivateProvType(RetrieveSession.Classification, existProvType);
                    TempData["Message"] = $"Provider Type deactivated successfully.";
                    break;
                case "Activate":
                    ProviderBL.ActivateProvType(RetrieveSession.Classification, existProvType);
                    TempData["Message"] = $"Provider Type activated successfully.";
                    break;
            }
            return RedirectToAction("Index", "ProvType");
        }
    }
}