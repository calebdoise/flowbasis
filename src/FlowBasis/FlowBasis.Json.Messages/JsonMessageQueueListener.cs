using FlowBasis.SimpleQueues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Messages
{
    public class JsonMessageQueueListener
    {
        private JsonMessageQueueListenerOptions options;
        private IServiceProvider serviceProvider;
        private Func<string, JsonMessageContext> customMessageParser;

        private IJsonMessageDispatcherResolver dispatcherResolver;


        public JsonMessageQueueListener(JsonMessageQueueListenerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (options.DispatcherResolver == null)
            {
                throw new Exception("options.DispatcherResolver is not specified");
            }

            this.options = options;
            this.serviceProvider = options.ServiceProvider;
            this.customMessageParser = options.CustomMessageParser;
            this.dispatcherResolver = options.DispatcherResolver;
        }

        public static ISimpleQueueSubscription Subscribe(ISimpleQueue simpleQueue, JsonMessageQueueListenerOptions options)
        {
            var listener = new JsonMessageQueueListener(options);
            return simpleQueue.Subscribe(listener.MessageHandler);
        }

        public void MessageHandler(string messageString)
        {
            JsonMessageContext messageContext;

            if (this.customMessageParser != null)
            {
                messageContext = this.customMessageParser(messageString);
            }
            else
            {
                IJsonSerializationService jsonSerializer;

                if (this.serviceProvider != null)
                {
                    jsonSerializer = (IJsonSerializationService)this.serviceProvider.GetService(typeof(IJsonSerializationService));                    
                }
                else
                {
                    jsonSerializer = JsonSerializers.Default;
                }

                var messageData = jsonSerializer.Parse<JsonMessageContextData>(messageString);
                messageContext = new JsonMessageContext(messageData.Headers, messageData.Body as JObject);
            }
      
            IJsonMessageDispatcher dispatcher = this.dispatcherResolver.GetDispatcher(messageContext);
            dispatcher.Dispatch(messageContext);            
        }
    }

    public class JsonMessageQueueListenerOptions
    {
        public IServiceProvider ServiceProvider { get; set; }

        public Func<string, JsonMessageContext> CustomMessageParser { get; set; }

        public IJsonMessageDispatcherResolver DispatcherResolver { get; set; }
    }
}
