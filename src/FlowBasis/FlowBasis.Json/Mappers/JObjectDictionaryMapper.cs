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
        private static Type s_typeOfGenericIDictionary = typeof(IDictionary<,>);
        private static Type s_typeOfGenericDictionary = typeof(Dictionary<,>);

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
            if (jObject == null)
            {
                return null;
            }

            bool isGenericType = targetType.IsGenericType;
            Type genericType = null;
            if (isGenericType)
            {
                genericType = targetType.GetGenericTypeDefinition();
            }

            Type keyType;
            Type valueType;
            if (isGenericType && genericType == s_typeOfGenericDictionary || genericType == s_typeOfGenericIDictionary)
            {                
                keyType = targetType.GetGenericArguments()[0];
                valueType = targetType.GetGenericArguments()[1];                

                Type dictionaryType = s_typeOfGenericDictionary.MakeGenericType(keyType, valueType);

                IDictionary dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);

                if (jObject is IDictionary<string, object> jObjectAsDictionary)
                {
                    foreach (var pair in jObjectAsDictionary)
                    {
                        object processedValue = rootMapper.FromJObject(pair.Value, valueType);
                        dictionary[pair.Key] = processedValue;
                    }
                }

                return dictionary;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
