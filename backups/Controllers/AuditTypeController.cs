// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System.Web.Mvc;
using Ganss.Xss;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.StatLog.BL.AuditType;
using MSLC.StatLog.DTO;
using MSLC.StatLog.MVC.ViewModels;

namespace MSLC.StatLog.Controllers
{
    public class AuditTypeController : LoggedInBaseController
    {
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Index()
        {
            var myViewModel = new AuditTypeViewModel.View
            {
                AuditTypes = AuditTypeBL.GetAllAuditTypes(RetrieveSession.Classification)
            };
            return View(myViewModel);
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Edit(string pAuditType)
        {
            AdtTypeDTO AuditType = AuditTypeBL.GetAuditTypeByAuditType(RetrieveSession.Classification, pAuditType);

            var myViewModel = AuditType != null ? new AuditTypeViewModel.Edit(AuditType) : new AuditTypeViewModel.Edit();

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult Save(AuditTypeViewModel.Edit viewModel, string command)
        {
            var sanitizer = new HtmlSanitizer();
            command = sanitizer.Sanitize(command);
            AdtTypeDTO existAuditType = AuditTypeBL.GetAuditTypeByAuditType(RetrieveSession.Classification, viewModel.AuditType);

            switch (command)
            {
                case "Save":
                    var AuditTypeDTO = new AdtTypeDTO
                    {
                        AuditType = sanitizer.Sanitize(viewModel.AuditType),
                        Name = sanitizer.Sanitize(viewModel.Name),
                        InActive = existAuditType == null ? true : existAuditType.InActive 
                    };
                    if (existAuditType == null)
                    {
                        //insert                      
                        int insertResponse = AuditTypeBL.Insert(RetrieveSession.Classification, AuditTypeDTO);
                        if (insertResponse == -1)
                        {
                            TempData["Message"] = $"{PortalConfiguration.AuditTypeReplacementName} was not saved, please contact support.";
                        }
                        else if (insertResponse == 0)
                        {
                            TempData["Message"] = $"{PortalConfiguration.AuditTypeReplacementName} with the key '{viewModel.AuditType}' already exists.";
                        }
                        else
                        {
                            TempData["Message"] = $"{PortalConfiguration.AuditTypeReplacementName} has been successfully saved.";                        
                        }
                    }
                    else
                    {
                        //update
                        AuditTypeBL.Update(RetrieveSession.Classification, AuditTypeDTO, existAuditType.AuditType);
                        TempData["Message"] = "Saved Successfully";
                    }
                    break;
                case "Deactivate":
                    AuditTypeBL.Deactivate(RetrieveSession.Classification, existAuditType);
                    TempData["Message"] = $"{PortalConfiguration.AuditTypeReplacementName} deactivated successfully.";
                    break;
                case "Activate":
                    AuditTypeBL.Activate(RetrieveSession.Classification, existAuditType);
                    TempData["Message"] = $"{PortalConfiguration.AuditTypeReplacementName} activated successfully.";
                    break;
            }
            return RedirectToAction("Index", "AuditType");
        }
    }
}