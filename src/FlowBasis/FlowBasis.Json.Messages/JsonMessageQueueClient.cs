using FlowBasis.SimpleQueues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Messages
{
    public class JsonMessageQueueClient : IJsonMessageQueueClient
    {
        private string queueName;
        private IServiceProvider serviceProvider;
        private ISimpleQueueManager queueManager;
        private Func<JsonMessageContext, string> customMessageFormatter;

        public JsonMessageQueueClient(JsonMessageQueueClientOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.queueName = options.QueueName;
            this.queueManager = options.QueueManager;
            this.serviceProvider = options.ServiceProvider;
            this.customMessageFormatter = options.CustomMessageFormatter;
        }

        public JsonMessageQueueClient(ISimpleQueueManager queueManager, string queueName, IServiceProvider serviceProvider = null)
        {
            this.queueManager = queueManager;
            this.queueName = queueName;
            this.serviceProvider = serviceProvider;
        }

        public void SendMessage(JsonMessageContext messageContext)
        {
            IJsonSerializationService jsonSerializer;
            ISimpleQueueManager queueManagerToUse = this.queueManager;

            if (this.serviceProvider != null)
            {
                jsonSerializer = (IJsonSerializationService)this.serviceProvider.GetService(typeof(IJsonSerializationService));
                if (queueManagerToUse == null)
                {
                    queueManagerToUse = (ISimpleQueueManager)this.serviceProvider.GetService(typeof(ISimpleQueueManager));
                }
            }
            else
            {
                jsonSerializer = JsonSerializers.Default;
            }

            if (queueManagerToUse == null)
            {
                throw new Exception("ISimpleQueueManager service not found.");
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

            ISimpleQueue queue = queueManagerToUse.GetQueue(this.queueName);
            queue.Publish(messageStr);
        }
    }

    public class JsonMessageQueueClientOptions
    {
        public string QueueName { get; set; }

        public ISimpleQueueManager QueueManager { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// If CustomMessageFormatter is not set, the JsonMessageContext will be serialized as a JsonMessageContextData data structure.
        /// </summary>
        public Func<JsonMessageContext, string> CustomMessageFormatter { get; set; }
    }

    public class JsonMessageContextData
    {
        public List<JsonMessageHeader> Headers { get; set; }
        public object Body { get; set; }
    }

}
