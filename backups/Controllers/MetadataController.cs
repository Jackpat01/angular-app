// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Xml.Schema;
using Ganss.Xss;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.StatLog.BL.EventLifeCycle;
using MSLC.StatLog.BL.XML;
using MSLC.StatLog.Controllers;
using MSLC.StatLog.Domain;
using MSLC.StatLog.DTO;
using MSLC.StatLog.MVC.ViewModels;
using MSLC.StatLog.StatlogUtility.XML;
using MSLC.StatLog.UI.Models;
using Omu.ValueInjecter;
using static MSLC.StatLog.UI.Models.StatLogMapping;

namespace MSLC.StatLog.UI.Controllers
{
    public class MetadataController : LoggedInBaseController
    {
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Index()
        {
            var model = new MetadataViewModel.Index
            {
                SchemaNames = XMLSchemaBL.GetAllSchemaNames(RetrieveSession.Classification).Select(schema => new SelectListItem
                {
                    Text = schema,
                    Value = schema,
                    Disabled = false,
                    Selected = false,
                }).ToList()
            };

            return View(model);
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult GetVersionsFromName(string name)
        {
            return Json(XMLSchemaBL.GetAllSchemaVersions(RetrieveSession.Classification, name)
                .Select(schema => new MetadataViewModel.SelectList().InjectFrom(schema) as MetadataViewModel.SelectList)
                .ToList());
        }

        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Manage(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Message"] = "Issue retrieving metadata versions for the selected Metadata";
                return RedirectToAction("Index");
            }

            var versionList = XMLSchemaBL.GetAllSchemaVersions(RetrieveSession.Classification, name);

            if (!versionList?.Any() ?? true)
            {
                TempData["Message"] = "No metadata versions could be found for the selected Metadata";
                return RedirectToAction("Index");
            }

            var model = new MetadataViewModel.Manage
            {
                Name = name,
                Versions = versionList.Select(schema => new SelectListItem
                {
                    Text = schema.VersionNumber.ToString(),
                    Value = schema.ID.ToString(),
                    Disabled = false,
                    Selected = false,
                }).ToList()
            };
            return View(model);
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Edit(int schemaID, string name = "")
        {
            var model = new MetadataViewModel.Edit
            {
                ID = schemaID,
                Name = name,
                VersionNumber = 1,
                DefaultXMLData = "",
                XMLSchemaString = "",
                Editable = true
            };

            if (schemaID == 0) return View(model);

            XMLSchemaDTO schema = XMLSchemaBL.GetSchemaByID(RetrieveSession.Classification, schemaID);

            if (schema == null)
            {
                TempData["Message"] = $"Issue retrieving selected Metadata (ID: {schemaID})";
                return RedirectToAction("Index");
            }
            
            model.DefaultXMLData = schema.DefaultXMLData?.ToString() ?? "";
            model.VersionNumber = schema.VersionNumber;
            model.XMLSchemaString = schema.XMLSchemaString;
            model.Editable = !XMLDataBL.SchemaHasBeenUsed(RetrieveSession.Classification, schemaID);

            return View(model);
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult ViewKeys(int schemaID)
        {
            if (schemaID == 0) 
                return MessageJson("Unable to load keys for the selected version.");

            string[] keys = XMLSchemaBL.GetMetadataDictionary(RetrieveSession.Classification, schemaID)?.Keys.ToArray() ?? new string[]{};

            var viewModel = new MetadataViewModel.ViewKeys
            {
                Keys = keys
            };
            return View(viewModel);
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(MetadataViewModel.Save newSchema)
        {
            if (ModelState.IsValid)
            {
                #region Parse XML Data
                XElement defaultXMLData = null;
                if (!string.IsNullOrWhiteSpace(newSchema.DefaultXMLData))
                {
                    try
                    {
                        defaultXMLData = XDocument.Parse(newSchema.DefaultXMLData).Root;
                    }
                    catch
                    {
                        return SaveRedirect(newSchema.Name, "Default XML could not be parsed. Save cancelled.");
                    }
                }
                #endregion

                #region Parse XML Schema
                if (!string.IsNullOrWhiteSpace(newSchema.XMLSchemaString))
                {
                    try
                    {
                        XmlSchemaSet schemaSet = new XmlSchemaSet();
                        TextReader textReader = new StringReader(newSchema.XMLSchemaString);
                        string error = null;
                        schemaSet.Add(XmlSchema.Read(textReader, (sender, args) => error = $"Error Reading XML Schema. Error Message: {args.Message}."));
                        schemaSet.Compile();

                        XmlSchema schema = null;
                        foreach (XmlSchema xmlSchema in schemaSet.Schemas())
                        {
                             schema = xmlSchema;
                             break;
                        }

                        if (!string.IsNullOrWhiteSpace(error))
                            return SaveRedirect(newSchema.Name, $"{error} Save Cancelled.");

                        if (schema == null)
                            return SaveRedirect(newSchema.Name, "XML Schema could not be compiled. Saved cancelled.");
                    }
                    catch
                    {
                        return SaveRedirect(newSchema.Name, "XML Schema could not be parsed. Save cancelled.");
                    }
                }
                #endregion

                var sanitizer = new HtmlSanitizer();

                var saveDTO = new XMLSchemaDTO
                {
                    ID = newSchema.ID,
                    Name = sanitizer.Sanitize(newSchema.Name),
                    DefaultXMLData = defaultXMLData,
                    XMLSchemaString = newSchema.XMLSchemaString
                };

                if (newSchema.ID != 0)
                {
                    string message = XMLSchemaBL.Update(RetrieveSession.Classification, saveDTO, RetrieveSession.ARUMUser.UserID);
                    return SaveRedirect(newSchema.Name, message);
                }

                XMLSchemaBL.Insert(RetrieveSession.Classification, saveDTO, RetrieveSession.ARUMUser.UserID);
                return SaveRedirect(saveDTO.Name, $"Saved {saveDTO.Name} (V{ saveDTO.VersionNumber}) successfully.");
            }

            return SaveRedirect(newSchema.Name, "Invalid values when trying to save. Save Cancelled.");
        }

        #region Add Metadata
        [HttpGet]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        public ActionResult ShowAddMetadata(string sHistoryID)
        {
            int historyID = ClientMappingStatLog.ToInternalID(sHistoryID, StatLogScopes.HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowNotApplicable", "Statlog:Interact");
            if (map.Error)
                return MessageJson(map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return MessageJson("Error: This history is not viewable.");

            if (!history.HasPermissionToAddMetadata(new[] { RetrieveSession.CurrentRole }))
                return MessageJson("Error: You do not have permission to add metadata to this history.");

            if (!history.Event.XMLSchemaIDList.Any() || history.XMLDataList.Any())
                return MessageJson("Error: This history does not have any metadata you can add.");

            var missingMetadata = history.Event.XMLSchemaIDList.Where(id => !history.XMLDataList.Contains(id)).ToArray();
            if (!missingMetadata.Any())
                return MessageJson("Error: This history has no metadata that needs to be filled.");

            var xmlSchemas = XMLSchemaBL.GetSchemaListByIDList(RetrieveSession.Classification, missingMetadata)
                .Select(schema => new AddMetadataViewModel.XMLSchemaList
                {
                    ID = schema.ID,
                    Name = schema.Name,
                    XMLSchemaString = schema.XMLSchemaString,
                    DefaultXMLData = schema.DefaultXMLData?.ToString() ?? ""
                }).ToList();

            var myViewModel = new AddMetadataViewModel.View
            {
                sHistoryID = sHistoryID,
                InitialEventName = history.Event.IsSystemEvent ? history.ParentHistory?.EventName ?? history.EventName : history.EventName,
                XMLSchemas = xmlSchemas
            };
            return View(myViewModel);
        }

        [HttpPost]
        [SelectedProviderFeatureFilter("Statlog:Interact", "StatLog", "Index")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveAddMetadata(AddMetadataViewModel.Save myViewModel)
        {
            int historyID = ClientMappingStatLog.ToInternalID(myViewModel.sHistoryID, StatLogScopes.HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "SaveNotApplicable", "Statlog:Interact");
            if (map.Error)
                return StatlogMessageRedirect(map, map.ErrorMessage);

            HistoryDO history = map.History;

            if (!history.Viewable(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: This history is not viewable.");

            if( !history.HasPermissionToAddMetadata(new[] { RetrieveSession.CurrentRole }))
                return StatlogMessageRedirect(map, "Error: You do not have permission to add metadata to this history.");

            if (!history.Event.XMLSchemaIDList.Any() || history.XMLDataList.Any())
                return StatlogMessageRedirect(map, "Error: This history does not have any metadata you can add.");

            bool missingSchema = history.Event.XMLSchemaIDList.Any(schemaID => myViewModel.XMLData.All(x => x.ID != schemaID));

            if (missingSchema)
                return StatlogMessageRedirect(map, "Event Missing required Metadata");

            var XMLSchemas = XMLSchemaBL.GetSchemaListByIDList(RetrieveSession.Classification, history.Event.XMLSchemaIDList);
            foreach (XMLSchemaDTO xmlSchemaDTO in XMLSchemas)
            {
                string data = myViewModel.XMLData.FirstOrDefault(d => d.ID == xmlSchemaDTO.ID)?.Data;
                string validationMessage = XMLValidation.Validate(xmlSchemaDTO.XMLSchemaString, data);
                if (validationMessage != null)
                {
                    return StatlogMessageRedirect(map, validationMessage);
                }
            }

            var xmlDataDTO = myViewModel.XMLData.Select(e => new XMLDataDTO
            {
                Data = XDocument.Parse(e.Data).Root,
                XMLSchemaID = e.ID
            }).ToList();

            XMLDataBL.SaveXMLAndHistory(RetrieveSession.Classification, map.History.ID, xmlDataDTO,
                RetrieveSession.ARUMUser.UserID);

            var _classification = RetrieveSession.Classification;
            var _arumUserProxy = ARUMUser.GetArumServiceProxy(_classification);
            var _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(_classification);
            var _providerServiceProxy = ARUMProvider.GetARUMProviderServiceProxy(_classification);
            var processLifeCycle = new ProcessLifeCycle(_classification, PortalURL, _arumUserProxy, _fileServiceProxy, _providerServiceProxy);
            processLifeCycle.MetadataAdded(map.History.ToDTO(), map.History.Event, RetrieveSession.ARUMUser, RetrieveSession.IsMSLCEmployee, map.StatLog, map.Provider, map.History.FileID);
            
            return StatlogMessageRedirect(map, "Metadata Successfully Saved to History");
        }
        #endregion

        [SelectedProviderFeatureFilter("Statlog:View", "StatLog", "Index")]
        public ActionResult ShowMetadata(string sHistoryID)
        {
            int historyID = ClientMappingStatLog.ToInternalID(sHistoryID, StatLogScopes.HISTORY_ID_STATLOG);

            Mapping map = MapByHistoryID(historyID, "StatLog", "ShowShowMetadata", "Statlog:View");
            if (map.Error)
                return MessageJson(map.ErrorMessage);

            if (!map.History.Viewable(new[] { RetrieveSession.CurrentRole }))
                return MessageJson("Error: This history is not viewable.");

            if (!map.History.HasPermissionToViewMetadata(new[] { RetrieveSession.CurrentRole }))
                return MessageJson("Error: You can not view the metadata on this History.");

            if (!map.History.XMLDataList.Any())
                return MessageJson("Error: This history does not have XML saved to it.");


            var viewModel = XMLDataBL.GetMetadataByHistoryID(RetrieveSession.Classification, map.History.ID)
                .Select(data => new MetadataViewModel.View
                {
                    ID = data.ID,
                    Name = data.Name,
                    XMLSchemaString = data.XMLSchemaString,
                    DefaultXMLData = data.DefaultXMLData?.ToString() ?? ""
                }).ToList();

            return View(viewModel);
        }

        #region Helper Methods
        private ActionResult SaveRedirect(string name, string message)
        {
            TempData["Message"] = message;
            return XMLSchemaBL.DoesNameExist(RetrieveSession.Classification, name)
                ? RedirectToAction("Manage", "Metadata", new { name })
                : RedirectToAction("Index");
        }

        private ActionResult StatlogMessageRedirect(int? providerID, int? costReportID, string message)
        {
            TempData["CostReportID"] = costReportID;
            TempData["ARUMProviderID"] = providerID;
            TempData["Message"] = message;
            return RedirectToAction("Index", "StatLog");
        }

        private ActionResult StatlogMessageRedirect(Mapping request, string message) =>
            StatlogMessageRedirect(request?.Provider?.ARUMProviderID, request?.StatLog?.CRID, message);

        private ActionResult MessageJson(string message)
        {
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = new { Error = message }
            };
        }
        private static string PortalURL =>
            new UrlHelper(((MvcHandler)System.Web.HttpContext.Current.Handler).RequestContext).AbsoluteAction("Login", "Home");
        #endregion
    }
}