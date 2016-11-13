using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace MVCChart.Common
{
    public static class Common
    {
        public static JavaScriptSerializer JsonConverter = new JavaScriptSerializer();
        public static string ConvertToJson(object o)
        {
            string jsonstring = JsonConverter.Serialize(o);
            return jsonstring;
        }
    }
}