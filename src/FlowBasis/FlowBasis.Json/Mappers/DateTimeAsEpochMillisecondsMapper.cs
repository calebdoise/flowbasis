using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    public class DateTimeAsEpochMillisecondsMapper : IJObjectMapper
    {
        private static readonly Int64 StartTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            if (jObject == null)
            {
                return null;
            }

            try
            {
                long millisecondsSince1970 = Convert.ToInt64(jObject);

                DateTime dateTime = new DateTime(1970, 1, 1) + new TimeSpan(millisecondsSince1970 * 10000);
                return dateTime;
            }
            catch
            {
                return null;
            }
        }

        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            DateTime? dateTime = instance as DateTime?;
            if (dateTime != null)
            {
                return (Int64)((dateTime.Value.Ticks - StartTicks) / 10000);
            }
            else
            {
                return null;
            }
        }
    }
}
