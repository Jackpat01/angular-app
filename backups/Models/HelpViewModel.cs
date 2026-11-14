using MSLC.FileProxy.Interfaces;
using System;

namespace WebPortal_OH_FI.Models
{
    public class HelpViewModel
    {
        public class View
        {
            public DateTime? UsersLastLoginDate { get; set; }
            public FileInformationViewModel[] Files { get; set; }

            public View()
            {

            }
        }
    }
}