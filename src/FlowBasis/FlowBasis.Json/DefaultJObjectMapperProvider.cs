using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace FlowBasis.Json
{
    public class DefaultJObjectMapperProvider : IJObjectMapperProvider
    {
        private static IdentityMapper s_identityMapper = new IdentityMapper();

        private static Type s_typeOfGenericIList = typeof(IList<>);
        private static Type s_typeOfGenericList = typeof(List<>);
        private static Type s_typeOfGenericIEnumerable = typeof(IEnumerable<>);
        private static Type s_typeOfGenericNullable = typeof(Nullable<>);


        private DefaultJObjectMapperProviderOptions options;
        private DefaultClassMappingOptions defaultClassMappingOptions;

        private Dictionary<Type, IJObjectMapper> typeToCustomMapperMap = new Dictionary<Type, IJObjectMapper>();

        private JObjectPrimitiveMapper primitiveMapper = new JObjectPrimitiveMapper();
        private JObjectArrayMapper arrayMapper = new JObjectArrayMapper();
        private JObjectListMapper listMapper = new JObjectListMapper();
        private JObjectDictionaryMapper dictionaryMapper = new JObjectDictionaryMapper();
        private JObjectEnumMapper enumMapper = new JObjectEnumMapper();


        public DefaultJObjectMapperProvider() : this(null)
        {            
        }

        public DefaultJObjectMapperProvider(DefaultJObjectMapperProviderOptions options)
        {
            this.options = (options != null) ? options : new DefaultJObjectMapperProviderOptions();

            this.defaultClassMappingOptions = this.options.DefaultClassMappingOptions;
            if (this.defaultClassMappingOptions == null)
            {
                this.defaultClassMappingOptions = new DefaultClassMappingOptions();
            }
        }


        public void RegisterJObjectMapperForType(Type type, IJObjectMapper mapper)
        {
            this.typeToCustomMapperMap[type] = mapper;
        }


        public IJObjectMapper ResovleJObjectMapperForInstance(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            Type type = instance.GetType();
            return this.ResovleJObjectMapperForInstanceType(type);
        }

        private IJObjectMapper ResovleJObjectMapperForInstanceType(Type type)
        {
            IJObjectMapper registeredMapper;
            if (this.typeToCustomMapperMap.TryGetValue(type, out registeredMapper))
            {
                return registeredMapper;
            }

            bool typeIsGeneric = type.IsGenericType;
            Type genericTypeDefinition;
            if (typeIsGeneric)
                genericTypeDefinition = type.GetGenericTypeDefinition();
            else
                genericTypeDefinition = null;

            if (typeIsGeneric && genericTypeDefinition == typeof(Nullable<>))
            {
                Type valueType = type.GetGenericArguments()[0];
                return this.ResovleJObjectMapperForInstanceType(valueType);
            }
            else if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
            {
                return this.primitiveMapper;
            }
            else if (typeof(IDictionary).IsAssignableFrom(type) || typeof(IDictionary<string, object>).IsAssignableFrom(type))
            {
                return this.dictionaryMapper;
            }
            else if (type.IsArray)
            {
                return this.arrayMapper;
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                return this.listMapper;
            }
            else if (type.IsEnum)
            {
                return this.enumMapper;
            }
            else if (type.IsClass || type.IsValueType)
            {
                return this.GetClassMapper(type);
            }
            else
            {
                throw new NotSupportedException("Unsupported type: " + type.FullName);
            }
        }


        public IJObjectMapper ResovleJObjectMapperForJObject(object jObject, Type targetType)
        {
            if (jObject == null)
                throw new ArgumentNullException("jObject");

            Type jObjectType = jObject.GetType();

            IJObjectMapper registeredMapper;
            if (this.typeToCustomMapperMap.TryGetValue(targetType, out registeredMapper))
            {
                return registeredMapper;
            }

            if (targetType == typeof(object))
            {
                return s_identityMapper;
            }

            bool targetTypeIsGeneric = targetType.IsGenericType;
            Type genericTypeDefinition;
            if (targetTypeIsGeneric)
                genericTypeDefinition = targetType.GetGenericTypeDefinition();
            else
                genericTypeDefinition = null;

            if (targetTypeIsGeneric && genericTypeDefinition == s_typeOfGenericNullable)
            {
                Type valueType = targetType.GetGenericArguments()[0];
                return this.ResovleJObjectMapperForJObject(jObject, valueType);
            }
            else if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(decimal) || targetType == typeof(DateTime))
            {
                return this.primitiveMapper;
            }
            else if (targetType.IsArray)
            {
                return this.arrayMapper;
            }
            else if (typeof(IList).IsAssignableFrom(targetType))
            {
                return this.listMapper;
            }
            else if (targetType == typeof(object) && jObject is IList)
            {
                return this.listMapper;
            }
            else if (genericTypeDefinition != null && s_typeOfGenericIList == genericTypeDefinition)
            {
                return this.listMapper;
            }
            else if (genericTypeDefinition != null && s_typeOfGenericIEnumerable == genericTypeDefinition)
            {
                return this.listMapper;
            }
            else if (targetType == typeof(object) && (jObjectType.IsPrimitive || jObjectType == typeof(string) || jObjectType == typeof(decimal) || jObjectType == typeof(DateTime)))
            {
                return this.primitiveMapper;
            }
            else if (targetType.IsEnum)
            {
                return this.enumMapper;
            }
            else if ((targetType.IsClass || targetType.IsValueType) && !targetType.IsAbstract)
            {
                return this.GetClassMapper(targetType);
            }
            else
            {
                throw new NotSupportedException("Unsupported target type: " + targetType.FullName);
            }
        }

        protected virtual IJObjectMapper GetClassMapper(Type type)
        {
            var classMapper = JObjectStructuredClassMapper.GetDefaultClassMapping(type, this.defaultClassMappingOptions);
            return classMapper;
        }


        public virtual IJObjectMapper GetJObjectMapperByMapperType(Type mapperType)
        {
            object mapper = Activator.CreateInstance(mapperType);
            return (IJObjectMapper)mapper;
        }


        private class IdentityMapper : IJObjectMapper
        {
            public object ToJObject(object instance, IJObjectRootMapper rootMapper)
            {
                return instance;
            }

            public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
            {
                return jObject;
            }
        }
    }


    public class DefaultJObjectMapperProviderOptions
    { 
        public DefaultClassMappingOptions DefaultClassMappingOptions { get; set; }
    }


    public class JObjectPrimitiveMapper : IJObjectMapper
    {
        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            return instance;
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            return CoercePrimitive(jObject, targetType);
        }

        private object CoercePrimitive(object value, Type targetType)
        {
            Type elementType = targetType;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                elementType = targetType.GetGenericArguments()[0];

                if (value == null)
                {
                    return null;
                }
                else if (value is string && ((string)value == ""))
                {
                    return null;
                }
            }

            if (elementType == typeof(decimal))
            {
                return Convert.ToDecimal(value);
            }
            else if (elementType == typeof(float))
            {
                return Convert.ToSingle(value);
            }
            else if (elementType == typeof(double))
            {
                return Convert.ToDouble(value);
            }
            else if (elementType == typeof(Int32))
            {
                return Convert.ToInt32(value);
            }
            else if (elementType == typeof(Int64))
            {
                return Convert.ToInt64(value);
            }
            else if (elementType == typeof(byte))
            {
                return Convert.ToByte(value);
            }
            else if (elementType == typeof(bool))
            {
                return Convert.ToBoolean(value);
            }
            else if (elementType == typeof(string))
            {
                return value.ToString();
            }
            else
            {
                return value;
            }
        }
    }


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
