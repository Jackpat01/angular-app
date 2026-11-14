// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using Ganss.Xss;
using MSLC.ApplicationProxy.Models;
using MSLC.FileProxy.Interfaces;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.StatLog.BL.AuditType;
using MSLC.StatLog.BL.Event;
using MSLC.StatLog.BL.MappedObjectsNamespace;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.BL.Role;
using MSLC.StatLog.BL.XML;
using MSLC.StatLog.Configuration;
using MSLC.StatLog.Domain;
using MSLC.StatLog.DTO;
using MSLC.StatLog.Enumerations;
using MSLC.StatLog.MVC.ViewModels;
using Newtonsoft.Json;
using Omu.ValueInjecter;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MSLC.ArumProxy.Models;
using MSLC.StatLog.Helpers;
using static MSLC.StatLog.MVC.ViewModels.EventViewModel;
using static MSLC.StatLog.UI.Models.ClientMappingStatLog;
using static MSLC.StatLog.UI.Models.StatLogScopes;

namespace MSLC.StatLog.Controllers
{
    public class EventController : LoggedInBaseController
    {
	    private readonly ClassificationDTO _classification;
	    private readonly ARUMProxy_User _user;
        private readonly HtmlSanitizer _sanitizer;

	    public EventController()
	    {
		    _classification = RetrieveSession.Classification;
		    _user = RetrieveSession.ARUMUser;
            _sanitizer = new HtmlSanitizer();
		}

        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Index(bool showSystemEvents = false)
        {
            var myViewModel = new EventManagementViewModel
            {
                EventList = EventBL.GetEvents(_classification)
                    .Where(e => showSystemEvents || !e.IsSystemEvent).Select(e => 
                        new BasicEventInfo
                        {
                            sEventID = ToClientID(e.ID, UI.Models.StatLogScopes.EVENT_ID_MANAGEMENT),
                            Name = e.Name,
                            Description = e.Descript,
                            IsPHI = e.IsPHI
                        }).ToList()
            };
            return View(myViewModel);
        }

        
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult ManageFileNames()
        {
	        var myViewModel = new ManageFileNamesViewModel.View()
	        {
                FileNames = StatlogARUMConfiguration.GetFileNameTemplates(_classification)
	                .Select(kvp => new ManageFileNamesViewModel.FileNameTemplate()
                {
                    FileName = kvp.Key,
                    Template = kvp.Value
                }).ToList()
	        };

            return View(myViewModel);
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditFileName(string fileName)
        {
	        var fileNames = StatlogARUMConfiguration.GetFileNameTemplates(_classification);
	        fileName = fileName ?? "";
	        var dataHooks = StatlogDatahooks.GetDataHooks(_classification, _user.UserID).Select(kvp => kvp.Key).ToArray();
			var myViewModel = new ManageFileNamesViewModel.Edit
            {
                FileNameTemplate = new ManageFileNamesViewModel.FileNameTemplate()
                {
	                FileName = fileName,
	                Template = fileNames.TryGetValue(fileName, out string template) ? template : "",
                },
                DataHooks = dataHooks
            };
			return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFileName(ManageFileNamesViewModel.Save viewModel, string command)
        {
            var sanitizer = new HtmlSanitizer();
            var fileName = sanitizer.Sanitize(viewModel.FileNameTemplate.FileName);
            var template = sanitizer.Sanitize(viewModel.FileNameTemplate.Template);
            switch (command)
            {
                case "Save":
	                if (string.IsNullOrWhiteSpace(fileName))
	                {
		                TempData["Message"] = $"Invalid File Name given. It can not be empty.";
					} 
	                else if (string.IsNullOrWhiteSpace(template))
	                {
		                TempData["Message"] = $"Invalid Template given. It can not be empty.";
	                }
                    else
					{
						// Update File Name Template
						StatlogARUMConfiguration.SaveFileNameTemplate(_classification,
							fileName,
							template);
                        TempData["Message"] = "Saved Successfully";
                    }
                    break;
                case "Deactivate":
                    break;
                case "Activate":
                    break;
            }
            return RedirectToAction("ManageFileNames", "Event");
        }


        #region Edit Event
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Edit(string sEventID)
        {
            if (String.IsNullOrEmpty(sEventID))
            {
                TempData["Message"] = "Event not found.";
                return RedirectToAction("Index", "Event");
            }
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            //var AllEvents = EventBL.GetEvents(_classification).ToArray();
            var eventDTO = EventBL.GetBasicEventByID(_classification, internalEventID)
                               ?? new EventDTO();

            EventViewModel.Edit myViewModel = new EventViewModel.Edit{
                IsNewEvent = internalEventID == 0,
                sEventID = ToClientID(eventDTO.EventID, EVENT_ID_MANAGEMENT),
                Name = eventDTO.Event,
                Description = eventDTO.Descript
            };
            
            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult Save(EventViewModel.Edit viewModel, string command)
        {
            var eventID = ToInternalID(viewModel.sEventID, EVENT_ID_MANAGEMENT);
            var eventSanitized = _sanitizer.Sanitize(viewModel.Name);
            var descriptionSanitized = _sanitizer.Sanitize(viewModel.Description);
            if (eventID == 0)
            {
                //Insert, redirect to Manage page
                var newEventDTO = new EventDTO
                {
                    Event = eventSanitized,
                    Descript = descriptionSanitized
                };
                int newEventID = EventBL.SaveEventNameDescript(_classification, eventID, eventSanitized, descriptionSanitized);
                var encodedNewEventID = ToClientID(newEventID, EVENT_ID_MANAGEMENT);
                TempData["Message"] = "Event Created Successfully";
                return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = encodedNewEventID }));
            }
            else
            {
                //Update, redirect based on command

                int savedEventID = EventBL.SaveEventNameDescript(_classification, eventID, eventSanitized, descriptionSanitized);
                if (savedEventID < 0)
                {
                    TempData["Message"] = "Error saving Event";
                    return RedirectToAction("Index", "Event");
                }

                TempData["Message"] = "Saved Successfully";
                switch (command)
                {
                    case "Save":
                        return RedirectToAction("Index", "Event");

                    case "SaveToManage":
                        var manageViewModel = new EventViewModel.Manage();
                        return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = viewModel.sEventID }));
                }
            }
            
            return RedirectToAction("Index", "Event");
        }
        #endregion

        #region Manage Event
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult Manage(string sEventID)
        {
            if (String.IsNullOrEmpty(sEventID))
            {
                TempData["Message"] = "Event not found.";
                return RedirectToAction("Index", "Event");
            }
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            //var AllEvents = EventBL.GetEvents(_classification).ToArray();
            var eventDO = EventBL.GetEventByID(_classification, (int)internalEventID)
                               ?? new EventDO(_classification);

            //Set up viewmodel
            EventViewModel.Manage myViewModel = new EventViewModel.Manage
            {
                sEventID = sEventID,
                Name = eventDO.Name,
                EventDescription = eventDO.Descript,
                //Files
                UploadEnabled = eventDO.CanImport ? "True" : "False",
                NAEnabled = eventDO.CanUseNA ? "True" : "False",
                FileNameTemplates = eventDO.FileNameTemplates
            };

            if (eventDO.CanImport)
            {
                var fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(_classification);
                var fileTypes = fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "Event");
                var currentFileType = fileTypes.FileTypes.FirstOrDefault(f => f.ApplicationFileTypeID.ToString() == eventDO.ARUMFileType);
                if (currentFileType != null)
                {
                    myViewModel.CurrentFileTypeID = ToClientID(eventDO.ARUMFileType, FILE_ID_MANAGEMENT);
                    myViewModel.CurrentFileType = currentFileType.ParentFileTypeName + " - " + currentFileType.ChildFileTypeName;
                    var extensionArray = currentFileType.AllowedFileExtensions.ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    var fileCodes = ARUMConfiguration.GetApplicationCodes(_classification).Where(c => c.Type == "AllowedFileExtensions" && c.IsActive && extensionArray.Contains(c.Value.ToLower()));
                    myViewModel.AllowedExtensions = String.Join(", ", fileCodes.Select(c => c.Name));
                }
            }

            //Review
            myViewModel.ReviewType = eventDO.NeedsFileReview ? "File" : eventDO.NeedsTextReview ? "Event" : "Reviews Disabled";
            myViewModel.DisableUploaderReview = eventDO.DisableUploader;
            myViewModel.AcceptReviewText = !String.IsNullOrEmpty(eventDO.ReviewAcceptText) ? eventDO.ReviewAcceptText : "Accept";
            myViewModel.RejectReviewText = !String.IsNullOrEmpty(eventDO.ReviewRejectText) ? eventDO.ReviewRejectText : "Reject";
            myViewModel.EventReviewText = !String.IsNullOrEmpty(eventDO.ReviewText) ? eventDO.ReviewText : "None Set";

            //Due Dates
            myViewModel.NeedsResponse = eventDO.NeedsReply ? "True" : "False";
            if (eventDO.NeedsReply)
            {
                myViewModel.DueDateTrigger = GetDueDateTriggerDropdown(eventDO.DueDateTrigger).FirstOrDefault(b => b.Value == eventDO.DueDateTrigger).Text;
                myViewModel.DueDateType = GetDueDateBaseDropdown(eventDO.DueDateBase).FirstOrDefault(b => b.Value == eventDO.DueDateBase).Text;
                myViewModel.DaysToWait = (eventDO.DueDateBase == "Event" || eventDO.DueDateBase == "StatLog") ? eventDO.Days2Wait.ToString() : "N/A";
                myViewModel.CascadeManualDueDate = eventDO.CascadeManualDueDate ? "True" : "False";
                myViewModel.AddFileDueDate = eventDO.AddFileDueDate ? "True" : "False";
                myViewModel.BulkUpdatable = eventDO.BulkUpdatable ? "True" : "False";
                myViewModel.EventDateEditRefreshDueDate = eventDO.EventDateEditRefreshDueDate ? "True" : "False";
            }

            //Lifecycle
            var applicationRoles = RoleBL.GetApplicationRoles(_classification);
            myViewModel.LifeCycleSummaries = new Dictionary<string, LifeCycleSummary>();
            foreach (var lifecycle in EnumHelper.LifeCycleList)
            {
                var lifeCycleSummary = new LifeCycleSummary
                {
                    Lifecycle = lifecycle.ToString(),
                    EmailEnabled = eventDO.LifeCycles[lifecycle].SendEmail ? "True" : "False",
                    EmailSubject = eventDO.LifeCycles[lifecycle].EmailSubject,
                    Recipients = GenerateRecipientList(eventDO, lifecycle),
                    Contacts = GenerateContactList(eventDO, lifecycle),
                    NewParentEvents = GenerateWorkflowList(eventDO, lifecycle, WorkflowStrategyEnum.NEW_PARENT),
                    SiblingEvents = GenerateWorkflowList(eventDO, lifecycle, WorkflowStrategyEnum.SIBLING),
                    ChildEvents = GenerateWorkflowList(eventDO, lifecycle, WorkflowStrategyEnum.CHILD),
                    FillResponse = eventDO.LifeCycles[lifecycle].TriggerResponseDate ? "True" : "",
                    ClearResponse = eventDO.LifeCycles[lifecycle].ClearResponseDate ? "True" : "",
                    CascadeResponse = eventDO.LifeCycles[lifecycle].CascadeResponseDate ? "True" : "",
                    CloseUploads = eventDO.LifeCycles[lifecycle].CloseEventInstance ? "True" : "",
                    OpenUploads = eventDO.LifeCycles[lifecycle].OpenEventInstance ? "True" : "",
                };
                if (String.IsNullOrEmpty(lifeCycleSummary.NewParentEvents) && String.IsNullOrEmpty(lifeCycleSummary.SiblingEvents) && String.IsNullOrEmpty(lifeCycleSummary.ChildEvents))
                    lifeCycleSummary.AnyWorkFlowEnabled = false;
                if (String.IsNullOrEmpty(lifeCycleSummary.FillResponse) && String.IsNullOrEmpty(lifeCycleSummary.ClearResponse) && String.IsNullOrEmpty(lifeCycleSummary.CascadeResponse) && String.IsNullOrEmpty(lifeCycleSummary.CloseUploads) && String.IsNullOrEmpty(lifeCycleSummary.OpenUploads))
                    lifeCycleSummary.AnyResponseEnabled = false;
                myViewModel.LifeCycleSummaries.Add(lifecycle.ToString(), lifeCycleSummary);
            }

            //MetaData
            myViewModel.MetadataSelectList = XMLSchemaBL
                .GetSchemaListByIDList(_classification, eventDO.XMLSchemaIDList)
                .Select(schema => new MetadataViewModel.SelectList().InjectFrom(schema) as MetadataViewModel.SelectList)
                .ToList();

            //Web Controls
            foreach (var appRole in applicationRoles)
            {
                var dbPermissions = eventDO.GetPermissions(appRole.Name);
                var roleName = appRole.Name.ToUpper().Replace(' ', '_');
                var permissions = new RolePermissionSummary
                {
                    Role = roleName,
                    CanView = dbPermissions.CanView ? "True" : "",
                    CanAdd = dbPermissions.CanAdd ? "True" : "",
                    CanUpload = dbPermissions.CanUpload ? "True" : "",
                    CanDownload = dbPermissions.CanDownload ? "True" : "",
                    CanAcceptReview = dbPermissions.CanAcceptReview ? "True" : "",
                    CanRejectReview = dbPermissions.CanRejectReview ? "True" : "",
                    CanReleaseReview = dbPermissions.CanReleaseReview ? "True" : "",
                    CanAssignUser = dbPermissions.CanAssignUser ? "True" : "",
                    CanHide = dbPermissions.CanHide ? "True" : "",
                    CanEditDueDate = dbPermissions.CanEditDueDate ? "True" : "",
                    CanEditResponseDate = dbPermissions.CanEditResponseDate ? "True" : "",
                    CanEditEventDate = dbPermissions.CanEditEventDate ? "True" : "",
                    CanViewMetadata = dbPermissions.CanViewMetadata ? "True" : "",
                    CanAddMetadata = dbPermissions.CanAddMetadata ? "True" : "",
                };
                //Check if role has any permissions, if so set property (if there is a better way to do these please implement -- GSturgill 5/5/2023)
                if (!String.IsNullOrEmpty(permissions.CanView) || !String.IsNullOrEmpty(permissions.CanAdd) || !String.IsNullOrEmpty(permissions.CanUpload) || !String.IsNullOrEmpty(permissions.CanDownload)
                     || !String.IsNullOrEmpty(permissions.CanAcceptReview) || !String.IsNullOrEmpty(permissions.CanRejectReview) || !String.IsNullOrEmpty(permissions.CanEditResponseDate) || !String.IsNullOrEmpty(permissions.CanReleaseReview) || !String.IsNullOrEmpty(permissions.CanAssignUser)
                      || !String.IsNullOrEmpty(permissions.CanHide) || !String.IsNullOrEmpty(permissions.CanEditDueDate) || !String.IsNullOrEmpty(permissions.CanViewMetadata) || !String.IsNullOrEmpty(permissions.CanAddMetadata) || !String.IsNullOrEmpty(permissions.CanEditEventDate))
                    permissions.AnyPermissions = true;
                myViewModel.RolePermissions.Add(roleName, permissions);
            }

            //Other
            myViewModel.OneTime = eventDO.OneTime ? "True" : "False"; 
            myViewModel.AssignInit = eventDO.EnableAssignUser ? "True" : "False";
            var roleDictionary = applicationRoles.ToDictionary(applicationRole => applicationRole.ID,
                applicationRole => applicationRole.Name);

            myViewModel.SelectedUserAssignableRoles = string.Join(", ", eventDO.AssignableUserRoleList
                .Select(assignableRoleID => roleDictionary.TryGetValue(assignableRoleID, out string roleName) ? roleName : null)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .ToArray());

            if (string.IsNullOrEmpty(myViewModel.SelectedUserAssignableRoles))
                myViewModel.SelectedUserAssignableRoles = "None assigned";

            return View(myViewModel);
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditFiles(string sEventID, string currentApplicationFileTypeID)
        {
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var internalFileTypeID = ToInternalID(currentApplicationFileTypeID, FILE_ID_MANAGEMENT);
            var eventDO = EventBL.GetEventByID(_classification, (int)internalEventID);

            var fileNameTemplates = StatlogARUMConfiguration.GetFileNameTemplates(_classification)
	            .Select(template => template.Key).ToArray();

            var selectedFileNameTemplates = fileNameTemplates
	            .Where(template => eventDO.FileNameTemplates.Contains(template)).ToArray();

            EventViewModel.Files myViewModel = new EventViewModel.Files
            {
                CanImport = eventDO.CanImport,
                CanUseNA = eventDO.CanUseNA,
                FileNameTemplates = fileNameTemplates,
                SelectedFileNameTemplates = string.Join(",", selectedFileNameTemplates)
			};

            var typeList = new List<SelectListItem>();
            var fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(_classification);
            var fileTypes = fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "Event");

            if (string.IsNullOrEmpty(currentApplicationFileTypeID)) typeList.Add(new SelectListItem() { Selected = true, Text = " Select...", Value = "0" });
            if (fileTypes.Results.Successful && fileTypes.FileTypes.Any())
            {
                foreach (IFileType fileType in fileTypes.FileTypes)
                {
                    if (fileType.ParentFileTypeName.ToUpper() != "DOCUMENTS")
                    {
                        var listItem = new SelectListItem   
                        {
                            Text = fileType.DisplayName,
                            Value = ToClientID(fileType.ApplicationFileTypeID, FILE_ID_MANAGEMENT),
                            Selected = internalFileTypeID == fileType.ApplicationFileTypeID
                        };
                        typeList.Add(listItem);
                    }
                }
            }
            myViewModel.SelectARUMFileTypes = new SelectList(typeList, "Value", "Text", internalFileTypeID);
            myViewModel.ARUMFileType = ToClientID(internalFileTypeID, FILE_ID_MANAGEMENT);

            myViewModel.AllowedExtensions = eventDO.WhiteList;
            myViewModel.IsPHI = eventDO.IsPHI;
            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveFileOptions(EventViewModel.Files myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);
            var internalFileTypeID = ToInternalID(myViewModel.ARUMFileType, FILE_ID_MANAGEMENT);
            var fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(_classification);
            var fileType = fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "Event")
	            .FileTypes.FirstOrDefault(f => f.ApplicationFileTypeID == internalFileTypeID);
            
            if (myViewModel.CanImport && fileType == null)
            {
	            TempData["Message"] = "Invalid file type selected for attachments.";
				return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
            }

            var fileNameTemplates = StatlogARUMConfiguration.GetFileNameTemplates(_classification)
	            .Select(template => template.Key).ToArray();
            var selectedTemplates = (myViewModel.SelectedFileNameTemplates ?? "").Split(',').Where(selection => fileNameTemplates.Contains(selection)).ToArray();
            
            var fileTypeExtensions = fileType?.AllowedFileExtensions ?? "";
            EventBL.UpdateEventFileOptions(_classification, internalEventID, myViewModel.CanImport, myViewModel.CanUseNA, internalFileTypeID, fileTypeExtensions, myViewModel.IsPHI, selectedTemplates, _user.UserID);

            TempData["Message"] = "";
            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditReview(string sEventID)
        {
            var myViewModel = new EventViewModel.Review();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var eventDO = EventBL.GetEventByID(_classification, (int)internalEventID);

            myViewModel.sEventID = sEventID;
            myViewModel.NeedsFileReview = eventDO.NeedsFileReview;
            myViewModel.NeedsTextReview = eventDO.NeedsTextReview;
            myViewModel.ReviewAcceptText = eventDO.ReviewAcceptText;
            myViewModel.ReviewRejectText = eventDO.ReviewRejectText;
            myViewModel.DisableUploader = eventDO.DisableUploader;
            myViewModel.ReviewText = eventDO.ReviewText;

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveReviewOptions(EventViewModel.Review myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);

            // Concept for potential technical debt removal
            // EventDO eventDO = EventBL.GetEventByID(_classification, internalEventID);
            // eventDO.NeedsFileReview = myViewModel.NeedsFileReview;
            // eventDO.NeedsTextReview = myViewModel.NeedsTextReview;
            // EventBL.Update(_classification, eventDO, _user.UserID);

            EventBL.UpdateEventReviewOptions(_classification, internalEventID, _user.UserID, 
                myViewModel.NeedsFileReview, 
                myViewModel.NeedsTextReview, 
                myViewModel.ReviewAcceptText, 
                myViewModel.ReviewRejectText, 
                myViewModel.ReviewText,
                myViewModel.DisableUploader);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditDueDates(string sEventID)
        {
            var myViewModel = new EventViewModel.DueDate();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var eventDO = EventBL.GetEventByID(_classification, (int)internalEventID);

            myViewModel.sEventID = sEventID;
            myViewModel.NeedsResponse = eventDO.NeedsReply;
            myViewModel.DueDateBaseList = GetDueDateBaseDropdown(eventDO.DueDateBase);
            myViewModel.DueDateBase = eventDO.DueDateBase;
            myViewModel.DueDateTriggerList = GetDueDateTriggerDropdown(eventDO.DueDateTrigger);
            myViewModel.DueDateTrigger = eventDO.DueDateTrigger;
            myViewModel.CascadeManualDueDate = eventDO.CascadeManualDueDate;
            myViewModel.DaysToWait = eventDO.Days2Wait;
            myViewModel.AddFileDueDate = eventDO.AddFileDueDate;
            myViewModel.BulkUpdatable = eventDO.BulkUpdatable;
            myViewModel.EventDateEditRefreshDueDate = eventDO.EventDateEditRefreshDueDate;

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveDueDateOptions(EventViewModel.DueDate myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);

            EventBL.UpdateDueDateOptions(_classification, internalEventID, _user.UserID, 
                myViewModel.NeedsResponse, myViewModel.DueDateTrigger, myViewModel.DueDateBase, myViewModel.DaysToWait, myViewModel.AddFileDueDate, myViewModel.CascadeManualDueDate, myViewModel.BulkUpdatable, myViewModel.EventDateEditRefreshDueDate);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditLifecycleEmailOptions(string sEventID, LifeCycleEnum lifeCycle)
        {
            var myViewModel = new EventViewModel.Emails();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var lifeCycleDO = EventBL.GetEventByID(_classification, (int)internalEventID).LifeCycles[lifeCycle];

            myViewModel.sEventID = sEventID;
            myViewModel.LifeCycle = lifeCycle;
            myViewModel.SendEmail = lifeCycleDO.SendEmail;
            myViewModel.AutomatedEmailScheduleProcedure = lifeCycleDO.AutomatedEmailScheduleProcedure;
            myViewModel.AutomatedEmailMaximum = lifeCycleDO.AutomatedEmailMaximum;
            myViewModel.AutomatedEmailDelay = lifeCycleDO.AutomatedEmailDelay;
            myViewModel.AutomatedEmailInterval = lifeCycleDO.AutomatedEmailInterval;
            myViewModel.AutomatedEmailDayList = lifeCycleDO.AutomatedEmailDayList;
            myViewModel.ApplicationRoles = RoleBL.GetApplicationRoles(_classification);
            myViewModel.RecipientRoles = lifeCycleDO.RecipientRoles;
            myViewModel.ContactTypes = ARUMConfiguration.GetApplicationCodes(_classification)?.Where(c => c.Type == "Contact" && c.IsActive).Select(c => new ContactSelection {Name = c.Name, Value = c.Value}).ToList();
            myViewModel.RecipientContacts = lifeCycleDO.RecipientContacts;
            myViewModel.EmailCC = lifeCycleDO.EmailCC;
            myViewModel.EmailBCC = lifeCycleDO.EmailBCC;
            myViewModel.SendEmailIndividually = lifeCycleDO.SendEmailIndividually;
            myViewModel.EmailSubject = lifeCycleDO.EmailSubject;
            myViewModel.EmailBody = lifeCycleDO.EmailBody;

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveLifecycleEmailOptions(EventViewModel.Emails myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);
            var sanitizer = new HtmlSanitizer();
            myViewModel.EmailBody = sanitizer.Sanitize(myViewModel.EmailBody);

            EventBL.UpdateEmailOptionsForLifecycle(_classification, internalEventID, myViewModel.LifeCycle, _user.UserID,
                myViewModel.SendEmail, myViewModel.SendEmailIndividually, myViewModel.RecipientRoles, myViewModel.RecipientContacts, myViewModel.EmailCC, myViewModel.EmailBCC, myViewModel.EmailSubject,
                myViewModel.EmailBody, myViewModel.AutomatedEmailDelay, myViewModel.AutomatedEmailInterval, myViewModel.AutomatedEmailMaximum, myViewModel.AutomatedEmailScheduleProcedure, myViewModel.AutomatedEmailDayList);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditLifecycleWorkflowOptions(string sEventID, LifeCycleEnum lifeCycle)
        {
            var myViewModel = new EventViewModel.Workflow();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var workflowDO = EventBL.GetEventByID(_classification, (int)internalEventID).LifeCycles[lifeCycle].Workflow;
            myViewModel.LifeCycle = lifeCycle;

            myViewModel.EventList = EventBL.GetEvents(_classification)
                .Where(e => !e.IsSystemEvent).ToDictionary(e => ToClientID(e.ID, WORKFLOW_EVENT_MANAGEMENT), e => new BasicEventInfo{ sEventID = ToClientID(e.ID, WORKFLOW_EVENT_MANAGEMENT), Name = e.Name, Description = e.Descript });

            var workflowList = workflowDO.Select(flow => new MappedObjects.Workflow().InjectFrom(flow) as MappedObjects.Workflow).ToList();

            myViewModel.WorkflowList = workflowList.Select(mFlow => new WorkflowViewModel
            {
                PrimaryKey = ToClientID(mFlow.PrimaryKey.ToString(), WORKFLOW_PK_MANAGEMENT),
                sEventID = ToClientID(mFlow.EventID, WORKFLOW_EVENT_MANAGEMENT),
                Strategy = mFlow.Strategy,
                ActionEnum = mFlow.ActionEnum,
                AutomaticallyUploadFile = mFlow.AutomaticallyUploadFile,
                Order = mFlow.Order
            }).ToList();

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveLifecycleWorkflowOptions(EventViewModel.Workflow myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);

            var workflow = new List<WorkflowDO>();

            if (myViewModel.WorkflowList != null)
            {
                var mappedWorkflow = myViewModel.WorkflowList.Select(flow => new MappedObjects.Workflow
                {
                    PrimaryKey = ToInternalIDAsNonNullGuid(flow.PrimaryKey, WORKFLOW_PK_MANAGEMENT),
                    EventID = ToInternalID(flow.sEventID, WORKFLOW_EVENT_MANAGEMENT),
                    Strategy = flow.Strategy,
                    ActionEnum = flow.ActionEnum,
                    AutomaticallyUploadFile = flow.AutomaticallyUploadFile,
                    Order = flow.Order
                }).ToList();

                workflow = mappedWorkflow.Select(flow => new WorkflowDO(myViewModel.LifeCycle, flow.PrimaryKey.ToString()).InjectFrom(flow) as WorkflowDO).ToList() ?? new List<WorkflowDO>();

            }
            EventBL.UpdateWorkflowOptionsForLifecycle(_classification, internalEventID, myViewModel.LifeCycle, _user.UserID, workflow);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditLifecycleResponseOptions(string sEventID, LifeCycleEnum lifeCycle)
        {
            var myViewModel = new EventViewModel.Response();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var lifeCycleDO = EventBL.GetEventByID(_classification, (int)internalEventID).LifeCycles[lifeCycle];

            myViewModel.sEventID = sEventID;
            myViewModel.LifeCycle = lifeCycle;
            myViewModel.TriggerResponseDate = lifeCycleDO.TriggerResponseDate;
            myViewModel.ClearResponseDate = lifeCycleDO.ClearResponseDate;
            myViewModel.CascadeResponseDate = lifeCycleDO.CascadeResponseDate;
            myViewModel.CloseEventInstance = lifeCycleDO.CloseEventInstance;
            myViewModel.OpenEventInstance = lifeCycleDO.OpenEventInstance;

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveLifecycleResponseOptions(EventViewModel.Response myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);

            EventBL.UpdateResponseOptionsForLifecycle(_classification, internalEventID, myViewModel.LifeCycle, _user.UserID, myViewModel.TriggerResponseDate, myViewModel.ClearResponseDate,
                myViewModel.CascadeResponseDate, myViewModel.CloseEventInstance, myViewModel.OpenEventInstance);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditMetaData(string sEventID)
        {
            var myViewModel = new EventViewModel.Metadata();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var eventDO = EventBL.GetEventByID(_classification, (int)internalEventID);

            myViewModel.MetadataSelectList = XMLSchemaBL
                .GetSchemaListByIDList(_classification, eventDO.XMLSchemaIDList)
                .Select(schema => new MetadataViewModel.SelectList().InjectFrom(schema) as MetadataViewModel.SelectList)
                .ToList();
            myViewModel.MetadataIDList = string.Join(",", eventDO.XMLSchemaIDList);
            myViewModel.MetadataNamesDropdown = XMLSchemaBL
                .GetAllSchemaNames(_classification)
                .Select(name => new SelectListItem
                {
                    Text = name,
                    Value = name,
                    Disabled = false,
                    Selected = false
                })
                .ToList();

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveMetaData(EventViewModel.Metadata myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);

            var XMLSchemaIDList = (myViewModel.MetadataIDList ?? "").Split(',')
                .Select(x => int.TryParse(x, out int i) ? i : -1)
                .Where(x => x != -1)
                .ToArray();

            EventBL.UpdateEventMetadataOptions(_classification, internalEventID, _user.UserID, XMLSchemaIDList);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditRolePermissions(string sEventID, string applicationRoleName)
        {
            var myViewModel = new EventViewModel.RolePermissionsModel();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var rolePermissions = EventBL.GetEventByID(_classification, (int)internalEventID)?.GetPermissions(applicationRoleName);

            myViewModel.RoleName = applicationRoleName;
            myViewModel.CanView = rolePermissions.CanView;
            myViewModel.CanAdd = rolePermissions.CanAdd;
            myViewModel.CanUpload = rolePermissions.CanUpload;
            myViewModel.CanDownload = rolePermissions.CanDownload;
            myViewModel.CanAcceptReview = rolePermissions.CanAcceptReview;
            myViewModel.CanRejectReview = rolePermissions.CanRejectReview;
            myViewModel.CanReleaseReview = rolePermissions.CanReleaseReview;
            myViewModel.CanAssignUser = rolePermissions.CanAssignUser;
            myViewModel.CanHide = rolePermissions.CanHide;
            myViewModel.CanEditDueDate = rolePermissions.CanEditDueDate;
            myViewModel.CanViewMetadata = rolePermissions.CanViewMetadata;
            myViewModel.CanAddMetadata = rolePermissions.CanAddMetadata;
            myViewModel.CanEditResponseDate = rolePermissions.CanEditResponseDate;
            myViewModel.CanEditEventDate = rolePermissions.CanEditEventDate;

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveRolePermissions(EventViewModel.RolePermissionsModel myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);

            EventBL.UpdateEventRolePermissionsForRole(_classification, internalEventID, myViewModel.RoleName, _user.UserID, myViewModel.CanAdd, myViewModel.CanView, myViewModel.CanAcceptReview,
                myViewModel.CanRejectReview, myViewModel.CanReleaseReview, myViewModel.CanUpload, myViewModel.CanDownload, myViewModel.CanAssignUser, myViewModel.CanHide, myViewModel.CanEditDueDate, myViewModel.CanViewMetadata, 
                myViewModel.CanAddMetadata, myViewModel.CanEditResponseDate, myViewModel.CanEditEventDate);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditOtherOptions(string sEventID)
        {
            var myViewModel = new EventViewModel.Other();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var eventDO = EventBL.GetEventByID(_classification, (int)internalEventID);

            myViewModel.sEventID = sEventID;
            myViewModel.OneTime = eventDO.OneTime;

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveOtherOptions(EventViewModel.Other myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);

            EventBL.UpdateOtherOptions(_classification, internalEventID, _user.UserID, myViewModel.OneTime);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult EditAssignUser(string sEventID)
        {
            var myViewModel = new AssignUser();
            var internalEventID = ToInternalID(sEventID, EVENT_ID_MANAGEMENT);
            var eventDO = EventBL.GetEventByID(_classification, internalEventID);
            
            myViewModel.sEventID = sEventID;
            myViewModel.EnableAssignUser = eventDO.EnableAssignUser;

            var applicationRoles = RoleBL.GetApplicationRoles(_classification);
            var roleDictionary = applicationRoles.ToDictionary(applicationRole => applicationRole.ID, applicationRole => applicationRole.Name);

            var selectedAssignableUserRoles = string.Join(", ", eventDO.AssignableUserRoleList
                .Select(assignableRoleID => roleDictionary.TryGetValue(assignableRoleID, out string roleName) ? roleName : null)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .ToArray());

            myViewModel.ARUMUserRoles = RoleBL.GetApplicationRoles(_classification)
                .ConvertAll(role => new SelectListItem()
                {
                    Text = role.Name,
                    Value = ToClientID(role.ID, ASSIGN_USER_ROLE_ID_EVENT),
                    Selected = selectedAssignableUserRoles.Contains(role.Name)
                });

            return View(myViewModel);
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveAssignedUser(EventViewModel.AssignUser myViewModel)
        {
            var internalEventID = ToInternalID(myViewModel.sEventID, EVENT_ID_MANAGEMENT);
            
            var selectedARUMRoleList = new List<int>(); //this is done for when a user has no selections and the box is unchecked

            if (myViewModel.SelectedARUMRoleList != null)
            {
                selectedARUMRoleList = myViewModel.SelectedARUMRoleList.Select(roleID => ToInternalID(roleID, ASSIGN_USER_ROLE_ID_EVENT)).ToList();
            }
            
            var applicationRoles = RoleBL.GetApplicationRoles(_classification);
            var roleDictionary = applicationRoles.ToDictionary(applicationRole => applicationRole.ID, applicationRole => applicationRole.Name);

            //Filter invalid roleIDs using the dictionary
            selectedARUMRoleList = selectedARUMRoleList.Where(roleID => roleDictionary.ContainsKey(roleID)).ToList();

            EventBL.UpdateUserAssignmentOptions(_classification, internalEventID, _user.UserID,
                myViewModel.EnableAssignUser, selectedARUMRoleList);

            return RedirectToAction("Manage", "Event", new RouteValueDictionary(new { sEventID = myViewModel.sEventID }));
        }

        #endregion

        #region DefaultEvents
        [HttpGet]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult DefaultEvents()
        {
            SelectListItem[] providerTypes = ProviderBL.GetAllActiveProvTypes(_classification)
                .Select(provType => new SelectListItem { Text = provType.Name, Value = provType.ProvType1 }).ToArray();

            SelectListItem[] auditTypes = AuditTypeBL.GetAllActiveAuditTypes(_classification)
                .Select(auditType => new SelectListItem { Text = auditType.Name, Value = auditType.AuditType }).ToArray();

            SelectListItem[] allEvents = EventBL.GetEvents(_classification)
                .Where(e => !e.IsSystemEvent)
                .Select(e => new SelectListItem { Text = e.Name, Value = e.ID.ToString() }).ToArray();

            if (providerTypes.Any())
            {
                providerTypes[0].Selected = true;

                if (auditTypes.Any())
                {
                    auditTypes[0].Selected = true;
                }
            }

            return View(new DefaultEventsViewModel(providerTypes, auditTypes, allEvents));
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult GetDefaultEvents(SelectListItem providerType, SelectListItem auditType)
        {
            return Json(EventBL.GetDefaultEvents(_classification, providerType.Value, auditType.Value)
                .Select(e => new SelectListItem { Text = e.Name, Value = e.ID.ToString() }).ToArray());
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult SaveDefaultEvents(DefaultEventsViewModel.Save viewModel, string command)
        {
            switch (command)
            {
                case "Save":
                    EventBL.SaveDefaultEvents(_classification,
                        viewModel.providerType.Value,
                        viewModel.auditType.Value,
                        viewModel.defaultEvents?
                            .Where(de => int.TryParse(de.Value, out int _)).Select(de => int.Parse(de.Value)).ToArray());
                    break;
            }
            return Json("New Defaults Saved");
        }
        #endregion

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult GetFileTypeExtensions(string pApplicationFileTypeID)
        {
            var success = true;
            var fileTypeExtensions = "";
            var message = "";
            var internalFileType = ToInternalID(pApplicationFileTypeID, FILE_ID_MANAGEMENT);

            var fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(_classification);
            var fileTypes = fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "Event");
            if (fileTypes.Results.Successful && fileTypes.FileTypes.Any())
            {
                var selectedFileType = fileTypes.FileTypes.FirstOrDefault(f => f.ApplicationFileTypeID.ToString() == internalFileType.ToString());
                if (selectedFileType != null)
                {
                    var extensionArray = selectedFileType.AllowedFileExtensions.ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    var fileCodes = ARUMConfiguration.GetApplicationCodes(_classification).Where(c => c.Type == "AllowedFileExtensions" && c.IsActive && extensionArray.Contains(c.Value.ToLower()));
                    fileTypeExtensions = String.Join(", ", fileCodes.Select(c => c.Name));
                    if (string.IsNullOrEmpty(fileTypeExtensions))
                    {
                        success = false;
                        message = "Selected file type does not have any extensions.";
                    }
                }
                else
                {
                    success = false;
                    message = "Could not find selected application file type.";
                }
            }
            else
            {
                success = false;
                message = "Could not retrieve application file types.";
            }
            return Json(new
            {
                Success = success,
                Extensions = fileTypeExtensions,
                Message = message
            });
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult SaveWebControlsReport()
        {
            var eventList = EventBL.GetEvents(_classification)
                    .Where(e => !e.IsSystemEvent).ToList();
            var listRoles = RoleBL.GetApplicationRoles(_classification);

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Xlsx;
                IWorkbook workbook = application.Workbooks.Create(2);
                workbook.Worksheets[1].Name = "Web Controls";
                workbook.Worksheets[0].Name = "Settings";
                var worksheet = workbook.Worksheets[1];

                int rowCount = 1;
                int column = 1;

                worksheet.Range[rowCount, column].Text = "ID";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Event";
                worksheet.SetColumnWidth(column, 30);
                column++;
                worksheet.Range[rowCount, column].Text = "Role";
                worksheet.SetColumnWidth(column, 30);
                column++;
                worksheet.Range[rowCount, column].Text = "View";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Add";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Upload";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Download";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Accept Review";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Reject Review";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Release Review";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Assign User";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Hide Event";
                worksheet.SetColumnWidth(column, 15);
                column++;
                worksheet.Range[rowCount, column].Text = "Edit Due Date";
                worksheet.SetColumnWidth(column, 15);
                column++;

                rowCount++;

                foreach (var eventDO in eventList)
                {
                    foreach (var role in listRoles)
                    {
                        RolePermissionsDO rolePermission = eventDO.GetPermissions(role.Name);
                        column = 1;
                        worksheet.Range[rowCount, column].Text = eventDO.ID.ToString();
                        column++;
                        worksheet.Range[rowCount, column].Text = eventDO.Name.Trim();
                        column++;
                        worksheet.Range[rowCount, column].Text = role.Name;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanView;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanAdd;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanUpload;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanDownload;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanAcceptReview;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanRejectReview;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanReleaseReview;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanAssignUser;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanHide;
                        column++;
                        worksheet.Range[rowCount, column].Boolean = rolePermission.CanEditDueDate;
                        rowCount++;
                    }
                }

                worksheet.AutoFilters.FilterRange = worksheet.Range[1, 2, rowCount, 3];

                worksheet = workbook.Worksheets[0];
                worksheet.SetColumnWidth(1, 15);
                worksheet.SetColumnWidth(2, 25);
                worksheet.Range[1, 1].Text = "Environment";
                worksheet.Range[1, 2].Text = PortalConfiguration.EnvironmentString;
                worksheet.Range[2, 1].Text = "Created By";
                worksheet.Range[2, 2].Text = RetrieveSession.Username;
                worksheet.Range[3, 1].Text = "Time Created";
                worksheet.Range[3, 2].Text = DateTime.Now.ToString();

                //Save the workbook to stream
                string contentType = "Application/msexcel";
                MemoryStream outputStream = new MemoryStream();
                workbook.SaveAs(outputStream);
                outputStream.Position = 0;

                return File(outputStream, contentType, "WebControlsReport.xlsx");
            }
        }

        [HttpPost]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult WebControlSettingsFromReport(string listPreviews)
        {
            try { 
                var changes = JsonConvert.DeserializeObject<List<WebControlPreviewModel.Preview>>(listPreviews);
                var changedIds = changes.Select(m => m.Id).Distinct();
                foreach (var id in changedIds)
                {
                    var eventToChange = EventBL.GetEventByID(_classification, id);
                    var groupChanges = changes.Where(m => m.Id == id);
                    foreach (var change in groupChanges) {
                        RolePermissionsDO rolePermission = eventToChange.GetPermissions(change.Role);

                        rolePermission.CanAdd = change.listPermissions[0].Allowed;
                        rolePermission.CanView = change.listPermissions[1].Allowed;
                        rolePermission.CanUpload = change.listPermissions[2].Allowed;
                        rolePermission.CanDownload = change.listPermissions[3].Allowed;
                        rolePermission.CanAcceptReview = change.listPermissions[4].Allowed;
                        rolePermission.CanRejectReview = change.listPermissions[5].Allowed;
                        rolePermission.CanReleaseReview = change.listPermissions[6].Allowed;
                        rolePermission.CanAssignUser = change.listPermissions[7].Allowed;
                        rolePermission.CanHide = change.listPermissions[8].Allowed;
                        rolePermission.CanEditDueDate = change.listPermissions[9].Allowed;
                    }
                    EventBL.Update(_classification, eventToChange, _user.UserID);
                }
                TempData["Message"] = "Permissions have been successfully updated.";
            }
            catch
            {
                TempData["Message"] = "There was an error updating permissions.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        [ValidateAntiForgeryToken]
        public ActionResult PreviewWebControls(IEnumerable<HttpPostedFileBase> fileUpload, string selector)
        {
            if (fileUpload != null && fileUpload.Count() > 0)
            {
                var listEvents = EventBL.GetEvents(_classification);
                IWorkbook workbook;
                IWorksheet worksheet;

                try
                {
                    HttpPostedFileBase file = fileUpload.First();
                    if (file == null)
                    {
                        throw new Exception("You must select a file to upload.");
                    }
                    if (!file.FileName.EndsWith(".xlsx"))
                    {
                        throw new Exception("Please upload a report file in *.xlsx format.");
                    }

                    using (ExcelEngine excelEngine = new ExcelEngine())
                    {
                        try
                        {
                            workbook = excelEngine.Excel.Workbooks.Open(file.InputStream);
                        }
                        catch
                        {
                            throw new Exception("There was an issue opening the file.");
                        }

                        try
                        {
                            worksheet = workbook.Worksheets[1];
                        }
                        catch
                        {
                            throw new Exception("The uploaded workbook is empty.");
                        }

                        var columnLabels = new Dictionary<int, string>();
                        int row = 1;
                        int column = 1;

                        while (worksheet.Range[row, column].HasString)
                        {
                            columnLabels[column] = worksheet.Range[row, column].Text;
                            column++;
                        }

                        int maxCol = column - 1;
                        string role;
                        bool changed = true;
                        EventDO eventToCompare;
                        var viewModel = new WebControlPreviewModel() { TargetEnvironment = PortalConfiguration.EnvironmentString };
                        row++;

                        while (worksheet.Range[row, maxCol].HasBoolean)
                        {
                            if (selector == "ID" && columnLabels.Values.Contains("ID"))
                            {
                                int eventID = int.Parse(worksheet.Range[row, columnLabels.FirstOrDefault(c => c.Value == "ID").Key].Text);
                                eventToCompare = listEvents.Where(e => e.ID == eventID).FirstOrDefault();
                            }
                            else if (selector == "Name" && columnLabels.Values.Contains("Event"))
                            {
                                string eventName = worksheet.Range[row, columnLabels.FirstOrDefault(c => c.Value == "Event").Key].Text;
                                eventToCompare = listEvents.Where(e => e.Name.Trim() == eventName).FirstOrDefault();
                            }
                            else
                            {
                                throw new Exception("The report does not contain a column identifying the event.");
                            }

                            if (columnLabels.Values.Contains("Role"))
                            {
                                role = worksheet.Range[row, columnLabels.FirstOrDefault(c => c.Value == "Role").Key].Text;
                            }
                            else
                            {
                                throw new Exception("The report does not contain a Role column.");
                            }

                            if (eventToCompare != null && eventToCompare.GetPermissions(role) != null)
                            {
                                column = 4;
                                changed = false;
                                var preview = new WebControlPreviewModel.Preview(eventToCompare, role);

                                while (column <= maxCol)
                                {
                                    switch (columnLabels[column])
                                    {
                                        case "Add":
                                            if (eventToCompare.GetPermissions(role).CanAdd != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[0].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[0].Changed = true;
                                            }
                                            break;
                                        case "View":
                                            if (eventToCompare.GetPermissions(role).CanView != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[1].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[1].Changed = true;
                                            }
                                            break;
                                        case "Upload":
                                            if (eventToCompare.GetPermissions(role).CanUpload != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[2].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[2].Changed = true;
                                            }
                                            break;
                                        case "Download":
                                            if (eventToCompare.GetPermissions(role).CanDownload != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[3].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[3].Changed = true;
                                            }
                                            break;
                                        case "Accept Review":
                                            if (eventToCompare.GetPermissions(role).CanAcceptReview != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[4].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[4].Changed = true;
                                            }
                                            break;
                                        case "Reject Review":
                                            if (eventToCompare.GetPermissions(role).CanRejectReview != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[5].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[5].Changed = true;
                                            }
                                            break;
                                        case "Release Review":
                                            if (eventToCompare.GetPermissions(role).CanReleaseReview != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[6].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[6].Changed = true;
                                            }
                                            break;
                                        case "Assign User":
                                            if (eventToCompare.GetPermissions(role).CanAssignUser != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[7].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[7].Changed = true;
                                            }
                                            break;
                                        case "Hide Event":
                                            if (eventToCompare.GetPermissions(role).CanHide != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[8].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[8].Changed = true;
                                            }
                                            break;
                                        case "Edit Due Date":
                                            if (eventToCompare.GetPermissions(role).CanEditDueDate != worksheet.Range[row, column].Boolean)
                                            {
                                                changed = true;
                                                preview.listPermissions[9].Allowed = worksheet.Range[row, column].Boolean;
                                                preview.listPermissions[9].Changed = true;
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                    column++;
                                }
                                if (changed)
                                {
                                    viewModel.listPreviews.Add(preview);
                                }
                                row++;
                            }
                        }
                        worksheet = workbook.Worksheets[0];
                        row = 1;
                        column = 1;
                        while (worksheet.Range[row, column].HasString)
                        {
                            if (worksheet.Range[row, column].Text == "Environment")
                            {
                                viewModel.SourceEnvironment = worksheet.Range[row, column + 1].Text;
                            }
                            row++;
                        }
                        if (viewModel.listPreviews.Count > 0)
                        {
                            return View(viewModel);
                        }
                        else
                        {
                            TempData["Message"] = "There are no changes to preview in the selected report.";
                            return RedirectToAction("Index");
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["Message"] = ex.Message;
                }

            }
            return RedirectToAction("Index");
        }

        [ARUMFeatureActionFilterAttribute("Statlog:Configuration")]
        public ActionResult UploadReport()
        {
            var viewModel = new WebControlsUploadModel();
            return View(viewModel);
        }

        #region Helper Methods
        public static object AddEventText(LifeCycleEnum lifeCycle)
        {
            switch (lifeCycle)
            {
                case LifeCycleEnum.DUE_DATE_HIT:
                    return "when the due date is hit.";
                case LifeCycleEnum.DUE_DATE_REMINDER:
                    return "when the due date is approaching.";
                case LifeCycleEnum.EVENT_ADDED:
                    return "when this event is added to the stat log.";
                case LifeCycleEnum.FILE_UPLOADED:
                    return "when a file is uploaded to this event.";
                case LifeCycleEnum.REVIEW_APPROVED:
                    return "when this event has a review approved.";
                case LifeCycleEnum.REVIEW_REJECTED:
                    return "when this event has a review rejected.";
                case LifeCycleEnum.REVIEW_RELEASED:
                    return "when the review on this event is released.";
                case LifeCycleEnum.USER_ASSIGNED:
                    return "when a user is assigned to this event.";
                case LifeCycleEnum.USER_REMOVED:
                    return "when a user is removed from this event. (When a new user is assigned, the old user is removed.)";
                case LifeCycleEnum.METADATA_ADDED:
                    return "when a user adds metadata to this event.";
                case LifeCycleEnum.NA_EVENT_ADDED:
                    return "when a file has been marked as N/A";
                default:
                    // When adding a new life cycle, you are subscribing to all the functionality of the other life cycles.
                    throw new ArgumentOutOfRangeException();
            }
        }
        private SelectList GetARUMFileTypeDropdown(string preSelectedFileType = null)
        {
            var TypeList = new List<SelectListItem>();
            var _fileServiceProxy = ARUMFile.GetARUMFileServiceProxy(_classification);
            var fileTypes = _fileServiceProxy.GetApplicationFileTypesByApplicationID(_user.UserID, "Event");
            if (string.IsNullOrEmpty(preSelectedFileType)) TypeList.Add(new SelectListItem() { Selected = true, Text = " Select...", Value = "0" });
            if (fileTypes.Results.Successful && fileTypes.FileTypes.Any())
            {
                foreach (IFileType fileType in fileTypes.FileTypes)
                {
                    if (fileType.ParentFileTypeName.ToUpper() != "DOCUMENTS")
                    {
                        var listItem = new SelectListItem
                        {
                            Text = fileType.DisplayName,
                            Value = fileType.ApplicationFileTypeID.ToString(),
                            Selected = preSelectedFileType == fileType.ApplicationFileTypeID.ToString()
                        };
                        TypeList.Add(listItem);
                    }
                }
            }
            return new SelectList(TypeList, "Value", "Text", preSelectedFileType);
        }

        private SelectList GetDueDateBaseDropdown(string preSlectedBase = null)
        {
            Dictionary<string, string> DueDateBaseList = DueDateConfiguration.GenerateDueDateBaseSelectList(StatlogARUMConfiguration.GetDueDateBaseList(_classification));
            var dueDateBaseItems = DueDateBaseList.Select(m => new SelectListItem()
            {
                Text = m.Value,
                Value = m.Key
            });
            return new SelectList(dueDateBaseItems, "Value", "Text");
        }

        private SelectList GetDueDateTriggerDropdown(string preSelectedTrigger = null)
        {
            var DueDateTriggerList = EnumHelper.LifeCycleList;

            if (StatlogARUMConfiguration.HideMetaDataManagement(_classification))
            {
                var lifecycleList = DueDateTriggerList.ToList();
                lifecycleList.Remove(LifeCycleEnum.METADATA_ADDED);
                DueDateTriggerList = lifecycleList.ToArray();
            }

            var dueDateTriggerItems = DueDateTriggerList.Where(l => !l.ToString().Contains("DUE_DATE")).Select(m => new SelectListItem()
            {
                Text = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.ToString().Replace('_', ' ').ToLower()),
                Value = m.ToString()
            });
            return new SelectList(dueDateTriggerItems, "Value", "Text");
        }

        private string GenerateRecipientList(EventDO Event, LifeCycleEnum Lifecycle)
        {
            if (String.IsNullOrEmpty(Event.LifeCycles[Lifecycle].RecipientRoles))
                return "";
            var Roles = RoleBL.GetApplicationRoles(_classification);
            var RecipientRoleList = Event.LifeCycles[Lifecycle].RecipientRoles.Split(new Char[] {';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            var recipientRoleNames = Roles.Where(r => RecipientRoleList.Contains(r.ID.ToString())).Select(r => r.Name).ToList();

            return String.Join(", ", recipientRoleNames.ToArray());
        }

        private string GenerateContactList(EventDO Event, LifeCycleEnum Lifecycle)
        {
            if (String.IsNullOrEmpty(Event.LifeCycles[Lifecycle].RecipientContacts))
                return "";
            var ContactTypes = ARUMConfiguration.GetApplicationCodes(_classification)?.Where(c => c.Type == "Contact" && c.IsActive).ToList();
            var RecipientContactList = Event.LifeCycles[Lifecycle].RecipientContacts.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var recipientContactNames = ContactTypes.Where(r => RecipientContactList.Contains(r.Value.ToString())).Select(r => r.Name).ToList();

            return String.Join(", ", recipientContactNames.ToArray());
        }

        private string GenerateWorkflowList(EventDO Event, LifeCycleEnum Lifecycle, WorkflowStrategyEnum WorkflowStrategy)
        {
            var EventList = EventBL.GetEvents(_classification);
            var WorkflowForStrategy = Event.LifeCycles[Lifecycle].Workflow.Where(w => w.Strategy == WorkflowStrategy).OrderBy(w => w.Order).Select(w => w.EventID).ToList();
            var workflowEventNames = EventList.Where(e => WorkflowForStrategy.Contains(e.ID)).Select(e => e.Name).ToList();

            return String.Join(", ", workflowEventNames.ToArray());
        }
        #endregion
    }
}