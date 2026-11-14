using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models
{
    public class ImportProgressViewModel
    {
        public int DocumentCount = 0;
        public int DocumentTotal = 0;
        public int FileProgress = 0;
        public bool ProcessSetup = false;
        public bool Failure = false;
        public bool Complete = false;

        public int DocumentProgress
        {
            get
            {
                if (DocumentTotal == 0) return 0;
                return (int)(((decimal)DocumentCount + 1) / DocumentTotal * 100);
            }
        }
    }
}