using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues
{
    public interface IQueueManager<QueueType>
    {        
        void RegisterQueue(string queueName, QueueType queue);

        QueueType GetQueue(string queueName);
    }

    public interface ISimpleQueueManager : IQueueManager<ISimpleQueue>
    {
    }
}
