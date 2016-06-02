using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{
    public class JsonSerializationService : IJsonSerializationService
    {        
        private Func<IJObjectRootMapper> rootMapperFactory;
        private int? maxDepth = null;

        public JsonSerializationService(Func<IJObjectRootMapper> rootMapperFactory)
        {
            this.rootMapperFactory = rootMapperFactory;
        }        

        public int? MaxDepth
        {
            get { return this.maxDepth; }
            set { this.maxDepth = value; }
        }

        public string Stringify(object value)
        {
            var rootMapper = this.rootMapperFactory();
            object jObject = rootMapper.ToJObject(value);

            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.FloatFormatHandling = Newtonsoft.Json.FloatFormatHandling.String;
            serializer.MaxDepth = this.maxDepth;
            serializer.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None;
            serializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            serializer.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;

            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, jObject);
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
