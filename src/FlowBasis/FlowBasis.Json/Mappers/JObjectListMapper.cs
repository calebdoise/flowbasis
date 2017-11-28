using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Mappers
{
    public class JObjectListMapper : IJObjectMapper
    {
        private static Type s_typeOfGenericIList = typeof(IList<>);
        private static Type s_typeOfGenericList = typeof(List<>);
        private static Type s_typeOfGenericIEnumerable = typeof(IEnumerable<>);

        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            IList list = (IList)instance;

            ArrayList result = new ArrayList(list.Count);
            foreach (var entry in list)
            {
                object processedEntry = rootMapper.ToJObject(entry);
                result.Add(processedEntry);
            }

            return result;
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            IList sourceList = jObject as IList;
            if (sourceList != null)
            {
                bool useGenericList;
                Type targetTypeToUse;

                Type elementType;
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == s_typeOfGenericList)
                {
                    useGenericList = true;
                    elementType = targetType.GetGenericArguments()[0];
                    targetTypeToUse = targetType;
                }
                else if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == s_typeOfGenericIList)
                {
                    useGenericList = true;
                    elementType = targetType.GetGenericArguments()[0];
                    targetTypeToUse = s_typeOfGenericList.MakeGenericType(elementType);
                }
                else if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == s_typeOfGenericIEnumerable)
                {
                    useGenericList = true;
                    elementType = targetType.GetGenericArguments()[0];
                    targetTypeToUse = s_typeOfGenericList.MakeGenericType(elementType);
                }
                else
                {
                    useGenericList = false;
                    elementType = typeof(object);
                    targetTypeToUse = targetType;
                }

                IList result;
                if (useGenericList)
                {
                    result = (IList)Activator.CreateInstance(targetTypeToUse);
                }
                else
                {
                    result = new ArrayList();
                }

                foreach (var entry in sourceList)
                {
                    object processedEntry = rootMapper.FromJObject(entry, elementType);
                    result.Add(processedEntry);
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
