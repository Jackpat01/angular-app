using MSLC.ArumProxy;
using MSLC.Portal.Configuration;
using MSLC.Portal.MVC.ActionFilters;
using MSLC.Portal.MVC.Utility;
using MSLC.Portal.Utility.Proxies;
using MSLC.StatLog.Controllers;
using Serilog;
using System;
using System.Linq;
using System.Web.Mvc;
using WebPortal_OH_FI.Models;

namespace WebPortal_OH_FI.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class BulletinBoardController : LoggedInBaseController
    {
        #region Properties

        private ArumServiceProxy _arumUserProxy;

        #endregion

        public BulletinBoardController()
        {
            _arumUserProxy = ARUMUser.GetArumServiceProxy(RetrieveSession.Classification);
        }

        // GET: BulletinBoard
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            try
            {
                var myViewModel = new BulletinBoardViewModel();
                var myUser = RetrieveSession.ARUMUser;
                if (PortalConfiguration.EnableBB)
                {
                    if (myUser != null)
                    {
                        if (myUser.BulletinBoards != null)
                        {
                            myViewModel.myBulletinBoards = myUser.BulletinBoards.ToList();
                            myViewModel.BulletinBoardTypes = myUser.BulletinBoards.Select(c => c.Type).Distinct().ToList();
                            return View(myViewModel);
                        }
                        else
                        {
                            myViewModel.myBulletinBoards = null;
                            myViewModel.BulletinBoardTypes = null;
                            return View(myViewModel);
                        }
                    }
                    else
                    {
                        return RedirectToAction("Login", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "Secure");
                }
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
            }
            return RedirectToAction("Login", "Home");
        }

        /// <summary>
        /// Shows all.
        /// </summary>
        /// <returns></returns>
        [ClearProviderID]
        public ActionResult ShowAll()
        {
            try
            {
                if (PortalConfiguration.EnableBB)
                {
                    var myViewModel = new BulletinBoardViewModel();
                    var myUser = RetrieveSession.ARUMUser;
                    if (myUser != null)
                    {
                        if (myUser.BulletinBoards != null)
                        {
                            myViewModel.myBulletinBoards = myUser.BulletinBoards.ToList();
                            myViewModel.BulletinBoardTypes = myUser.BulletinBoards.Select(c => c.Type).Distinct().ToList();
                            return View(myViewModel);
                        }
                        else
                        {
                            myViewModel.myBulletinBoards = null;
                            myViewModel.BulletinBoardTypes = null;
                            return View(myViewModel);
                        }
                    }
                    else
                    {
                        return RedirectToAction("Login", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "Secure");
                }
            }
            catch (Exception ex)
            {
                var exMessage = ex.InnerException != null ? $"{ex.Message} Inner Exception: {ex.InnerException.Message}" : ex.Message;
                Log.Error(ex, exMessage);
            }
            return RedirectToAction("Login", "Home");
        }
    }
}