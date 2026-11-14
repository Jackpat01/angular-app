using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models
{
    public class DatabaseLookupValueViewModel
    {
        public string DatabaseLookupVariableId { get; set; }

        public int? VersionVGS { get; set; }

        public string ValueType { get; set; }

        public string DisplayName { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string RangeMinimum { get; set; }

        public string RangeMaximum { get; set; }

        public int? Sequence { get; set; }
    }

    public class DatabaseLookupGroupDataViewModel
    {
        public string DatabaseLookupGroupDataId { get; set; }
        public string DatabaseLookupGroupId { get; set; }
        public string DatabaseLookupGroupYearId { get; set; }
        public string ValueType { get; set; }
        public string VariableName { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
    }

    public class DatabaseLookupGroupYearViewModel
    {
        public string DatabaseLookupGroupYearId { get; set; }
        public int Year { get; set; }
        public string ValueType { get; set; }
    }
}