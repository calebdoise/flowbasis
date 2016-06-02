using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues
{
    public class SimpleQueueManager : ISimpleQueueManager
    {
        private SimpleQueueManagerOptions options;

        private Dictionary<string, RegisteredQueue> queueNameToEntryMap = new Dictionary<string, RegisteredQueue>();
            
        public SimpleQueueManager(SimpleQueueManagerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.options = options;
        }

        public ISimpleQueue GetQueue(string queueName)
        {
            RegisteredQueue registeredQueue;
            if (this.queueNameToEntryMap.TryGetValue(queueName, out registeredQueue))
            {
                return registeredQueue.Queue;
            }
            else
            {
                throw new Exception($"Queue is not registered: {queueName}");
            }
        }

        public ISimpleQueue RegisterQueue(string queueName, SimpleQueueMode queueMode)
        {
            lock (this)
            {
                if (this.queueNameToEntryMap.ContainsKey(queueName))
                {
                    throw new Exception($"Queue is already registered: {queueName}");
                }

                ISimpleQueue queue = this.options.CreateQueueHandler(queueName, queueMode);

                var registeredQueue = new RegisteredQueue
                {
                    Name = queueName,
                    Queue = queue
                };

                this.queueNameToEntryMap[queueName] = registeredQueue;

                return queue;
            }
        }

        private class RegisteredQueue
        {
            public string Name { get; set; }
            public ISimpleQueue Queue { get; set; }
        }
    }

    public class SimpleQueueManagerOptions
    {
        public CreateQueueHandler CreateQueueHandler { get; set; }
    }

    public delegate ISimpleQueue CreateQueueHandler(string queueName, SimpleQueueMode queueMode);
}
