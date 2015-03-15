using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{
    public interface IJsonSerializationService
    {
        string Stringify(object value);

        object Parse(string json);
        object Parse(string json, Type targetType);
        T Parse<T>(string json);
    }
    
}
