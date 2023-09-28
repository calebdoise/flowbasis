using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using FlowBasis.Json.Mappers;
using System.Collections.Concurrent;

namespace FlowBasis.Json
{
    public class DefaultJObjectMapperProvider : IJObjectMapperProvider
    {
        private static IdentityMapper s_identityMapper = new IdentityMapper();

        private static readonly Type s_typeOfObject = typeof(object);
        private static readonly Type s_typeOfGenericIList = typeof(IList<>);
        private static readonly Type s_typeOfGenericIDictionary = typeof(IDictionary<,>);
        private static readonly Type s_typeOfGenericDictionary = typeof(Dictionary<,>);
        private static readonly Type s_typeOfGenericIEnumerable = typeof(IEnumerable<>);
        private static readonly Type s_typeOfGenericNullable = typeof(Nullable<>);
        private static readonly Type s_typeOfString = typeof(string);
        private static readonly Type s_typeOfDecimal = typeof(decimal);
        private static readonly Type s_typeOfDateTime = typeof(DateTime);

        private readonly DefaultJObjectMapperProviderOptions options;
        private readonly DefaultClassMappingOptions defaultClassMappingOptions;

        private readonly ConcurrentDictionary<Type, IJObjectMapper> typeToJObjectMapperMap;
        private readonly ConcurrentDictionary<TargetTypeAndJObjectType, IJObjectMapper> typeAndJObjectTypeToJObjectMapperMap;

        private readonly JObjectPrimitiveMapper primitiveMapper = new JObjectPrimitiveMapper();
        private readonly IJObjectMapper stringMapper;
        private readonly JObjectArrayMapper arrayMapper = new JObjectArrayMapper();
        private readonly JObjectListMapper listMapper = new JObjectListMapper();
        private readonly JObjectDictionaryMapper dictionaryMapper = new JObjectDictionaryMapper();
        private readonly JObjectEnumMapper enumMapper = new JObjectEnumMapper();

        private readonly Dictionary<Type, IJObjectMapper> typeToCustomMapperMap = new Dictionary<Type, IJObjectMapper>();

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

            this.typeToJObjectMapperMap = new ConcurrentDictionary<Type, IJObjectMapper>();
            this.typeAndJObjectTypeToJObjectMapperMap = new ConcurrentDictionary<TargetTypeAndJObjectType, IJObjectMapper>();
        }


        public void RegisterJObjectMapperForType(Type type, IJObjectMapper mapper)
        {
            this.typeToCustomMapperMap[type] = mapper;

            // We clear the cached types since the new custom mapping renders the cache stale.
            this.typeToJObjectMapperMap.Clear();
            this.typeAndJObjectTypeToJObjectMapperMap.Clear();
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
            if (this.typeToJObjectMapperMap.TryGetValue(type, out var jObjectMapper))
            {
                return jObjectMapper;
            }

            jObjectMapper = this.ResovleJObjectMapperForInstanceTypeInner(type);
            this.typeToJObjectMapperMap.AddOrUpdate(type, jObjectMapper, (_, mapper) => mapper);
            return jObjectMapper;
        }

        private IJObjectMapper ResovleJObjectMapperForInstanceTypeInner(Type type)
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
            {
                throw new ArgumentNullException("jObject");
            }

            Type jObjectType = jObject.GetType();

            var key = new TargetTypeAndJObjectType(targetType, jObjectType);

            if (this.typeAndJObjectTypeToJObjectMapperMap.TryGetValue(key, out var jObjectMapper))
            {
                return jObjectMapper;
            }

            jObjectMapper = this.ResovleJObjectMapperForJObjectInner(jObject, jObjectType, targetType);
            this.typeAndJObjectTypeToJObjectMapperMap.AddOrUpdate(key, jObjectMapper, (type, mapper) => mapper);
            return jObjectMapper;
        }

        private IJObjectMapper ResovleJObjectMapperForJObjectInner(object jObject, Type jObjectType, Type targetType)
        {
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
            {
                genericTypeDefinition = targetType.GetGenericTypeDefinition();
            }
            else
            {
                genericTypeDefinition = null;
            }

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

        /// <summary>
        /// NET45 does not have tuples. This class exists to be used as a dictionary key.
        /// </summary>
        private class TargetTypeAndJObjectType
        {
            public TargetTypeAndJObjectType(Type targetType, Type jObjectType)
            {
                this.TargetType = targetType;
                this.JObjectType = jObjectType;
            }

            public Type TargetType { get; }

            public Type JObjectType { get; }

            public override bool Equals(object obj)
            {
                return obj is TargetTypeAndJObjectType type &&
                       EqualityComparer<Type>.Default.Equals(this.TargetType, type.TargetType) &&
                       EqualityComparer<Type>.Default.Equals(this.JObjectType, type.JObjectType);
            }

            public override int GetHashCode()
            {
                int hashCode = 908088354;
                hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(this.TargetType);
                hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(this.JObjectType);
                return hashCode;
            }
        }
    }


    public class DefaultJObjectMapperProviderOptions
    {
        public DefaultClassMappingOptions DefaultClassMappingOptions { get; set; }
    }
}
