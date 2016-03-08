using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeocodingConsole
{
    public static class Extensions
    {

        public static string To6DecimalsPrecisionString(this double val)
        {
            return val.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string ToJsonString(this object val)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(val);
        }

    }
}
