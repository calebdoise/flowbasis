using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues
{
    public interface ISimpleQueueManager
    {
        ISimpleQueue RegisterQueue(string queueName, SimpleQueueMode queueMode);

        ISimpleQueue GetQueue(string queueName);
    }
}
