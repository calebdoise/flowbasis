using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{
    public interface IJObjectMapperProvider
    {
        IJObjectMapper ResovleJObjectMapperForInstance(object instance);
        IJObjectMapper ResovleJObjectMapperForJObject(object jObject, Type targetType);

        IJObjectMapper GetJObjectMapperByMapperType(Type mapperType);
    }
}
