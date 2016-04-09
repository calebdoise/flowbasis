using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FlowBasis.Json
{    

    public class JObjectStructuredClassMapper : IJObjectMapper
    {
        public JObjectStructuredClassMapper()
        {
            this.PropertyMappings = new List<IJObjectPropertyMapper>();
        }

        public JObjectStructuredClassMapper(IList<IJObjectPropertyMapper> propertyMappings)
        {
            this.PropertyMappings = propertyMappings;
        }

        public IList<IJObjectPropertyMapper> PropertyMappings { get; private set; }

        public IJObjectPropertyMapper GetPropertyMappingByJObjectPropertyName(string jObjectPropertyName)
        {
            return this.PropertyMappings.FirstOrDefault(pm => pm.JObjectPropertyName == jObjectPropertyName);
        }


        public object ToJObject(object instance, IJObjectRootMapper rootMapper)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            foreach (var propertyMapping in this.PropertyMappings)
            {
                object jObjectPropertyValue = propertyMapping.GetJObjectValue(instance, rootMapper);
                if (jObjectPropertyValue != null)
                {
                    result[propertyMapping.JObjectPropertyName] = jObjectPropertyValue;
                }
            }

            return new JObject(result);
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            object instance = Activator.CreateInstance(targetType);

            Newtonsoft.Json.Linq.JObject newtonsoftJObject = jObject as Newtonsoft.Json.Linq.JObject;
            if (newtonsoftJObject != null)
            {
                foreach (var prop in newtonsoftJObject.Properties())
                {
                    string jObjectPropertyName = prop.Name;
                    Newtonsoft.Json.Linq.JToken jObjectTokenValue = prop.Value;

                    IJObjectPropertyMapper propertyMapping = this.GetPropertyMappingByJObjectPropertyName(jObjectPropertyName);
                    if (propertyMapping != null)
                    {
                        propertyMapping.SetInstanceValue(instance, jObjectTokenValue, rootMapper);
                    }
                }
            }
            else
            {
                IDictionary<string, object> source = (IDictionary<string, object>)jObject;

                foreach (var entry in source)
                {
                    string jObjectPropertyName = entry.Key;
                    object jObjectValue = entry.Value;

                    IJObjectPropertyMapper propertyMapping = this.GetPropertyMappingByJObjectPropertyName(jObjectPropertyName);
                    if (propertyMapping != null)
                    {
                        propertyMapping.SetInstanceValue(instance, jObjectValue, rootMapper);
                    }
                }
            }

            return instance;
        }


        public static IJObjectMapper GetDefaultClassMapping(Type type, DefaultClassMappingOptions options = null)
        {
            if ((type.IsClass || type.IsValueType) && !type.IsAbstract)
            {
                // See if there is an override for the mapper.
                var attrList = type.GetCustomAttributes(true);
                foreach (var attr in attrList)
                {
                    JsonTypeAttribute jsonTypeAttr = attr as JsonTypeAttribute;
                    if (jsonTypeAttr != null)
                    {
                        if (jsonTypeAttr.MapperType != null)
                        {
                            return (IJObjectMapper)Activator.CreateInstance(jsonTypeAttr.MapperType);
                        }
                    }                    
                }

                List<IJObjectPropertyMapper> propertyMappings = new List<IJObjectPropertyMapper>();

                var properties = type.GetProperties();
                foreach (var propertyInfo in properties)
                {
                    JsonPropertyAttribute jsonPropertyAttr = null;
                    bool jsonDateTimeAsEpochMillisecondsFound = false;

                    object[] propertyAttrList = propertyInfo.GetCustomAttributes(true);

                    bool ignore = false;
                    foreach (object attr in propertyAttrList)
                    {
                        if (attr is JsonIgnoreAttribute)
                        {
                            ignore = true;
                            break;
                        }
                        else if (attr is NonSerializedAttribute)
                        {
                            ignore = true;
                            break;
                        }
                        else if (attr is JsonPropertyAttribute)
                        {
                            jsonPropertyAttr = (JsonPropertyAttribute)attr;
                        }
                        else
                        {
                            // We allow some name-based attribute checks to influence formatting, so POCO class
                            // libraries do not have to have a dependency on FlowBasis assembly.
                            string attrTypeName = attr.GetType().Name;
                            switch (attrTypeName)
                            {
                                case "JsonIgnoreAttribute":
                                    ignore = true;
                                    break;
                                case "JsonDateTimeAsEpochMillisecondsAttribute":
                                    jsonDateTimeAsEpochMillisecondsFound = true;
                                    break;
                            }
                        }
                    }
                    
                    if (!ignore)
                    {                        
                        string jsonPropertyName;
                        if (jsonPropertyAttr != null && jsonPropertyAttr.Name != null)
                        {
                            jsonPropertyName = jsonPropertyAttr.Name;
                        }
                        else
                        {
                            if (options == null || options.UseCamelCase)
                                jsonPropertyName = ConvertToCamelCase(propertyInfo.Name);
                            else
                                jsonPropertyName = propertyInfo.Name;
                        }

                        var propertyMapping = new JObjectPropertyMapper()
                        {
                            ClassPropertyInfo = propertyInfo,                            
                            JObjectPropertyName = jsonPropertyName,
                            MapperType = null
                        };

                        if (jsonPropertyAttr != null)
                        {
                            propertyMapping.MapperType = jsonPropertyAttr.MapperType;
                        }
                        else if (jsonDateTimeAsEpochMillisecondsFound)
                        {
                            propertyMapping.MapperType = typeof(Mappers.DateTimeAsEpochMillisecondsMapper);
                        }

                        propertyMappings.Add(propertyMapping);
                    }
                }

                return new JObjectStructuredClassMapper(propertyMappings);
            }
            else
            {
                throw new Exception("Unsupported type: " + type);
            }
        }


        private static string ConvertToCamelCase(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            int firstLowerCaseIndex = 0;
            while (firstLowerCaseIndex < str.Length)
            {
                if (Char.IsLower(str[firstLowerCaseIndex]))
                {
                    break;
                }

                firstLowerCaseIndex++;
            }

            if (firstLowerCaseIndex == 0)
            {
                return str;
            }
            else if (firstLowerCaseIndex == 1)
            {
                return str.Substring(0, 1).ToLowerInvariant() + str.Substring(1);
            }
            else if (firstLowerCaseIndex >= str.Length)
            {
                return str.ToLowerInvariant();
            }            
            else
            {
                // Everything up to last capital should be lower-cased (i.e. IPAddress to ipAddress).
                return str.Substring(0, firstLowerCaseIndex - 1) + str.Substring(firstLowerCaseIndex - 1);
            }
        }
    }

    public class JObjectPropertyMapper : IJObjectPropertyMapper
    {
        private PropertyInfo classPropertyInfo;

        public JObjectPropertyMapper()
        {
        }

        public PropertyInfo ClassPropertyInfo
        {
            get { return this.classPropertyInfo; }

            set
            {
                this.classPropertyInfo = value;
                this.ClassPropertyType = this.classPropertyInfo.PropertyType;
                this.ClassPropertyName = this.classPropertyInfo.Name;
            }
        }

        public Type ClassPropertyType { get; private set; }

        public string ClassPropertyName { get; private set; }
        public string JObjectPropertyName { get; set; }

        public Type MapperType { get; set; }

        public void SetInstanceValue(object instance, object jObject, IJObjectRootMapper rootMapper)
        {            
            object processedValue;
            if (this.MapperType != null)
            {
                IJObjectMapper mapper = rootMapper.MapperProvider.GetJObjectMapperByMapperType(this.MapperType);                
                processedValue = mapper.FromJObject(jObject, this.ClassPropertyType, rootMapper);
            }
            else
            {
                processedValue = rootMapper.FromJObject(jObject, this.ClassPropertyType);
            }

            this.classPropertyInfo.SetValue(instance, processedValue, null);
        }

        public object GetJObjectValue(object instance, IJObjectRootMapper rootMapper)
        {
            object value = this.classPropertyInfo.GetValue(instance, null);
            object jObjectValue;
            if (this.MapperType != null)
            {
                IJObjectMapper mapper = rootMapper.MapperProvider.GetJObjectMapperByMapperType(this.MapperType);
                jObjectValue = mapper.ToJObject(value, rootMapper);
            }
            else
            {
                jObjectValue = rootMapper.ToJObject(value);
            }

            return jObjectValue;
        }
    }

    public interface IJObjectPropertyMapper
    {
        string JObjectPropertyName { get; set; }

        void SetInstanceValue(object instance, object jObject, IJObjectRootMapper rootMapper);
        object GetJObjectValue(object instance, IJObjectRootMapper rootMapper); 
    }

    public class DefaultClassMappingOptions
    {
        public DefaultClassMappingOptions()
        {
            this.UseCamelCase = true;
        }

        public bool UseCamelCase { get; set; }
    }
}
