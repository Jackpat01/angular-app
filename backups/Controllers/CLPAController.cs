using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.IP;
using MSLC.Portal.MVC.URL;
using MSLC.Portal.MVC.Utility;
using MSLC.StatLog.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebPortal_OH_FI.BL;
using WebPortal_OH_FI.Models.ViewModels;

namespace WebPortal_OH_FI.Controllers
{
    public class CLPAController : LoggedInBaseController
    {
        #region Search Provider
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("CLPA:View")]
        public ActionResult SearchAssessment()
        {
            var myViewModel = new SearchAssessmentViewModel.View
            {
                mySearchResults = null
            };

            return View(myViewModel);
        }

        [HttpPost]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("CLPA:View")]
        public ActionResult SearchAssessment(SearchAssessmentViewModel.Search search)
        {
            var viewModel = new SearchAssessmentViewModel.View
            {
                mySearchResults = null,
                SSN = search.SSN
            };

            if (CLPABL.IsRateLimitReachedForTodayForFunction("SearchAssessment"))
                return View(viewModel);

            string querySSN = search.SSN?.Trim();
            var assessments = CLPABL.SearchAssessmentsBySSN(querySSN)
                .OrderByDescending(p => p.AssessmentRefrenceDate).ToList();

            if (!assessments.Any())
            {
                TempData["Message"] = "No Assessments for that SSN were found";
            }
            else
            {
                viewModel.mySearchResults = assessments.Select(a => new SearchAssessmentViewModel.SearchAssessmentResults
                {
                    AssessmentID = a.AssessmentID.Substring(0, a.AssessmentID.LastIndexOf('.')),
                    ReferenceDate = a.AssessmentRefrenceDate,
                    ReferenceDateString = a.AssessmentRefrenceDate.ToString()
                }).OrderByDescending(x => x.ReferenceDate.HasValue).ThenByDescending(x => x.ReferenceDate).ThenBy(x => x.AssessmentID).ToList();
            }

            CLPABL.LogAccess(RetrieveSession.ARUMUser.IPAddress, "Test");

            return View(viewModel);
        }
        #endregion
    }
}