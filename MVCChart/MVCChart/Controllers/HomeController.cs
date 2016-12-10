using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;
using System.Web.Mvc;

namespace MVCChart.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Chat()
        {
            return View();
        }

        public ActionResult Doodle()
        {
            string a = ((LeaveType)1).ToString();
            return View();
        }

        public ActionResult Pics()
        {
            return View();
        }
    }

    public enum LeaveType
    {
        事假 = 0,
        病假 = 1,
        婚嫁 = 2
    }
}