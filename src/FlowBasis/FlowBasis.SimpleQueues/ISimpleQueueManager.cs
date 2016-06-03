using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues
{
    public interface ISimpleQueueManager
    {        
        void RegisterQueue(string queueName, ISimpleQueue queue);

        ISimpleQueue GetQueue(string queueName);
    }
}
