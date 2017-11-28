using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    public class JObjectArrayMapper : IJObjectMapper
    {
        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            Array valueArray = (Array)instance;
            ArrayList list = new ArrayList(valueArray.Length);

            foreach (var entry in valueArray)
            {
                object entryJObject = rootMapper.ToJObject(entry);
                list.Add(entryJObject);
            }

            return list;
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            IList sourceList = jObject as IList;
            if (sourceList != null)
            {
                Type elementType = targetType.GetElementType();
                Array result = Array.CreateInstance(elementType, sourceList.Count);
                int entryCo = 0;
                foreach (var entry in sourceList)
                {
                    object processedEntry = rootMapper.FromJObject(entry, elementType);
                    if (processedEntry != null)
                    {
                        result.SetValue(processedEntry, entryCo);
                    }

                    entryCo++;
                }

                return result;
            }
            else
            {
                throw new ArgumentException("jObject does implement IList: " + jObject.GetType().FullName, "jObject");
            }
        }
    }
}
