using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    internal class StringTransformMapper : IJObjectMapper
    {
        private Func<string, string> stringInputTransform;

        public StringTransformMapper(Func<string, string> stringInputTransform)
        {
            this.stringInputTransform = stringInputTransform;
        }

        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            return instance;
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            string strValue = jObject?.ToString();

            if (this.stringInputTransform != null)
            {
                string transformedValue = this.stringInputTransform(strValue);
                return transformedValue;
            }
            else
            {
                return strValue;
            }
        }
    }
}
