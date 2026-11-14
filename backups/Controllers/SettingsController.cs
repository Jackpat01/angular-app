using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.StatLog.Controllers;
using WebPortal_OH_FI.BL.MappedObjects;
using WebPortal_OH_FI.Common.Extensions;
using WebPortal_OH_FI.Models.ViewModels;
using WebPortal_OH_FI.Web;

using static WebPortal_OH_FI.Models.ViewModels.SettingsViewModel;

namespace WebPortal_OH_FI.Controllers
{
    public class SettingsController : LoggedInBaseController
    {
        [HttpGet]
        [ClearProviderID]
        [ARUMFeatureActionFilterAttribute("OHFI:SettingsManagement")]
        public ActionResult Index()
        {
            ClassificationDTO classification = RetrieveSession.Classification;
            // This bit is not neccessary but makes sure we are getting the most up-to-date values.
            ARUMHandler.ClearCache(classification);

            var applicationServiceProxy = ARUMApplication.GetARUMApplicationProxy(classification);
            var response = applicationServiceProxy.GetApplicationRoles(classification.ARUMToken, false, "Settings/Index");
            if (!response.Result.Successful)
            {
                TempData["Message"] = response.Result.Message;
                return View();
            }
            var roleNames = response.lstApplicationRoles.Select(r => r.Role.Name).ToArray();
            var FeatureList = CreateFeatureList();
            var tabNames = FeatureList.Select(f => f.Tab).Distinct().ToList();
            var viewModel = new SettingsViewModel.View
            {
                ApplicationRoles = roleNames,
                PermissionsList = CreatePermissionsDataTable(roleNames),
                FeatureList = FeatureList,
                TabNames = tabNames
            };
            return View(viewModel);
        }

        private List<FeatureViewModel> CreateFeatureList()
        {
            return new List<FeatureViewModel>
            {
                new FeatureViewModel(RetrieveSession.Classification, "CostReportFileDownloadPath", "The path where the cost report files will be downloaded.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:PublicKeyFileForEncrypt", "The file containing the public key for encrypting provider data.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "CostReportFileName", "The name of the cost report file.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "StatlogImportRunDate", "The date when the statlog import will be run.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "Statlog_Generation_Success_Email_To", "The email address where success messages will be sent.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "Statlog_Generation_Errors_Email_To", "The email address where error messages will be sent.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "Statlog_Generation_System_Errors_Email_To", "This feature specifies the email address to send system error messages to.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "Statlog_Generation_AuditTypeKey", "This feature specifies the audit type key for statlog generation.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "Statlog_Generation_ProviderType", "This feature specifies the provider type for statlog generation.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "Statlog_Generation_IsInitialLoad", "This feature specifies whether the statlog generation process is running for the first time.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ApplicationName", "This feature specifies the name of the application.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "EmailFrom", "This feature specifies the email address to use as the sender for outgoing emails.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "EmailFromName", "This feature specifies the name to use as the sender for outgoing emails.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "HelpDeskContactEmail", "This feature specifies the email address for the help desk contact.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "HelpDeskContactPhone", "This feature specifies the phone number for the help desk contact.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "HelpDeskMessage", "This feature specifies the message to show on the help desk contact page.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "HelpDeskPhoneNumber", "This feature specifies the phone number to show on the help desk contact page.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "HasProviders", "This feature specifies whether the application has providers.", "General", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPEncryptionLocalFilePath", "This feature specifies the local file path for FTP encryption.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPEncryptedFileName", "This feature specifies the name of the encrypted FTP file.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPUnEncryptedFile", "This feature specifies the name of the unencrypted FTP file.", "ProviderImport", "ReadOnly"),
                // new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPUserName", "This feature specifies the FTP user name.", "ProviderImport", "ReadOnly"),
                // new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPPassword", "This feature specifies the FTP password.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPRemoteFile", "This feature specifies the remote file to import from an FTP server.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPLocalFile", "This feature specifies the local file to import from an FTP server.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPLocalFilePath", "This feature specifies the local file path to import from an FTP server.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPServer", "This feature specifies the FTP server to import from.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPPort", "This feature specifies the port to use for FTP connections.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:KeyLocation", "This feature specifies the location of the encryption keys.", "ProviderImport", "ReadOnly"),
                // new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:PrivateKeyFile", "This feature specifies the name of the private key file.", "ProviderImport", "ReadOnly"),
                // new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:PublicKeyFile", "This feature specifies the name of the public key file.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:PrivateFileForDecrypt", "This feature specifies the name of the file to use for decryption.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:RunDecryptProcess", "This feature specifies whether to run the decryption process.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:ImportRunDirectory", "This feature specifies the directory to use for importing files.", "ProviderImport", "ReadOnly"),
                new FeatureViewModel(RetrieveSession.Classification, "ProviderImport:FTPUploadFilePath", "This feature specifies the file path to use for FTP uploads.", "ProviderImport", "ReadOnly"),
            };
        }


        [HttpPost]
        [ARUMFeatureActionFilterAttribute("OHFI:SettingsManagement")]
        public ActionResult Save(SettingsViewModel.Save viewModel)
        {
            #region Save Feature Roles

            var featureNames = GetFeatureNames().ToList();

            if (!OHFISession.IsICF)
                featureNames.RemoveAll(x => ICFExclusivePermissions.Contains(x));

            Dictionary<string, string> features = new Dictionary<string, string>();

            foreach (var featureRoleList in viewModel.PermissionsList)
            {
                string featureName = featureRoleList.Key;
                if (!featureNames.Contains(featureName))
                    continue;

                var featureValue = string.Join(",", featureRoleList.Value.Where(r => r.Value).Select(r => $"^{r.Key}$").ToArray());
                if (string.IsNullOrWhiteSpace(featureValue))
                    featureValue = "NA";

                features.Add($"FeatureRoles:{featureName}", featureValue);
            }
            ARUMHandler.SetConfigurationList(RetrieveSession.Classification, features);
            #endregion


            return RedirectToAction("Index");
        }

        private static Dictionary<string, Dictionary<string, bool>> CreatePermissionsDataTable(string[] roleNames)
        {
            ClassificationDTO classification = RetrieveSession.Classification;
            var featureNames = GetFeatureNames().ToList();

            if (!OHFISession.IsICF)
                featureNames.RemoveAll(x => ICFExclusivePermissions.Contains(x));

            var featureRoleAccess = new Dictionary<string, Dictionary<string, bool>>();

            foreach (var featureName in featureNames)
            {
                var roleAccess = new Dictionary<string, bool>();

                foreach (string role in roleNames)
                {
                    bool featureMatchesRole = ARUMConfiguration.FeatureMatchesRole(classification, featureName, new[] { role });
                    roleAccess.Add(role, featureMatchesRole);
                }

                featureRoleAccess.Add(featureName, roleAccess);
            }

            return featureRoleAccess;
        }

        private static string[] ICFExclusivePermissions = new string[]
        {
            RateSettingPermissions.ProvRatesCreateCHOP_SPEC,
            RateSettingPermissions.ProvRatesCalculateCHOP_SPEC,
            RateSettingPermissions.ProvRatesSelectCHOP_SPEC,
            RateSettingPermissions.ProvRatesDeleteCHOP_SPEC
        };

        private static string[] GetFeatureNames()
        {
            return (new string[]
            {
                "OHFI:SettingsManagement",
                "BulkProcessing:View",
                "BulkProcessing:Upload",
                "Portal:Upload",
                "Portal:Download",
                "Reports:ARAP",
                "Reports:UploadDownload",
                "Reports:MassUpload",
                "Reports:MDS",
                "Reports:MDSResidentListing",
                "Reports:SectionQ",
                "Reports:CostReportSubmissionMonitoring",
                "Reports:CostReportDataWizard",
                "LetterAutomation:Access",
                "Statlog:View",
                "Statlog:Interact",
                "Statlog:Configuration",
                "DueDateUpdate:Access",
                "CLPA:View"
            }).Concat(GetPermissionsFromClass(typeof(CostReportPermissions))).ToArray()
            .Concat(GetPermissionsFromClass(typeof(RateSettingPermissions))).ToArray();
        }

        /// <summary>
        /// Grabs all constant strings from a class and converts the values into a string[]
        /// </summary>
        /// <param name="type">Type of class containing feature names as its constant strings</param>
        /// <returns></returns>
        private static string[] GetPermissionsFromClass(Type type)
        {
            return type
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(x => x.FieldType == typeof(string))
                .Select(x => x.GetRawConstantValue().ToString())
                .ToArray();
        }
    }
}