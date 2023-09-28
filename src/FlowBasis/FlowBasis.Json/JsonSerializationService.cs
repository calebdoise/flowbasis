using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{
    public class JsonSerializationService : IJsonSerializationService
    {
        private readonly Func<IJObjectRootMapper> rootMapperFactory;
        private readonly Newtonsoft.Json.JsonSerializer serializer;

        public JsonSerializationService(Func<IJObjectRootMapper> rootMapperFactory)
        {
            this.rootMapperFactory = rootMapperFactory;
            this.serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.FloatFormatHandling = Newtonsoft.Json.FloatFormatHandling.String;
            serializer.MaxDepth = this.MaxDepth;
            serializer.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None;
            serializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            serializer.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
        }

        public int? MaxDepth
        {
            get => this.serializer.MaxDepth;
            set => this.serializer.MaxDepth = value;
        }

        public string Stringify(object value)
        {
            var rootMapper = this.rootMapperFactory();
            object jObject = rootMapper.ToJObject(value);

            using (var sw = new StringWriter())
            {
                this.serializer.Serialize(sw, jObject);
                string json = sw.ToString();
                return json;
            }
        }

        public object Parse(string json)
        {
            object jObject = JObject.Parse(json);
            return jObject;
        }

        public object Parse(string json, Type targetType)
        {
            object jObject = JObject.Parse(json);

            var rootMapper = this.rootMapperFactory();
            object value = rootMapper.FromJObject(jObject, targetType);
            return value;
        }

        public T Parse<T>(string json)
        {
            return (T)Parse(json, typeof(T));
        }

        public object Map(object jObject, Type targetType)
        {
            var rootMapper = this.rootMapperFactory();
            object targetValue = rootMapper.FromJObject(jObject, targetType);
            return targetValue;
        }

        public T Map<T>(object jObject)
        {
            return (T)Map(jObject, typeof(T));
        }
    }
}
