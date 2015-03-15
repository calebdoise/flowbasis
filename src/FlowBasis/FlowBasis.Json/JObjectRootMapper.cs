using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace FlowBasis.Json
{    

    public class JObjectRootMapper : IJObjectRootMapper
    {
        private IJObjectMapperProvider mapperProvider;        

        public JObjectRootMapper()
        {
            this.mapperProvider = new DefaultJObjectMapperProvider();
        }

        public JObjectRootMapper(IJObjectMapperProvider mapperProvider)
        {
            this.mapperProvider = mapperProvider;
        }


        public object ToJObject(object value)
        {
            return this.ToJObject(value, this);
        }

        public object ToJObject(object value, IJObjectRootMapper rootMapper)
        {
            if (value == null)
                return null;

            IJObjectMapper mapper = this.mapperProvider.ResovleJObjectMapperForInstance(value);
            if (mapper != null)
            {
                return mapper.ToJObject(value, rootMapper);
            }
            else
            {
                return null;
            }            
        }


        public object FromJObject(object jObject, Type targetType)
        {
            return this.FromJObject(jObject, targetType, this);
        }

        public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
        {
            if (jObject == null)
            {
                return null;
            }

            IJObjectMapper mapper = this.mapperProvider.ResovleJObjectMapperForJObject(jObject, targetType);
            if (mapper != null)
            {
                return mapper.FromJObject(jObject, targetType, rootMapper);
            }
            else
            {
                return null;
            }                                           
        }


        public IJObjectMapperProvider MapperProvider 
        {
            get { return this.mapperProvider; }
        }
    }

    
}
