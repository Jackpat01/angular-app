// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using MSLC.ArumProxy.Interfaces;
using MSLC.Portal.MVC.Utility;
using MSLC.StatLog.Configuration;
using MSLC.StatLog.Domain;
using MSLC.StatLog.MVC.MVC;
using MSLC.StatLog.MVC.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using MSLC.ArumProxy.Models;
using MSLC.Portal.Configuration;
using MSLC.StatLog.BL.CostReportHistory;
using MSLC.StatLog.Helpers;
using static MSLC.StatLog.UI.Models.ClientMappingStatLog;
using static MSLC.StatLog.UI.Models.StatLogScopes;
using static System.String;
using System;

// ReSharper disable UnusedParameter.Local

namespace MSLC.StatLog.Models
{
    public static class GenerateActionItems
    {
        public static void generateActionItems(StatLogViewModel.View myViewModel)
        {
            var classification = RetrieveSession.Classification;
            var userRoles = new[] { RetrieveSession.CurrentRole };
            var userEmail = RetrieveSession.ARUMUser.Email;
            var statLogTable = new List<StatLogViewModel.StatLogRow>();
            if (myViewModel.myCostReportHistory != null && myViewModel.myCostReportHistory.Any())
            {
                var requestContext = HttpContext.Current.Request.RequestContext;
                var urlHelper = new UrlHelper(requestContext);
                int row = 0;
                foreach (var history in myViewModel.myCostReportHistory)
                {
                    // If you can't view the event, skip it.
                    if(!history.Viewable(userRoles))
                        continue;

                    var actionItemHtml = "";
                    var notesItemHtml = "";

                    #region Action column icons

                    if ((history.Event.IsPHI || (history.ParentHistory?.IsPHI ?? false)) && history.Event.IsSystemEvent)
                    {
                        actionItemHtml += AddPHIDownloadIcon(urlHelper, history, userRoles, row, myViewModel.sProviderID, myViewModel.readOnly);
                    }
                    else if (history.Event.IsSystemEvent)
                    {
                        actionItemHtml += AddDownloadIcon(urlHelper, history, userRoles, row, myViewModel.readOnly, myViewModel.sProviderID);
                    }

                    actionItemHtml += AddEditDateIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    actionItemHtml += AddReviewIcon(urlHelper, history, userRoles, row, myViewModel.readOnly, userEmail);//, applicationFileTypes);

                    if (history.Event.IsPHI || (history.ParentHistory?.Event?.IsPHI ?? false))
                    {
                        actionItemHtml += AddPHIUploadIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    }
                    else
                    {
                        actionItemHtml += AddUploadIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    }
                    
                    actionItemHtml += AddAssignUserIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    actionItemHtml += AddHideEventIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    actionItemHtml += AddShowEventIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    actionItemHtml += AddAddMetadataIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    actionItemHtml += AddShowNotApplicableInfoIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);

                    #endregion

                    #region Details column icons

                    notesItemHtml += AddViewNotesIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    notesItemHtml += AddEmailIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    notesItemHtml += AddViewIssueIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    notesItemHtml += AddShowFileInfoIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    notesItemHtml += AddReviewStatusIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);//, applicationFileTypes);
                    notesItemHtml += AddViewMetadataIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    notesItemHtml += AddNotApplicableIcon(urlHelper, history, userRoles, row, myViewModel.readOnly);
                    
                    #endregion

                    string myEvent = history.Event.Name.ToUpper() == "NA" ? "N/A" : history.Event.Name;

                    if (history.ParentHistory != null)
                    {
                        myEvent = $"{history.Spacing}{myEvent}";
                    }

                    //var showParentEventNameOnChildEvent =
                    //    ARUMHandler.GetCategory(classification, "FeatureRoles:StatLog:ShowParentEventNameOnChildEvent", "false")
                    //        ?.ToUpper().Equals("TRUE") ?? false;

                    //if (showParentEventNameOnChildEvent)
                    //{
                    //    history.ParentHistory = CostReportHistoryBL.GetHistoryByHistoryID(
                    //            classification, history.ID, RetrieveSession.ARUMUser.UserID,
                    //            "GenerateActionItems", "GenerateActionItems")
                    //        .ParentHistory;

                    //    if (history.ParentHistory != null)
                    //    {
                    //        myEvent += $" - {history?.ParentHistory?.EventName}";
                    //    }
                    //}

                    if (history.Event.EnableAssignUser)
                        myEvent = !IsNullOrEmpty(history.UserAssignedName)
                            ? $"{myEvent} - Assigned To {history.UserAssignedName.Trim()}"
                            : $"{myEvent} - Unassigned";

                    statLogTable.Add(new StatLogViewModel.StatLogRow
                    {
                        HistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG),
                        ParentHistoryID = ToClientID(history.ParentHistory?.ID, HISTORY_ID_STATLOG),
                        UserName = history.UserName,
                        Response = history.Response,
                        ResponseDateString = history.Response.ToString(),
                        Event = myEvent,
                        EventDate = history.EventDate,
                        EventDateString = history.EventDate.ToString(),
                        ExpectDate = history.ExpectDate,
                        ExpectDateString = history.ExpectDate.ToString(),
                        actionItemHtml = actionItemHtml,
                        notesItemHtml = notesItemHtml,
                        FileName = history.FileName,
                        IsActive = history.IsActive
                    });
                    
                    row++;
                }
            }
            myViewModel.StatLogTable = statLogTable;
        }

        private static string AddDownloadIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly, string sProviderID)
        {
            var providerID = ToInternalID(sProviderID, PROVIDER_ID_STATLOG);

            bool isPHISafe = ProviderUserHelper.IsProviderUserPhiSafe(providerID, RetrieveSession.ARUMUser);
            if (!history.Downloadable(roles, isPHISafe))
                return Empty;

            if (!history.FileFoundInARUM)
                return "<a title='File Inactive' onclick='return false' style='cursor: not-allowed;opacity: 0.5;'><i data-feather='download-cloud'></i></a>";

            if (!history.IsActive)
                return "<a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='download-cloud'></i></a>";

            string urlString = url.AbsoluteAction("DownloadFile", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchDownloadFile{row}' title='Download'><i data-feather='download-cloud'></i></a>";
        }

        private static string AddEditDateIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        { 
            //This check means you can only modify a due date if it's present.  Can't modify blank due dates.
            bool canEditResponseDate = history.HasPermissionToEditResponseDate(new[] { RetrieveSession.CurrentRole });
            bool canEditDueDate = history.ExpectDate != null && history.HasPermissionToEditDueDate(new[] { RetrieveSession.CurrentRole });
            bool canEditEventDate = history.EventDate != null && history.HasPermissionToEditEventDate(new[] {RetrieveSession.CurrentRole});

            if (!canEditResponseDate && !canEditDueDate && !canEditEventDate)
                return Empty;//This check means you can only modify a due date if it's present.  Can't modify blank due dates.

            if (!history.IsActive || userIsReadOnly)
            return "<a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='calendar'></i></a>";

            string urlString = url.AbsoluteAction("EditEventDate", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a title='Changes a date.' href={urlString} id='EditEventDate{row}' onclick=\"return modalButton(this, 'tallerWidest')\"><i data-feather='calendar'></i></ a>";
        }

        private static string AddReviewIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly, string userEmail)
        {
            if (!history.Reviewable(roles, userEmail))
                return Empty;

            if (!history.IsActive || userIsReadOnly)
                return "<a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='zap'></i></a>";

            string urlString = url.AbsoluteAction("Review", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchReview{row}' onclick=\"return modalButton(this, '9080')\" title='Review'><i data-feather='zap' class='text-warning'></i></a>";
        }

        private static string AddUploadIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.Uploadable(roles))
                return Empty;

            if (!history.IsActive || userIsReadOnly)
                return "<a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='upload-cloud'></i></a><a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='flag'></i></a>";

            string uploadButtonURL = url.AbsoluteAction("Upload", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            var uploadButton = $"<a href={uploadButtonURL} id='anchUploadFile{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='Upload'><i data-feather='upload-cloud'></i></a>";

            string naButtonURL = url.AbsoluteAction("ShowNotApplicable", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            var naButton = $"<a href={naButtonURL} id='anchNotApplicable{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='Report Issue'><i data-feather='flag'></i></a>";

            return uploadButton + naButton;
        }

        /// <summary>
        /// Adds the new N/A (not applicable) icon to the NA Event
        /// </summary>
        /// <param name="url"></param>
        /// <param name="history"></param>
        /// <param name="roles"></param>
        /// <param name="row"></param>
        /// <param name="userIsReadOnly"></param>
        /// <returns></returns>
        private static string AddNotApplicableIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (history.EventID != StatLogConfiguration.EventIDForNA)
            {
                return Empty;
            }

            string naButtonURL = url.AbsoluteAction("ShowNA", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            var naButton = $"<a href={naButtonURL} id='anchNotApplicable{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='View N/A'><i data-feather='alert-circle' class='text-primary'></i></a>";

            return naButton;
        }

        private static string AddReviewStatusIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.HasLastReview) 
                return Empty;

            const string reviewOK = "Review is OK";
            const string reviewNotOK = "Item not accepted";

            string urlString = url.AbsoluteAction("ShowReviewInfo", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return history.IsLastReviewOkay ? 
                $"<a href={urlString} id='anchReviewStatus{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='{reviewOK}'><i data-feather='check-circle' class='text-success'></i></a>" : 
                $"<a href={urlString} id='anchReviewStatus{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='{reviewNotOK}'><i data-feather='slash' class='text-danger'></i></a>";
        }

        private static string AddShowFileInfoIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.HasFileUpload) 
                return Empty;

            string urlString = url.AbsoluteAction("ShowFileInfo", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchShowFileHistory{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='Show File Info'><i data-feather='info' class='infoButton'></i></a>";
        }

        private static string AddViewIssueIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (IsNullOrWhiteSpace(history.Notes) || history.Event.ID != StatLogConfiguration.EventIDForIssue) 
                return Empty;

            string urlString = url.AbsoluteAction("ShowNotApplicable", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchViewNotApplicable{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='View Issue'><i data-feather='flag' class='text-danger'></i></a>";
        }

        private static string AddViewNotesIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (history.ParentHistory != null || // TODO: Figure out why the history can't have a parent to view notes
                IsNullOrWhiteSpace(history.Notes)) 
                return Empty;

            string urlString = url.AbsoluteAction("ShowNotes", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchShowNotes{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='View Notes'><i data-feather='info' class='infoButton'></i></a>";
        }

        private static string AddAssignUserIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.Assignable(roles))
                return Empty;

            if (!history.IsActive || userIsReadOnly)
                return "<a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='user'></i></a>";

            string urlString = url.AbsoluteAction("AssignUser", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchAssignedUser{row}' onclick=\"return modalButton(this, 'mediumSmall')\" title='Change Assigned User'><i data-feather='user'></i></a>";
        }

        private static string AddEmailIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (history.Event.ID != StatLogConfiguration.EventIDForEmail) 
                return Empty;

            // We might need to take in multiple comma delimited emailIDs in the future.
            bool validEDSEmailID = int.TryParse(history.EDSEmailIDS, out int _);
            if (!validEDSEmailID)
                return "<a title='Email Missing' style='cursor: not-allowed;opacity: 0.5;'><i data-feather='info' class='infoButton'></i></a>";

            string urlString = url.AbsoluteAction("ViewEmail", "Communication", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchViewEmail{row}' onclick=\"return modalButton(this, '9080')\" title='View Email'><i data-feather='info' class='infoButton'></i></a>";
        }

        private static string AddHideEventIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.Hideable(roles)) 
                return Empty;

            if (userIsReadOnly) 
                return $"<a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='eye-off'></i></a>";
            
            string urlString = url.AbsoluteAction("ShowHideHistory", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG), hide = true });
            return $"<a title='Hide and disable interaction with an event for providers and consultants.' href={urlString} id='anchShowHide{row}' onclick=\"return modalButton(this, 'mediumSmall')\"><i data-feather='eye-off'></i></a>";
        }

        private static string AddShowEventIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.Showable(roles)) 
                return Empty;

            if (userIsReadOnly)
                return $"<a><i data-feather='eye' style='cursor: not-allowed;opacity: 0.5;'></i></a>";

            string urlString = url.AbsoluteAction("ShowHideHistory", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG), hide = false });
            return $"<a title='Show and allow interaction with an event for providers and consultants.' href={urlString} id='anchShowHide{row}' onclick=\"return modalButton(this, 'mediumSmall')\"><i data-feather='eye' class='showEventButton'></i></a>";
        }

        private static string AddAddMetadataIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            // Metadata can only be added if there is a metadata schema on the event and no metadata data on the history
            if(!history.Event.XMLSchemaIDList.Any() || history.XMLDataList.Any() || !history.HasPermissionToAddMetadata(roles))
                return Empty;
            
            string urlString = url.AbsoluteAction("ShowAddMetadata", "Metadata", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchShowHide{row}' onclick=\"return modalButton(this, '90auto')\"><i data-feather='file-text'></i></a>";
        }

        private static string AddViewMetadataIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.XMLDataList.Any() || !history.HasPermissionToViewMetadata(roles)) 
                return Empty;

            string urlString = url.AbsoluteAction("ShowMetadata", "Metadata", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchShowHide{row}' onclick=\"return modalButton(this, '90auto')\"><i data-feather='file-text'></i></a>";
        }

        /// <summary>
        /// Adds the new N/A (not applicable) icon
        /// </summary>
        /// <param name="url"></param>
        /// <param name="history"></param>
        /// <param name="roles"></param>
        /// <param name="row"></param>
        /// <param name="userIsReadOnly"></param>
        /// <returns></returns>
        private static string AddShowNotApplicableInfoIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.Event.CanUseNA)
            {
                return Empty;
            }

            if (!history.IsActive || userIsReadOnly)
                return "<a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='file-minus'></i></a>";

            string urlString = url.AbsoluteAction("ShowNA", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchShowNAHistory{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='Mark N/A'><i data-feather='file-minus'></i></a>";
        }

        private static string AddPHIUploadIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, bool userIsReadOnly)
        {
            if (!history.Uploadable(roles))
            {
                return Empty;
            }
            
            if (!history.Event.IsPHI)
            {
                return Empty;
            }

            if (!history.IsActive || userIsReadOnly)
            {
                return "<a style='cursor: not-allowed;opacity: 0.5;'><i class='text-warning' data-feather='upload-cloud'></i></a><a style='cursor: not-allowed;opacity: 0.5;'><i data-feather='flag'></i></a>";
            }

            string uploadPhiButtonUrl = url.AbsoluteAction("Upload", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            var uploadPhiButton = $"<a href={uploadPhiButtonUrl} id='anchUploadPHI{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='Upload PHI'><i class='text-warning' data-feather='upload-cloud'></i></a>";

            string naButtonURL = url.AbsoluteAction("ShowNotApplicable", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            var naButton = $"<a href={naButtonURL} id='anchNotApplicable{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='Report Issue'><i data-feather='flag'></i></a>";

            return uploadPhiButton + naButton;

        }


        private static string AddPHIDownloadIcon(UrlHelper url, HistoryDO history, IARUMRole[] roles, int row, string sProviderID, bool userIsReadOnly)
        {
            //If item is not system event, do not show download icon
            if (!(history.Event.IsPHI || (history.ParentHistory?.IsPHI ?? false)) && !history.Event.IsSystemEvent)
                return Empty;

            //If item does not have a file associated with it, or user does not have download rights for the event, do not show download icon
            if (!(history.FileID != null && history.FileID != Guid.Empty && history.HasPermissionToDownload(roles)))
                return Empty;
            
            //If item is inactive or user is read-only, do not show download icon
            if (!history.IsActive || userIsReadOnly)
                return Empty;

            //If file is not found in ARUM, show greyed-out download icon
            if (!history.FileFoundInARUM)
                return "<a title='File Inactive' onclick='return false' style='cursor: not-allowed;opacity: 0.5;'><i data-feather='download-cloud'></i></a>";

            var arumUser = RetrieveSession.ARUMUser;
            int providerID = ToInternalID(sProviderID, PROVIDER_ID_STATLOG);
            
            var isProviderUserPhiSafe = ProviderUserHelper.IsProviderUserPhiSafe(providerID, arumUser);

            //If user does not have access to PHI, show no access to PHI icon
            if (!isProviderUserPhiSafe)
            {
                return "<a style='cursor: not-allowed;'><i class='text-warning' data-feather='cloud-off'></i></a>";
            }

            //If all cases pass, show PHI download icon
            string urlString = url.AbsoluteAction("PHIWarning", "StatLog", new { sHistoryID = ToClientID(history.ID, HISTORY_ID_STATLOG) });
            return $"<a href={urlString} id='anchDownloadPHI{row}' onclick=\"return modalButton(this, 'tallerWidest')\" title='Download PHI'><i class='text-warning' data-feather='download-cloud'></i></a>";
        }

       
    }
}