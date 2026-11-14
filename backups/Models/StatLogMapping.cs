// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using MSLC.Portal.MVC.Utility;
using MSLC.StatLog.BL.CostReportHistory;
using MSLC.StatLog.BL.Provider;
using MSLC.StatLog.BL.StatLogBL;
using MSLC.StatLog.Domain;
using MSLC.StatLog.DTO;
using static MSLC.StatLog.BL.MappedObjectsNamespace.MappedObjects;

namespace MSLC.StatLog.UI.Models
{
    public class StatLogMapping
    {
        public static Mapping MapByHistoryID(int historyID, string source, string action, string featureName, Mapping returnValue = null)
        {
            returnValue = returnValue ?? new Mapping { Error = false };

            returnValue.History = CostReportHistoryBL.GetHistoryByHistoryID(RetrieveSession.Classification, historyID, RetrieveSession.ARUMUser.UserID, source, action);
            if (returnValue.History != null)
                return MapByCostReportID(returnValue.History.CRID, featureName, returnValue);

            returnValue.Error = true;
            returnValue.ErrorMessage = "Error: StatLog History cannot be retrieved at this time, please contact support.";
            return returnValue;
        }

        public static Mapping MapByCostReportID(int costReportID, string featureName, Mapping returnValue = null)
        {
            returnValue = returnValue ?? new Mapping { Error = false };

            returnValue.StatLog = StatLogBL.GetStatLogByStatLogID(RetrieveSession.Classification, costReportID);
            if (returnValue.StatLog != null)
                return MapByProviderID(returnValue.StatLog.ARUMProviderID, featureName, returnValue);

            returnValue.Error = true;
            returnValue.ErrorMessage = "Error: StatLog cannot be retrieved at this time, please contact support.";
            return returnValue;
        }

        public static Mapping MapByProviderID(int? providerID, string featureName, Mapping returnValue = null)
        {
            returnValue = returnValue ?? new Mapping { Error = false };

            returnValue.Provider = ProviderBL.GetProviderInfoByARUMUserAndProviderID(RetrieveSession.Classification, RetrieveSession.ARUMUser, providerID, featureName);
            if (returnValue.Provider != null)
                return returnValue;

            returnValue.Error = true;
            returnValue.ErrorMessage = "Error: Provider cannot be retrieved at this time, please contact support.";
            return returnValue;
        }

        public class Mapping
        {
            public HistoryDO History { get; set; }
            public StatLogDTO StatLog { get; set; }
            public ProviderInfo Provider { get; set; }
            public string ErrorMessage { get; set; }
            public bool Error { get; set; }
        }
    }
}