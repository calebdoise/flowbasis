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
        Task PublishAsync(string message);

        IQueueSubscription Subscribe(Action<string> messageCallback);
        Task<IQueueSubscription> SubscribeAsync(Action<string> messageCallback);
    }

    public interface IQueueSubscription
    {
        void Unsubscribe();
        Task UnsubscribeAsync();
    }

    public enum QueueMode
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
