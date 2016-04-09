using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    public class EnumAsIntegerMapper : IJObjectMapper
    {
        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            if (jObject == null)
            {
                return null;
            }

            try
            {
                Type enumType = targetType;

                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    enumType = targetType.GetGenericArguments()[0];                   
                }

                object enumValue = Enum.ToObject(enumType, Convert.ToInt64(jObject));
                return enumValue;
            }
            catch
            {
                return null;
            }
        }

        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            if (instance == null)
            {
                return null;
            }
            else
            {
                return Convert.ToInt64(instance);
            }
        }
    }
}
