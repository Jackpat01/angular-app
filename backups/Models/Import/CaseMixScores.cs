using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models.Import
{

    /// <summary>
    /// Interface to generalize the MDS score value of Annual Case Mix scores for different provider types.
    /// </summary>
    public interface IAnnualCaseMixScore
    {
        string CYE { get; }
        string ProviderNumber { get; }
        string CaseMixValue { get; }
        string Score { get; }
        string Comments { get; }
    }

    /// <summary>
    /// Specific Annual Case Mix Score implementation for NF. This implements IAnnualCaseMixScore and 
    /// has a column named "MDS Score Type" for MDS values.
    ///// </summary>
    public class AnnualCaseMixScoreNF: IAnnualCaseMixScore
    {
        public string CYE { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixValue { get; set; }
        public string MDSScoreType { get; set; }
        public string Comments { get; set; }
        public virtual string Score
        {
            get { return MDSScoreType; }
        }

    }

    /// <summary>
    /// Specific Annual Case Mix Score implementation for ICF. This implements IAnnualCaseMixScore and 
    /// has a column named "Score Type" for MDS values.
    /// </summary>
    public class AnnualCaseMixScoreICF  : IAnnualCaseMixScore
    {
        public string CYE { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixValue { get; set; }
        public string ScoreType { get; set; }
        public string Comments { get; set; }
        public virtual string Score
        {
            get { return ScoreType; }
        }
    }

    /// <summary>
    /// Specific Semiannual Case Mix Score implementation for NF. This has a column named "MDS Score Type" for MDS values.
    /// </summary>
    public class SemiAnnualCaseMixScoreImport
    {
        public string EffectiveDate { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixValue { get; set; }
        public string MDSScoreType { get; set; }
        public string Comments { get; set; }
    }

    /// <summary>
    /// Specific Quaterly Case Mix Score implementation for NF. This has a column of QuarterlyEndDate instead of EffectiveDate.
    /// </summary>
    public class QuarterlyCaseMixScoreImport
    {
        public string QuarterEndDate { get; set; }
        public string ProviderNumber { get; set; }
        public string CaseMixValue { get; set; }
        public string MDSScoreType { get; set; }
        public string Comments { get; set; }
    }

    public class QuarterlyCaseMixScorePDPMImport
    {
        public string QuarterEndDate { get; set; }
        public string ProviderNumber { get; set; }
        public string TotalFacilityCaseMixValue { get; set; }
        public string CaseMixValue { get; set; }
        public string MDSScoreType { get; set; }
        public string Comments { get; set; }
    }

    public class QuarterlyCaseMixScoreSFYImport
    {
        public string SFY { get; set; }
        public string ProviderNumber { get; set; }
        public string EffectiveDate { get; set; }
        public string CaseMixValue { get; set; }
        public string ScoreType { get; set; }
        public string Comments { get; set; }
    }
}