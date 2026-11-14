using System.Collections.Generic;
using MSLC.ArumProxy.Interfaces;

namespace WebPortal_OH_FI.Models
{
    public class BulletinBoardViewModel
    {
        public List<IARUMBulletinBoards> myBulletinBoards { get; set; }
        public List<string> BulletinBoardTypes { get; set; }
    }
}