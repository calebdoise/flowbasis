using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    public class NumberAsStringMapper : IJObjectMapper
    {
        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            if (jObject == null)
            {
                return null;
            }

            try
            {
                Type underlyingTargetType = targetType;

                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    underlyingTargetType = targetType.GetGenericArguments()[0];
                }

                if (underlyingTargetType == typeof(decimal))
                {
                    decimal decimalValue = Convert.ToDecimal(jObject);
                    return decimalValue;
                }
                else if (underlyingTargetType == typeof(float))
                {
                    float floatValue = Convert.ToSingle(jObject);
                    return floatValue;
                }
                else if (underlyingTargetType == typeof(double))
                {
                    double doubleValue = Convert.ToDouble(jObject);
                    return doubleValue;
                }
                else if (underlyingTargetType == typeof(int))
                {
                    int intValue = Convert.ToInt32(jObject);
                    return intValue;
                }
                else if (underlyingTargetType == typeof(long))
                {
                    long longValue = Convert.ToInt32(jObject);
                    return longValue;
                }
                else if (underlyingTargetType == typeof(short))
                {
                    short shortValue = Convert.ToInt16(jObject);
                    return shortValue;
                }
                else if (underlyingTargetType == typeof(byte))
                {
                    byte byteValue = Convert.ToByte(jObject);
                    return byteValue;
                }

                return null;
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
                return instance.ToString();
            }
        }
    }
}
