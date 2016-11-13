using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;

namespace MVCChart.Controllers
{
    [SessionState(SessionStateBehavior.ReadOnly)]
    public class PollingController : AsyncController
    {
        public ActionResult Polling()
        {
            return View();
        }

        public ActionResult AddNewMsg(string content)
        {
            PollingMannger.BeginSend(new PollingMannger.ClientData(PollingMannger.ClientData.MsgNewInformation,
                new
                {
                    content
                }), null, null);
            return Json(new { result = "发送完成!" });
        }

        [HttpPost, AsyncTimeout(1000 * 60 * 4)]
        public void PollingAsync(int? id, int v)
        {
            AsyncManager.OutstandingOperations.Increment();
            AsyncManager.Parameters["Version"] = v;
            PollingMannger.AddConnection(id, AsyncManager);
        }

        public ActionResult PollingCompleted()
        {
            try
            {
                (AsyncManager.Parameters["time"] as Timer).Dispose();
                AsyncManager.Parameters["Finish"] = 1;
                var v = AsyncManager.Parameters["Version"];
                var id = AsyncManager.Parameters["id"];
                if (!AsyncManager.Parameters.ContainsKey("Datas"))
                    return Json(new { result = "-1", v, id });
                var datas = AsyncManager.Parameters["Datas"] as List<PollingMannger.ClientData>;
                return Json(new { result = "-200", v, id, datas });
            }
            catch (Exception e)
            {
                return Json(new { result = "-500" });
            }
        }
    }
}