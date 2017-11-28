using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    public class JObjectEnumMapper : IJObjectMapper
    {
        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            if (instance == null)
            {
                return null;
            }

            return instance.ToString();
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            if (jObject == null)
            {
                return null;
            }

            Type enumType = null;
            if (targetType.IsEnum)
            {
                enumType = targetType;
            }
            else if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type valueType = targetType.GetGenericArguments()[0];
                if (valueType.IsEnum)
                {
                    enumType = valueType;
                }
            }

            if (enumType == null)
            {
                throw new Exception("targetType is not enum type: " + targetType.FullName);
            }

            try
            {
                object value = Enum.Parse(enumType, jObject.ToString());
                return value;
            }
            catch
            {
                // TODO: provide option for how to handle undefined value. 
                return null;
            }
        }
    }
}
