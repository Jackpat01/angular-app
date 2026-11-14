// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

using MSLC.ApplicationProxy;
using MSLC.ArumProxy;
using MSLC.FileProxy;
using MSLC.LetterAutomation;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.ProviderProxy;
using MSLC.StatLog.BL.AuditType;
using MSLC.StatLog.BL.CostReportHistory;
using MSLC.StatLog.BL.MappedObjectsNamespace;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.BL.StatLogBL;
using MSLC.StatLog.BL.XML;
using MSLC.StatLog.Configuration;
using MSLC.StatLog.DTO;
using MSLC.StatLog.UI.Models;
using Serilog;
using static MSLC.StatLog.MVC.ViewModels.LetterAutomationViewModel;
using static MSLC.StatLog.UI.Models.StatLogMapping;

namespace MSLC.StatLog.Controllers
{
    public class LetterAutomationController : LoggedInBaseController
    {
        private const string FILE_DOWNLOAD_COOKIE_NAME = "fileDownload";
        private readonly ArumServiceProxy _arumUserProxy;
        private readonly FileServiceProxy _fileServiceProxy;
        private readonly ARUMProviderProxy _providerProxy;
        private readonly ARUMApplicationServiceProxy _applicationProxy;

        public LetterAutomationController()
        {
            _arumUserProxy = ARUMUser.GetArumServiceProxy(RetrieveSession.Classification);
            _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(RetrieveSession.Classification);
            _providerProxy = ARUMProvider.GetARUMProviderServiceProxy(RetrieveSession.Classification);
            _applicationProxy = ARUMApplication.GetARUMApplicationProxy(RetrieveSession.Classification);
        }

        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("LetterAutomation:Access")]
        public ActionResult Index()
        {
            if(!StatLogConfiguration.CanAccessLetterAutomation(RetrieveSession.UserRoles))
                return RedirectToAction("Index", "Secure");

            var myArmUser = RetrieveSession.ARUMUser;

            if (myArmUser.Providers == null || !myArmUser.Providers.Any())
            {
                TempData["Message"] = "No Providers Assigned to Current User.  Contact Support.  Click the icon below.";
                return RedirectToAction("Login", "Home");
            }

            var primaryIdentifier = ProviderBL.GetPrimaryIdentifierDisplayName(RetrieveSession.Classification);

            var providers = ProviderBL
                .GetAllARUMProviderInfoByARUMUserID(RetrieveSession.Classification, RetrieveSession.ARUMUser, null)
                .OrderBy(m => m.Name).Select(p => new ProviderDropdown
                {
                    ARUMProviderID = ClientMappingStatLog.ToClientID(p.ARUMProviderID, StatLogScopes.PROVIDER_ID_STATLOG),
                    Name = p.Name,
                    Number = p.Number
                }).ToList();

            var templates = _providerProxy.GetAllFileTemplates(RetrieveSession.Classification.ARUMToken, "LetterAutomation");

            if (templates == null)
            {
                TempData["Message"] = "No file templates assigned to application.  Contact Support.";
                return RedirectToAction("Index", "Secure");
            }

            var templateItems = templates.Templates.Where(t => t.IsActive).Select(m => new SelectListItem()
            {
                Text = m.Title,
                Value = ClientMappingStatLog.ToClientID(m.FileTemplateID, StatLogScopes.FILE_TEMPLATE_ID_STATLOG)
            }).ToArray();

            if (!templateItems.Any())
            {
	            TempData["Message"] = "No file templates assigned to application.  Contact Support.";
	            return RedirectToAction("Index", "Secure");
            }

			var ddlTemplate = new SelectList(templateItems, "Value", "Text");
            var myViewModel = new View
            {
                ShowMetadata = StatlogARUMConfiguration.ShowLetterMetadata(RetrieveSession.Classification),
                TemplateDropdownList = ddlTemplate,
                ProviderList = providers,
                CostReportsDropdownList = new SelectList(Enumerable.Empty<SelectListItem>()),
                CorrespondentDropdownList = new List<MappedObjects.ProviderCorrespondent>(),
                PrimaryIdentifier = !String.IsNullOrEmpty(primaryIdentifier) ? primaryIdentifier : "MCAID"
            };

            return View(myViewModel);
        }

        [ARUMFeatureActionFilterAttribute("LetterAutomation:Access")]
        public JsonResult GetProviderSpecificCostReports(string sProviderID)
        {
            int ProviderID = ClientMappingStatLog.ToInternalID(sProviderID, StatLogScopes.PROVIDER_ID_STATLOG);

            var map = MapByProviderID(ProviderID, null);
            if (map.Error)
                return Json(new { Error = true, ReturnValue = map.ErrorMessage });

            var myUser = RetrieveSession.ARUMUser;
            if (myUser.UserID == -1 || string.IsNullOrWhiteSpace(myUser.Email))
            {
                TempData["Message"] = "ERROR: Session has ended, please log in to continue.";
                return Json(new
                {
                    Error = true,
                    ReturnValue = ""
                }, JsonRequestBehavior.AllowGet);
            }

            var costReports = ProviderBL.GetStatLogs(RetrieveSession.Classification, ProviderID);
            if (costReports == null)
                return Json(new
                {
                    Error = false,
                    ReturnValue = ""
                }, JsonRequestBehavior.AllowGet);
            
            var lstMappedObjects = new List<SelectListItem>();
            foreach (var costReport in costReports)
            {
                var mappedCostReport = new SelectListItem
                {
                    Value = ClientMappingStatLog.ToClientID(costReport.CostReportID.ToString(), StatLogScopes.COST_REPORT_ID_STATLOG),
                    Text = costReport.EndDate.Value.ToShortDateString() + " - " + costReport.AuditTypeDesc.ToString(),
                };
                lstMappedObjects.Add(mappedCostReport);
            }
            return lstMappedObjects != null
                ? Json(new {
                    Error = false,
                    ReturnValue = lstMappedObjects
                }, JsonRequestBehavior.AllowGet)
                : Json(new
                {
                    Error = false,
                    ReturnValue = ""
                }, JsonRequestBehavior.AllowGet);
        }

        [ARUMFeatureActionFilterAttribute("LetterAutomation:Access")]
        public JsonResult GetProviderSpecificCorrespondents(string sProviderID)
        {
            int ProviderID = ClientMappingStatLog.ToInternalID(sProviderID, StatLogScopes.PROVIDER_ID_STATLOG);

            var map = MapByProviderID(ProviderID, null);
            if (map.Error)
                return Json(new { Error = true, ReturnValue = map.ErrorMessage });

            var myUser = RetrieveSession.ARUMUser;
            if (myUser.UserID == -1 || string.IsNullOrWhiteSpace(myUser.Email))
            {
                TempData["Message"] = "Session has ended, please log in to continue.";
                return Json(new
                {
                    Error = true,
                    ReturnValue = ""
                }, JsonRequestBehavior.AllowGet);
            }
            

            var lstMappedObjects = new List<MappedObjects.ProviderCorrespondent>();
            var provider = _providerProxy.GetProviderInfoByARUMProviderID(RetrieveSession.Classification.ARUMToken, ProviderID, "LetterAutomation");
            if (provider != null)
            {
                var addressType = provider.Addresses.Any(a => a.Type == "MLG") ? "MLG" : (provider.Addresses.Any(a => a.Type == "PHY") ? "PHY" : "");
                if (!String.IsNullOrEmpty(addressType))
                {
                    var provAddress = provider.Addresses.FirstOrDefault(a => a.Type == addressType);
                    //Create default provider
                    MappedObjects.ProviderCorrespondent defaultProviderAddress = new MappedObjects.ProviderCorrespondent
                    {
                        CorrespondentID = ClientMappingStatLog.ToClientID(0, StatLogScopes.CORRESPONDENT_ID_STATLOG),
                        Type = "N/A",
                        CorrespondentName = provider.Provider.Name,
                        Address = provAddress.Address1 + (provAddress.Address2 == null ? "" : " " + provAddress.Address2),
                        City = provAddress.City,
                        State = provAddress.State
                    };
                    lstMappedObjects.Add(defaultProviderAddress);
                }
            }

            var correspondents = _providerProxy.GetProviderEffectiveCorrespondents(RetrieveSession.Classification.ARUMToken, ProviderID, "LetterAutomation")?.Correspondents;
            if (correspondents != null)
            { 
                foreach (var correspondent in correspondents)
                {
                    var mappedCorrespondent = new MappedObjects.ProviderCorrespondent
                    {
                        CorrespondentID = ClientMappingStatLog.ToClientID(correspondent.Correspondent.CorrespondentID, StatLogScopes.CORRESPONDENT_ID_STATLOG),
                        Type = correspondent.ContactType,
                        CorrespondentName = correspondent.Contact.FirstName + " " + correspondent.Contact.LastName,
                        Address = correspondent.Address.Address1 + (correspondent.Address.Address2 == null ? "" : " " + correspondent.Address.Address2),
                        City = correspondent.Address.City,
                        State = correspondent.Address.State
                    };
                    lstMappedObjects.Add(mappedCorrespondent);
                }                
            }
            if (lstMappedObjects.Count > 0)
            {
                return lstMappedObjects != null
                    ? Json(new
                    {
                        Error = false,
                        ReturnValue = lstMappedObjects
                    }, JsonRequestBehavior.AllowGet)
                    : Json(new
                    {
                        Error = false,
                        ReturnValue = ""
                    }, JsonRequestBehavior.AllowGet);
            }
            return Json(new
            {
                Error = false,
                ReturnValue = ""
            }, JsonRequestBehavior.AllowGet);
        }

        [ARUMFeatureActionFilterAttribute("LetterAutomation:Access")]
        public JsonResult GetMetadataOptions(string sCostReportID)
        {
            int costReportID = ClientMappingStatLog.ToInternalID(sCostReportID, StatLogScopes.COST_REPORT_ID_STATLOG);

            var map = MapByCostReportID(costReportID, null);
            if (map.Error)
                return Json(new { Error = true, ReturnValue = map.ErrorMessage });

            var costReport = CostReportHistoryBL.GetHistoryByCostReportID(RetrieveSession.Classification, costReportID, RetrieveSession.ARUMUser.UserID, "", "", map.Provider.ARUMProviderID);

            List<HistoryViewModel> lstHistory = costReport.Where(m => m.IsActive && m.XMLDataList.Any()).Select(m =>
                new HistoryViewModel
                {   
                    CreateDateString = m.Stamp.ToString("yyyy-MM-dd"),
                    Name = m.EventName,
                    sHistoryID = ClientMappingStatLog.ToClientID(m.ID, StatLogScopes.HISTORY_ID_STATLOG)
                }).ToList();

            return Json(lstHistory, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("LetterAutomation:Access")]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateStatLogAutomatedLetter(Save myViewModel)
        {
	        ClassificationDTO classification = RetrieveSession.Classification;
	        bool hasMetadataSelected = !string.IsNullOrWhiteSpace(myViewModel.sHistoryID);
            
            Mapping map;
            if (hasMetadataSelected)
            {
                int historyID = ClientMappingStatLog.ToInternalID(myViewModel.sHistoryID, StatLogScopes.HISTORY_ID_STATLOG);
                map = MapByHistoryID(historyID, "LetterAutomation", "GenerateStatLogAutomatedLetter", null);
            }
            else
            {
                int costReportID = ClientMappingStatLog.ToInternalID(myViewModel.CostReportID, StatLogScopes.COST_REPORT_ID_STATLOG);
                map = MapByCostReportID(costReportID, null);
            }
            if (map.Error)
            {
                TempData["Message"] = map.ErrorMessage;
                return RedirectToAction("Index", "LetterAutomation");
            }

            int providerID = map.Provider.ARUMProviderID;
            int correspondentID = ClientMappingStatLog.ToInternalID(myViewModel.CorrespondentID, StatLogScopes.CORRESPONDENT_ID_STATLOG);
            int templateID = ClientMappingStatLog.ToInternalID(myViewModel.TemplateID, StatLogScopes.FILE_TEMPLATE_ID_STATLOG);

            if (correspondentID < 0 || templateID < 0)
            {
                TempData["Message"] = "Error: Could not parse request, please try again.";
                return RedirectToAction("Index", "LetterAutomation");
            }

            try
            {
                if (!StatLogConfiguration.CanAccessLetterAutomation(RetrieveSession.UserRoles))
                    return RedirectToAction("Index", "Secure");
                var myUser = RetrieveSession.ARUMUser;

                //Get File Template
                var template = _providerProxy.GetFileTemplateByID(classification.ARUMToken, templateID, "LetterAutomation");
                if (template == null)
                {
                    TempData["Message"] = "Selected File Template was not found.";
                    return RedirectToAction("Index", "LetterAutomation");
                }
                var fields = template.Fields?.Where(t => t.IsActive);
                var providers = ProviderBL.GetAllARUMProviderInfoByARUMUserID(classification, RetrieveSession.ARUMUser, null).OrderBy(m => m.Name).ToList();
                var dict = new Dictionary<string, string>();
                if (hasMetadataSelected)
                {
                    var metaDataDict = XMLDataBL.GetMetadataDictionary(classification, map.History.XMLDataList.FirstOrDefault());
                    foreach (var metadata in metaDataDict)
                    {
                        dict.Add("{{" + metadata.Key +"}}", metadata.Value);
                    }
                }

                //Add base info to dict                
                dict.Add("Provider ID", providerID.ToString());
                dict.Add("Provider Name", providers.FirstOrDefault(p => p.ARUMProviderID == providerID)?.Name);
                //Add StatLog information to dict
                var statlog = StatLogBL.GetStatLogByStatLogID(classification, map.StatLog.CRID);
                if (statlog != null)
                {
                    foreach (var field in fields.Where(f => f.Field.StartsWith("{{StatLog.")))
                    {
                        var type = statlog.GetType().GetProperties().FirstOrDefault(p => field.Field.Contains(p.Name));
                        if (type != null && !dict.ContainsKey("{{StatLog." + type.Name + "}}"))
                        {
                            if (type.PropertyType == typeof(DateTime))
                                dict.Add("{{StatLog." + type.Name + "}}", ((DateTime)type.GetValue(statlog)).ToString("MMMM dd, yyyy"));
                            else
                                dict.Add("{{StatLog." + type.Name + "}}", type.GetValue(statlog).ToString());
                        }
                    }


                    // Adding {{Statlog.ProvTypeName}} and {{Statlog.AuditTypeName}} to letter automation.
                    ProvTypeDTO provType = ProviderBL.GetProvTypeByProvType(classification, statlog.ProviderType) ?? new ProvTypeDTO();
                    dict.Add("{{Statlog.ProvTypeName}}", provType.Name);

                    AdtTypeDTO auditType = AuditTypeBL.GetAuditTypeByAuditType(classification, statlog.AuditType) ?? new AdtTypeDTO();
                    dict.Add("{{Statlog.AuditTypeName}}", auditType.Name);

				}
				//Generate Docket number and add it to dict if present in template
				if (fields.FirstOrDefault(f => f.Field.Contains("{{LetterAutomation.DocketNo}}")) != null)
                {
                    //Get Application setting for format
                    string docketFormat = ARUMConfiguration.GetSettingByCategory(classification, "DocketNoFormat");
                    if (!string.IsNullOrEmpty(docketFormat))
                    {
                        List<string> hookList = ScanFieldForDataHooks(docketFormat);
                        //Fill in date fields in format
                        var dateHooks = hookList.Where(hook => hook.StartsWith("{{Date|"));
                        if (dateHooks.Any())
                        {
                            foreach (var hook in dateHooks)
                            {
                                var cleanHookString = hook.Replace("{", "").Replace("}", "");
                                DateTime unformattedDate = DateTime.Today;
                                var hookParameters = cleanHookString.Split(new[] { '|' });
                                if (hookParameters.Length == 2)
                                {
                                    var formattedDateString = unformattedDate.ToString(hookParameters[1]);
                                    docketFormat = docketFormat.Replace(hook, formattedDateString);
                                }

                            }
                        }
                        //Get/Create Application Setting hooks
                        var settingHooks = hookList.Where(hook => hook.StartsWith("{{ApplicationSetting."));
                        if (settingHooks.Any())
                        {
                            foreach (var hook in settingHooks)
                            {
                                var cleanHookString = hook.Replace("{", "").Replace("}", "");
                                var hookParameters = cleanHookString.Split(new[] { '.' });
                                if (hookParameters.Length == 2)
                                {
                                    if (hookParameters[1] == "LetterCount")
                                    {
                                        var hookCategory = "LetterCount" + DateTime.Now.Year.ToString();
                                        var settingValue = ARUMConfiguration.GetSettingByCategory(classification, hookCategory);
                                        settingValue = (Int32.TryParse(settingValue, out int value) ? value + 1 : 1).ToString(value < 10000 ? "D4" : "D" + settingValue.Length.ToString());
                                        _applicationProxy.InsertOrUpdateSetting(classification.ARUMToken, hookCategory, settingValue, true, 1, "StatLog-LetterAutomation");
                                        docketFormat = docketFormat.Replace(hook, settingValue);
                                    }
                                }
                            }
                        }
                        dict.Add("{{LetterAutomation.DocketNo}}", docketFormat);
                    }
                }

                var letter = GenerateAutomatedLetter.GenerateLetter(classification.ARUMToken, dict, correspondentID, template, "LetterAutomation", PortalConfiguration.Environment);
                var fileName = template.Template.ExportedFileName;
                //Replace file name hooks with provider information
                fileName = fileName.Replace("[Provider Name]", dict["Provider Name"]);
                fileName = fileName.Replace("[Provider Number]", providers.FirstOrDefault(p => p.ARUMProviderID == providerID)?.Number);
                string ZipFileName = fileName + ".zip";
                var myMemoryStreamForZip = new MemoryStream();
                using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())
                {
                    zip.AddEntry(fileName + ".docx", letter);
                    zip.Save(myMemoryStreamForZip);
                    myMemoryStreamForZip.Position = 0;
                    return File(myMemoryStreamForZip, @"application/zip", ZipFileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return RedirectToAction("Login", "Home");
            }
        }

        [ARUMFeatureActionFilterAttribute("LetterAutomation:Access")]
        public ActionResult ViewMetadata(string sHistoryID)
        {
            int historyID = ClientMappingStatLog.ToInternalID(sHistoryID, StatLogScopes.HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowShowMetadata", null);
            if (map.Error)
                return MessageJson(map.ErrorMessage);

            if (!map.History.XMLDataList.Any())
                return MessageJson("Error: This history does not have XML saved to it.");

            var viewModel = XMLDataBL.GetMetadataByHistoryID(RetrieveSession.Classification, map.History.ID)
                .Select(data => new ViewMetadata
                {
                    ID = data.ID,
                    Name = data.Name,
                    XMLSchemaString = data.XMLSchemaString,
                    DefaultXMLData = data.DefaultXMLData?.ToString() ?? ""
                }).ToList();

            return View(viewModel);
        }

        #region Helper Methods
        private ActionResult MessageJson(string message)
        {
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = new { Error = message }
            };
        }
        private static List<string> ScanFieldForDataHooks(string field)
        {
            var regex = @"({{[\w\.\|\,\s]+}})";
            var hooks = new List<string>();
            // Match the string for any alphanumeric, whitespace, or '.' characters inside double curly brackets.
            var match = Regex.Match(field, regex);

            // For each match found, add it to the list of matches.
            while (match.Success)
            {
                var hook = match.Groups[1].Captures[0].ToString();
                // Make sure we only add it once, the same hook may appear multiple times.
                if (!hooks.Contains(hook))
                {
                    hooks.Add(hook);
                }

                match = match.NextMatch();
            }

            return hooks;
        }
        #endregion
    }
}