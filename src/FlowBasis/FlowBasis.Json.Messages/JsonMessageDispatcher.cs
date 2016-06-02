using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Messages
{
    public class JsonMessageDispatcher : IJsonMessageDispatcher
    {
        private JsonMessageDispatchInfo dispatcherInfo;
        private IServiceProvider serviceProvider;        

        public JsonMessageDispatcher(JsonMessageDispatchInfo dispatcherInfo, IServiceProvider serviceProvider = null)
        {
            this.dispatcherInfo = dispatcherInfo;
            this.serviceProvider = serviceProvider;
        }

        public void Dispatch(JsonMessageContext messageContext)
        {
            if (this.dispatcherInfo.DispatchCallback != null)
            {
                this.dispatcherInfo.DispatchCallback(messageContext);
            }
            else if (this.dispatcherInfo.DispatchControllerType != null)
            {
                this.DispatchUsingStronglyTypedData(messageContext);
            }
            else
            {
                throw new Exception("Unable to determine how to dispatch message.");
            }
        }

        private void DispatchUsingStronglyTypedData(JsonMessageContext messageContext)
        {
            IJsonSerializationService jsonSerializer = null;
            object dispatchController = null;

            if (this.serviceProvider != null)
            {
                jsonSerializer = (IJsonSerializationService)this.serviceProvider.GetService(typeof(IJsonSerializationService));
                dispatchController = this.serviceProvider.GetService(this.dispatcherInfo.DispatchControllerType);
            }
            else
            {
                jsonSerializer = JsonSerializers.Default;
                dispatchController = Activator.CreateInstance(this.dispatcherInfo.DispatchControllerType);
            }

            if (this.dispatcherInfo.DispatchMethod == null)
            {
                throw new Exception("Expected DispatchMethod to be set.");
            }

            ParameterInfo[] paramInfos = this.dispatcherInfo.DispatchMethod.GetParameters();
            object[] paramValues = new object[paramInfos.Length];

            JObject body = messageContext.Body as JObject;

            for (int co = 0; co < paramInfos.Length; co++)
            {
                var paramInfo = paramInfos[co];

                object paramValue = null;

                if (paramInfo.Name == "messageContext" && paramInfo.ParameterType.IsAssignableFrom(typeof(JsonMessageContext)))
                {
                    paramValue = messageContext;
                }
                else if (paramInfo.Name == "messageBody")
                {
                    paramValue = jsonSerializer.Map(body, paramInfo.ParameterType);
                }
                else
                {
                    if (body != null)
                    {
                        object bodyPropertyValue = body[paramInfo.Name];
                        if (bodyPropertyValue != null)
                        {
                            paramValue = jsonSerializer.Map(bodyPropertyValue, paramInfo.ParameterType);
                        }
                    }
                }

                paramValues[co] = paramValue;
            }

            this.dispatcherInfo.DispatchMethod.Invoke(dispatchController, paramValues);            
        }
    }
}
