using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues
{
    public interface ISimpleQueue
    {
        void Publish(string message);
        IQueueSubscription Subscribe(Action<string> messageCallback);

        void UnsubscribeAll();
    }

    public interface IQueueSubscription
    {
        void Unsubscribe();
    }

    public enum SimpleQueueMode
    {
        /// <summary>
        /// Only one subscriber will see each message.
        /// </summary>
        Queue,

        /// <summary>
        /// All active subscribers will see each message.
        /// </summary>
        FanOut
    }
}
