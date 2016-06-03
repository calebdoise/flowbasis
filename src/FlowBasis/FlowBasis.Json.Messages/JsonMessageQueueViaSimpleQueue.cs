using FlowBasis.SimpleQueues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Messages
{
    public class JsonMessageQueueViaSimpleQueue : IJsonMessageQueue
    {
        private string queueName;
        private IServiceProvider serviceProvider;
        private IQueueManager<ISimpleQueue> queueManager;
        private Func<JsonMessageContext, string> customMessageFormatter;
        private Func<string, JsonMessageContext> customMessageParser;

        public JsonMessageQueueViaSimpleQueue(JsonMessageQueueViaSimpleQueueOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.queueName = options.QueueName;
            this.queueManager = options.QueueManager;
            this.serviceProvider = options.ServiceProvider;
            this.customMessageFormatter = options.CustomMessageFormatter;
            this.customMessageParser = options.CustomMessageParser;
        }

        public JsonMessageQueueViaSimpleQueue(IQueueManager<ISimpleQueue> queueManager, string queueName, IServiceProvider serviceProvider = null)
        {
            this.queueManager = queueManager;
            this.queueName = queueName;
            this.serviceProvider = serviceProvider;
        }

        private ISimpleQueue GetSimpleQueue()
        {
            IQueueManager<ISimpleQueue> queueManagerToUse = this.queueManager;
            if (this.serviceProvider != null)
            {                
                if (queueManagerToUse == null)
                {
                    queueManagerToUse = (IQueueManager<ISimpleQueue>)this.serviceProvider.GetService(typeof(IQueueManager<ISimpleQueue>));
                }
            }

            if (queueManagerToUse == null)
            {
                throw new Exception("IQueueManager<ISimpleQueue> service not found.");
            }

            ISimpleQueue queue = queueManagerToUse.GetQueue(this.queueName);
            return queue;
        }

        public void Publish(JsonMessageContext messageContext)
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
            
            string messageStr;

            if (this.customMessageFormatter != null)
            {
                messageStr = this.customMessageFormatter(messageContext);
            }
            else
            {
                var messageData = new JsonMessageContextData
                {
                    Headers = messageContext.Headers,
                    Body = messageContext.Body
                };

                messageStr = jsonSerializer.Stringify(messageData);
            }

            ISimpleQueue queue = this.GetSimpleQueue();
            queue.Publish(messageStr);
        }


        public IQueueSubscription Subscribe(Action<JsonMessageContext> messageContextCallback)
        {
            ISimpleQueue queue = this.GetSimpleQueue();

            return queue.Subscribe((string message) =>
            {
                JsonMessageContext messageContext;

                if (this.customMessageParser != null)
                {
                    messageContext = this.customMessageParser(message);
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

                    var messageData = jsonSerializer.Parse<JsonMessageContextData>(message);
                    messageContext = new JsonMessageContext(messageData.Headers, messageData.Body as JObject);
                }

                messageContextCallback(messageContext);
            });
        }


        public void UnsubscribeAll()
        {
            ISimpleQueue queue = this.GetSimpleQueue();
            queue.UnsubscribeAll();
        }
    }

    public class JsonMessageQueueViaSimpleQueueOptions
    {
        public string QueueName { get; set; }

        public IQueueManager<ISimpleQueue> QueueManager { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// If CustomMessageFormatter is not set, the JsonMessageContext will be serialized as a JsonMessageContextData data structure.
        /// </summary>
        public Func<JsonMessageContext, string> CustomMessageFormatter { get; set; }

        public Func<string, JsonMessageContext> CustomMessageParser { get; set; }
    }

    public class JsonMessageContextData
    {
        public List<JsonMessageHeader> Headers { get; set; }
        public object Body { get; set; }
    }

}
