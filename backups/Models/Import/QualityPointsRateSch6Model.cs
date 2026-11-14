using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace WebPortal_OH_FI.Models.Import
{
    public class QualityPointsRateSch6Model
    {
        public string EffectiveDate { get; set; }
        public string ProviderNumber { get; set; }
        public string PressureUlcers { get; set; }

        public string UrinaryTractInfection { get; set; }

        public string IndependentMovement { get; set; }

        public string Catheter { get; set; }

        public string Occupancy { get; set; }

        public string LSDI { get; set; }

        public string LSFI { get; set; }

        public string LSAM { get; set; }

        public string AdjustedStaffing { get; set; }

    }

    public class QualityPointsRateSch5Model
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string QTY_POINT_1 { get; set; }

        public string QTY_POINT_2 { get; set; }

        public string QTY_POINT_3 { get; set; }

        public string QTY_POINT_4 { get; set; }
        public string QTY_POINT_5 { get; set; }
        public string QTY_POINT_6 { get; set; }
        public string QTY_POINT_7 { get; set; }

    }

    public class CorpOfficeSalaryCeilingTablesModel
    {
        public string CYE { get; set; }
        public string General_Classification { get; set; }
        public string Classification_Title { get; set; }
        public string Classification_Number { get; set; }
        public string Bed_Size_Min { get; set; }
        public string POSITION_NUM { get; set; }
        public string Step_1 { get; set; }
        public string Step_2 { get; set; }
        public string Step_3 { get; set; }
        public string Step_4 { get; set; }
        public string Step_5 { get; set; }
        public string Step_6 { get; set; }
        public string Step_7 { get; set; }
        public string Step_8 { get; set; }
        public string Step_9 { get; set; }

    }

    public class QualityPointsModel
    {
        public string FYE { get; set; }
        public string ProviderNumber { get; set; }
        public string QualityPoints { get; set; }

    }
}