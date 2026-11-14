using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace WebPortal_OH_FI.Models.Import
{
    public class TrialBalanceModel
    {
        public string Account { get; set; }
        public string EndofPeriod { get; set; }
    }

    public class SchA1ImportModel
    {
        public List<SchA1Model> schA1Models { get; set; }
    }

    public class SchA1Model
    {
        public string Value { get; set; }

        public int DisplayLine { get; set; }

        public int DisplayColumn { get; set; }
    }

    public class StringWriterUTF8 : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}