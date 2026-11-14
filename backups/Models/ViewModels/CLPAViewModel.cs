using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models.ViewModels
{
    public class SearchAssessmentViewModel
    {
        public class View
        {
            /// <summary>
            /// Gets or sets the medicaid number.
            /// </summary>
            /// <value>
            /// The medicaid number.
            /// </value>
            public string SSN { get; set; }
            /// <summary>
            /// Gets or sets my search results.
            /// </summary>
            /// <value>
            /// My search results.
            /// </value>
            public List<SearchAssessmentResults> mySearchResults { get; set; }
        }

        public class SearchAssessmentResults
        {
            public string AssessmentID { get; set; }
            public DateTime? ReferenceDate { get; set; }
            public string ReferenceDateString { get; set; }
        }

        public class Search
        {
            public string SSN { get; set; }
        }
    }
}