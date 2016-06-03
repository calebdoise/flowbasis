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
            this.dispatcherResolver = options.DispatcherResolver;
        }

        public static IQueueSubscription Subscribe(IJsonMessageQueue jsonMessageQueue, IJsonMessageDispatcherResolver dispatcherResolver)
        {
            var listener = new JsonMessageQueueListener(
                new JsonMessageQueueListenerOptions
                {
                    DispatcherResolver = dispatcherResolver
                });
            return jsonMessageQueue.Subscribe(listener.MessageHandler);
        }       

        public void MessageHandler(JsonMessageContext messageContext)
        {
            IJsonMessageDispatcher dispatcher = this.dispatcherResolver.GetDispatcher(messageContext);
            dispatcher.Dispatch(messageContext);
        }
    }

    public class JsonMessageQueueListenerOptions
    {       
        public IJsonMessageDispatcherResolver DispatcherResolver { get; set; }
    }
}
