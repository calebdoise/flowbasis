using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues
{
    public class QueueManager<QueueType> : IQueueManager<QueueType>
    {        
        private Dictionary<string, RegisteredQueue<QueueType>> queueNameToEntryMap = new Dictionary<string, RegisteredQueue<QueueType>>();
            
        public QueueManager()
        {        
        }

        public QueueType GetQueue(string queueName)
        {
            RegisteredQueue<QueueType> registeredQueue;
            if (this.queueNameToEntryMap.TryGetValue(queueName, out registeredQueue))
            {
                return registeredQueue.Queue;
            }
            else
            {
                throw new Exception($"Queue is not registered: {queueName}");
            }
        }

        public void RegisterQueue(string queueName, QueueType queue)
        {
            lock (this)
            {
                if (this.queueNameToEntryMap.ContainsKey(queueName))
                {
                    throw new Exception($"Queue is already registered: {queueName}");
                }
         
                var registeredQueue = new RegisteredQueue<QueueType>
                {
                    Name = queueName,
                    Queue = queue
                };

                this.queueNameToEntryMap[queueName] = registeredQueue;                
            }
        }

        private class RegisteredQueue<QueueType2>
        {
            public string Name { get; set; }
            public QueueType2 Queue { get; set; }
        }
    }   

    public class SimpleQueueManager : QueueManager<ISimpleQueue>
    {
    }
}
