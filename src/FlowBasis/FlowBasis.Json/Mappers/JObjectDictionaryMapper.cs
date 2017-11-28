using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    public class JObjectDictionaryMapper : IJObjectMapper
    {
        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            var result = new JObject();

            if (instance is Dictionary<string, object>)
            {
                foreach (var pair in (Dictionary<string, object>)instance)
                {
                    object processedEntry = rootMapper.ToJObject(pair.Value);
                    result[pair.Key] = processedEntry;
                }
            }
            else if (instance is System.Collections.IDictionary)
            {
                foreach (DictionaryEntry pair in (System.Collections.IDictionary)instance)
                {
                    object processedEntry = rootMapper.ToJObject(pair.Value);
                    result[pair.Key.ToString()] = processedEntry;
                }
            }
            else
            {
                throw new Exception("Unsupported type: " + instance.GetType());
            }

            return result;
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            throw new NotImplementedException();
        }
    }
}
