using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json
{
    public static class JsonSerializers
    {
        private static IJObjectRootMapper defaultRootMapper;
        private static IJsonSerializationService defaultJsonSerializer;

        private static IJObjectRootMapper noCamelCasingRootMapper;
        private static IJsonSerializationService noCamelCasingSerializer;

        static JsonSerializers()
        {
            defaultRootMapper = new JObjectRootMapper(new DefaultJObjectMapperProvider());
            defaultJsonSerializer = new JsonSerializationService(() => defaultRootMapper);

            noCamelCasingRootMapper = new JObjectRootMapper(
                new DefaultJObjectMapperProvider(
                    new DefaultJObjectMapperProviderOptions
                    {
                        DefaultClassMappingOptions = new DefaultClassMappingOptions
                        {
                            UseCamelCase = false
                        }
                    }));
            noCamelCasingSerializer = new JsonSerializationService(() => noCamelCasingRootMapper);
        }

        public static IJsonSerializationService Default
        {
            get { return defaultJsonSerializer; }
        }

        public static IJsonSerializationService NoCamelCasing
        {
            get { return noCamelCasingSerializer; }
        }
    }
}
