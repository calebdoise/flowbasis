using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{

    public interface IJObjectRootMapper
    {
        object ToJObject(object instance);
        object FromJObject(object jObject, Type targetType);

        IJObjectMapperProvider MapperProvider { get; }
    }

}
