using MVCChart.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MVCChart
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            //Timer timer = new Timer(10000);
            //timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            //timer.Start(); 
        }

        public static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            new ChatHub().ChkLogout();
        }
    }
}
