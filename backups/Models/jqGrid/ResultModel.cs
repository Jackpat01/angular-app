using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebPortal_OH_FI.Models.jqGrid
{
    public class ResultModel
    {
        public int page;
        public int total;
        public int records;
        public Models.jqGrid.RowModel[] rows;
        public string GridCellDefArray;
    }
}