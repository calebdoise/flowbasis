using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues
{
    public class SimpleQueueManager : ISimpleQueueManager
    {        
        private Dictionary<string, RegisteredQueue> queueNameToEntryMap = new Dictionary<string, RegisteredQueue>();
            
        public SimpleQueueManager()
        {        
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

        public void RegisterQueue(string queueName, ISimpleQueue queue)
        {
            lock (this)
            {
                if (this.queueNameToEntryMap.ContainsKey(queueName))
                {
                    throw new Exception($"Queue is already registered: {queueName}");
                }
         
                var registeredQueue = new RegisteredQueue
                {
                    Name = queueName,
                    Queue = queue
                };

                this.queueNameToEntryMap[queueName] = registeredQueue;                
            }
        }

        private class RegisteredQueue
        {
            public string Name { get; set; }
            public ISimpleQueue Queue { get; set; }
        }
    }   
}
