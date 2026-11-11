using CaptchaMvc.Attributes;
using MSLC.ArumProxy;
using MSLC.ArumProxy.Interfaces;
using MSLC.ArumProxy.Models;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.IP;
using MSLC.Portal.Utility.Proxies;
using Omu.ValueInjecter;
using Serilog;
using System;
using System.Linq;
using System.Web.Mvc;
using WebPortal_OH_FI.Models;
using WebPortal_OH_FI.Web;
using Functions = WebPortal_OH_FI.Common.Functions;

namespace WebPortal_OH_FI.Controllers
{

    /// <summary>
    /// The Home Controller that controls all Pages and functions.
    /// </summary>
    public class HomeController : Controller
    {
        #region Properties
        private ArumServiceProxy _arumUserProxy;
        #endregion

        [HttpPost]
        public ActionResult HeartBeat()
        {
            Session["HeartBeat"] = DateTime.Now;
            return new JsonResult { Data = "Success" };
        }

        public HomeController()
        {
	        var allClassifications = PortalConfiguration.GetAllClassifications();
            // This proxy is used when no classification is selected.
            _arumUserProxy = ARUMUser.GetArumServiceProxy(allClassifications.First());
        }

        /// <summary>
        /// Logins this instance.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Login()
        {
            if(OHFISession.IsSSO)
            {
                return RedirectToAction("Error", "SSO");
            }
            Session["RemoveCaptcha"] = Session["RemoveCaptcha"] ?? true;

            var myViewModel = Session["HomeViewModel"] != null 
                ? (HomeViewModel)Session["HomeViewModel"] : new HomeViewModel();

            if (Session["GlobalErrorMessage"] != null)
            {
                TempData["ErrorMessage"] = Session["GlobalErrorMessage"];
                Session["GlobalErrorMessage"] = null;
            }

            return View(myViewModel);
        }
        /// <summary>
        /// Accepts the terms of use.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult AcceptTermsOfUse()
        {
            if (OHFISession.IsSSO)
            {
                return RedirectToAction("Error", "SSO");
            }
            try
            {
                var myViewModel = new HomeViewModel.EULA();
                if (HttpContext.Session["Email"] == null)
                {
                    return RedirectToAction("Login", "Home");
                }

                myViewModel.Email = HttpContext.Session["Email"].ToString();
                myViewModel.EULAText = GetEULAText();


                return View(myViewModel);
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
            }

            return RedirectToAction("Login", "Home");
        }

        /// <summary>
        /// Saves the accept terms of use.
        /// </summary>
        /// <param name="myViewModel">My view model.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SaveAcceptTermsOfUse(HomeViewModel.EULA myViewModel)
        {
            if (OHFISession.IsSSO)
            {
                return RedirectToAction("Error", "SSO");
            }
            try
            {
                var redirect = false;
                var myExternalIPAddress = IP.GetUserIP_RemoteAddR();
                if (!string.IsNullOrEmpty(myViewModel.Email))
                {
                    IARUMUserValidation arumValidations;
                    if (Session["ARUMUserValidations"] == null)
                    {
                        arumValidations = _arumUserProxy.GetUserRolesByEmailNoPasswordAndApplications(
                            Common.Functions.GetARUMTokensFromClassifications,
                            myViewModel.Email,
                            "AcceptTermsOfUse",
                            myExternalIPAddress
                        );
                        Session["ARUMUserValidations"] = null;
                        Session["ARUMUserValidations"] = arumValidations;
                    }
                    arumValidations = (IARUMUserValidation)Session["ARUMUserValidations"];


                    if (arumValidations != null && arumValidations.lstARUMUsers != null &&
                        arumValidations.lstARUMUsers.Any())
                    {
	                    var allClassifications = PortalConfiguration.GetAllClassifications();
                        foreach (var singleARUMValidation in arumValidations.lstARUMUsers)
                        {
                            redirect = false;
                            _arumUserProxy = ARUMUser.GetArumServiceProxy(allClassifications.FirstOrDefault(c => c.ARUMToken == singleARUMValidation.Token));
                            var myAcceptedTerms = _arumUserProxy.UpdateAcceptedTerms(singleARUMValidation.UserID, "Accepted Terms");

                            redirect = myAcceptedTerms.Results.Successful;
                            TempData["ErrorMessage"] = myAcceptedTerms.Results.Message;
                            
                        }
                    }
                    

                    if (redirect)
                    {
                        return RedirectToAction("Projects", "Projects");
                    }
                    
                }
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);

            }
            return RedirectToAction("Login", "Home");
        }
        #region old accept terms code. Replaced 10/5/2017

        /// <summary>
        /// Saves the accept terms of use.
        /// </summary>
        /// <param name="myViewModel">My view model.</param>
        /// <returns></returns>
        //[HttpPost]
        //public ActionResult SaveAcceptTermsOfUse(HomeViewModel.EULA myViewModel)
        //{
        //    try
        //    {
        //        if (myViewModel.UserID != 0)
        //        {
        //            var myAcceptedTerms = _arumUserProxy.UpdateAcceptedTerms(myViewModel.UserID, "Accepted Terms");
        //            if (myAcceptedTerms.Results.Successful)
        //            {
        //                return RedirectToAction("Index", "Secure");
        //            }
        //            return RedirectToAction("Login", "Home");
        //        }
        //        return RedirectToAction("Login", "Home");
        //    }
        //    catch (Exception ex)
        //    {
        //        var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
        //        Log.Error(ex, exMessage);
        //    }
        //    return RedirectToAction("Login", "Home");
        //}
        #endregion
        /// <summary>
        /// Logins to page.
        /// </summary>
        /// <param name="myViewModel">My view model.</param>
        /// <returns></returns>
        [HttpPost, CaptchaVerify("The Captcha you entered was not valid, Please try again.")]
        public ActionResult LoginToPage(HomeViewModel myViewModel)
        {
            if (OHFISession.IsSSO)
            {
                return RedirectToAction("Error", "SSO");
            }
            try
            {
                var redirect = false;
                var redirectToTerms = false;
                Session["RemoveCaptcha"] = Session["RemoveCaptcha"] ?? true;

                if ((bool)Session["RemoveCaptcha"] && PortalConfiguration.RemoveCaptchaAfterFailedReset != 0)
                {
                    ModelState.Remove("CaptchaInputText");
                }

                if (ModelState.IsValid)
                {
                    var myExternalIPAddress = IP.GetUserIP_RemoteAddR();


                    var arumValidations = _arumUserProxy.GetUserRolesByEmailAndPasswordAndApplications(
                        Functions.GetARUMTokensFromClassifications,
                        myViewModel.Email,
                        myViewModel.NonHashedPassword,
                        "LoginToPage",
                        myExternalIPAddress
                    );

                    if (arumValidations != null && arumValidations.Results.Successful == false)
                    {
                        TempData["ErrorMessage"] = "Username/password invalid";
                        return RedirectToAction("Login", "Home");
                    }

                    if (arumValidations.Results.Successful)
                    {

                        if (!arumValidations.lstARUMUsers.Any())
                        {
                            TempData["ErrorMessage"] = "Username/password invalid";
                            return RedirectToAction("Login", "Home");
                        }
                        Session["ARUMUserValidations"] = null;
                        Session["ARUMUserValidations"] = arumValidations;
                        var allClassifications = PortalConfiguration.GetAllClassifications();
                        foreach (var singleARUMValidation in arumValidations.lstARUMUsers)
                        {
                            redirect = false;
                            _arumUserProxy = ARUMUser.GetArumServiceProxy(allClassifications.FirstOrDefault(c => c.ARUMToken == singleARUMValidation.Token));
                            if (singleARUMValidation.IPAddress != myExternalIPAddress)
                            {
                                var myARUMResults = _arumUserProxy.UpdateUserIPAddress(singleARUMValidation.UserID, myExternalIPAddress, "Home - Login");
                                if (!myARUMResults.Successful)
                                {
                                    TempData["ErrorMessage"] = myARUMResults.Message;
                                }
                            }
                            Session["ARUMUser"] = null;
                            Session["ARUMUser"] = new ARUMProxy_User().InjectFrom(singleARUMValidation) as ARUMProxy_User;
                            Session["UserName"] = singleARUMValidation.Email;
                            Session["Email"] = singleARUMValidation.Email;
                            redirect = true;
                        }
                        if (arumValidations.lstARUMUsers.Any(c => !c.AcceptedTerms.HasValue))
                        {
                            redirectToTerms = true;
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = arumValidations.Results.Message;
                    }
                }
                else
                {
                    if (ModelState["CaptchaInputText"] != null)
                    {
                        if (ModelState["CaptchaInputText"].Errors != null)
                        {
                            if (ModelState["CaptchaInputText"].Errors.Count >= 1)
                            {
                                if (ModelState["CaptchaInputText"].Errors[0] != null)
                                {
                                    if (!string.IsNullOrEmpty(ModelState["CaptchaInputText"].Errors[0].ErrorMessage))
                                    {
                                        TempData["CaptchaMessage"] = ModelState["CaptchaInputText"].Errors[0].ErrorMessage;
                                    }
                                }
                            }
                        }
                    }
                }

                var myHomeViewModel = new HomeViewModel
                {
                    Email = myViewModel.Email
                };
                Session["HomeViewModel"] = null;
                Session["HomeViewModel"] = myHomeViewModel;
                if (redirect)
                {
                    if (redirectToTerms)
                    {
                        return RedirectToAction("AcceptTermsOfUse");
                    }
                    else
                    {
                        return RedirectToAction("Projects", "Projects");
                    }
                }
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
            }

            return RedirectToAction("Login", "Home");
        }

        /// <summary>
        /// Gets the EULA text.
        /// </summary>
        /// <returns></returns>
        private string GetEULAText()
        {
            try
            {
                string FilePath = HttpContext.Server.MapPath("~") + @"Documents";
                string FullPath = string.Format(@"{0}\EULA.txt", FilePath);

                if (!System.IO.File.Exists(FullPath)) return string.Empty;
                
                var ReturnValue = System.IO.File.ReadAllText(FullPath);
                if (ReturnValue.Contains("xx_State_xx"))
                {
                    ReturnValue = ReturnValue.Replace("xx_State_xx", PortalConfiguration.Application);
                }
                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ActionResult Error()
        {
            return View();
        }
    }
}