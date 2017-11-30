using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using FlowBasis.Json.Mappers;

namespace FlowBasis.Json
{
    public class DefaultJObjectMapperProvider : IJObjectMapperProvider
    {
        private static IdentityMapper s_identityMapper = new IdentityMapper();

        private static Type s_typeOfObject = typeof(object);
        private static Type s_typeOfGenericIList = typeof(IList<>);
        private static Type s_typeOfGenericList = typeof(List<>);
        private static Type s_typeOfGenericIDictionary = typeof(IDictionary<,>);
        private static Type s_typeOfGenericDictionary = typeof(Dictionary<,>);
        private static Type s_typeOfGenericIEnumerable = typeof(IEnumerable<>);
        private static Type s_typeOfGenericNullable = typeof(Nullable<>);
        private static Type s_typeOfString = typeof(string);
        private static Type s_typeOfDecimal = typeof(decimal);
        private static Type s_typeOfDateTime = typeof(DateTime);

        private DefaultJObjectMapperProviderOptions options;
        private DefaultClassMappingOptions defaultClassMappingOptions;

        private Dictionary<Type, IJObjectMapper> typeToCustomMapperMap = new Dictionary<Type, IJObjectMapper>();

        private JObjectPrimitiveMapper primitiveMapper = new JObjectPrimitiveMapper();
        private IJObjectMapper stringMapper;
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
            
            this.stringMapper = this.primitiveMapper;            
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
            else if (type == s_typeOfString)
            {
                return this.stringMapper;
            }
            else if (type.IsPrimitive || type == s_typeOfDecimal || type == s_typeOfDateTime)
            {
                return this.primitiveMapper;                
            }
            else if (typeof(IDictionary).IsAssignableFrom(type) 
                || typeof(IDictionary<string, object>).IsAssignableFrom(type)
                || typeof(IDictionary<string, string>).IsAssignableFrom(type))
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

            if (targetType == s_typeOfObject)
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
            else if (targetType == s_typeOfString)
            {
                return this.stringMapper;
            }
            else if (targetType.IsPrimitive || targetType == s_typeOfDecimal || targetType == s_typeOfDateTime)
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
            else if (genericTypeDefinition != null && (s_typeOfGenericDictionary == genericTypeDefinition || s_typeOfGenericIDictionary == genericTypeDefinition))
            {
                return this.dictionaryMapper;
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
}
