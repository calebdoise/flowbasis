using FlowBasis.SimpleQueues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Messages
{
    public interface IJsonMessageQueue
    {
        void Publish(JsonMessageContext messageContext);
        IQueueSubscription Subscribe(Action<JsonMessageContext> messageContextCallback);

        void UnsubscribeAll();
    }
}
