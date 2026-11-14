using System.Collections.Generic;
using System.Linq;
using MSLC.Portal.Configuration;
using WebPortal_OH_FI.Common;
using WebPortal_OH_FI.Common.Models;

namespace WebPortal_OH_FI.Models
{
    public class ProjectsViewModel
    {
        public class Projects
        {
            public List<ClassificationDTO> Classifications { get; set; }

            public Projects()
            {
                Classifications = PortalConfiguration.GetAllClassifications().ToList();

                var arumValidations = Functions.GetValidations();

                foreach (var singClassification in Classifications.ToList())
                {
                    if (arumValidations != null && arumValidations.lstARUMUsers != null && arumValidations.lstARUMUsers.Any())
                    {
                        if (arumValidations.lstARUMUsers.All(c => c.Token != singClassification.ARUMToken))
                        {
                            Classifications.Remove(singClassification);
                        }
                    }
                }
            }           
        }
        public static List<ClassificationModel> CloneClassifications(List<ClassificationDTO> classifications)
        {
            List<ClassificationModel> list = new List<ClassificationModel>();
            foreach (ClassificationDTO classification in classifications)
            {
                list.Add(new ClassificationModel
                {
                    ARUMToken = classification.ARUMToken,
                    Database = classification.Database,
                    DisplayName = classification.DisplayName,
                    EDSToken = classification.EDSToken,
                    ID = classification.ID,
                    Server = classification.Server,
                    StateAbbreviation = classification.StateAbbreviation,
                    StateName = classification.StateName,
                    Type = classification.Type,
                    URL = classification.URL
                });
            }

            return list;
        }
    }
}