using System.Collections.Generic;
using MSLC.Portal.Configuration;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class SettingsViewModel
    {
        public class View
        {
            public string[] ApplicationRoles { get; set; }
            public Dictionary<string, Dictionary<string, bool>> PermissionsList { get; set; }
            public List<FeatureViewModel> FeatureList { get; set; }
            public List<string> TabNames { get; set; }
        }

        public class Save
        {
            public Dictionary<string, Dictionary<string, bool>> PermissionsList { get; set; }
        }

        public class FeatureViewModel
        {
            private ClassificationDTO _classification;
            public string FeatureName { get; set; }
            public string Value => ARUMHandler.GetCategory(_classification, FeatureName);

            public string Description { get; set; }
            public string Tab { get; set; }
            public string Type { get; set; }

            public FeatureViewModel(ClassificationDTO classification, string featureName, string description, string tab, string type)
            {
                FeatureName = featureName;
                Description = description;
                Tab = tab;
                Type = type;
                _classification = classification;
            }
        }

    }
}