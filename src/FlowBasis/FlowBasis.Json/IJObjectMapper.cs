using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{
    public interface IJObjectMapper
    {
        object ToJObject(object instance, IJObjectRootMapper rootMapper);
        object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper);
    }
}
