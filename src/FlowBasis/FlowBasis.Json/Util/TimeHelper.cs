using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Json.Util
{
    public static class TimeHelper
    {
        private static readonly Int64 StartTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

        public static DateTime FromEpochMilliseconds(long millisecondsSince1970)
        {           
            DateTime dateTime = new DateTime(1970, 1, 1) + new TimeSpan(millisecondsSince1970 * 10000);
            return dateTime;            
        }

        public static long ToEpochMilliseconds(DateTime dateTime)
        {            
            return (long)((dateTime.Ticks - StartTicks) / 10000);
        }
    }
}
