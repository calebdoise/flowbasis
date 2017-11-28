using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    public class JObjectPrimitiveMapper : IJObjectMapper
    {
        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            return instance;
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            return CoercePrimitive(jObject, targetType);
        }

        private object CoercePrimitive(object value, Type targetType)
        {
            Type elementType = targetType;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                elementType = targetType.GetGenericArguments()[0];

                if (value == null)
                {
                    return null;
                }
                else if (value is string && ((string)value == ""))
                {
                    return null;
                }
            }

            if (elementType == typeof(decimal))
            {
                return Convert.ToDecimal(value);
            }
            else if (elementType == typeof(float))
            {
                return Convert.ToSingle(value);
            }
            else if (elementType == typeof(double))
            {
                return Convert.ToDouble(value);
            }
            else if (elementType == typeof(Int32))
            {
                return Convert.ToInt32(value);
            }
            else if (elementType == typeof(Int64))
            {
                return Convert.ToInt64(value);
            }
            else if (elementType == typeof(byte))
            {
                return Convert.ToByte(value);
            }
            else if (elementType == typeof(bool))
            {
                return Convert.ToBoolean(value);
            }
            else if (elementType == typeof(string))
            {
                return value.ToString();
            }
            else
            {
                return value;
            }
        }
    }
}
