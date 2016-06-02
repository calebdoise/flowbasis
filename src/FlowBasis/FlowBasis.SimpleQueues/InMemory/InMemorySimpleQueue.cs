using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues.InMemory
{
    public class InMemorySimpleQueue : ISimpleQueue
    {
        private SimpleQueueMode queueMode;

        private Queue<string> currentMessages = new Queue<string>();
        private List<Action<string>> subscribers = new List<Action<string>>();

        private int lastCallbackIndex = -1;

        public InMemorySimpleQueue(SimpleQueueMode queueMode)
        {
            this.queueMode = queueMode;
        }

        public void Publish(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (this.queueMode == SimpleQueueMode.FanOut)
            {
                // All active callbacks should get the message.
                Action<string>[] callbacks;

                lock (this)
                {
                    callbacks = this.subscribers.ToArray();
                }

                foreach (var callback in callbacks)
                {
                    ThreadPool.QueueUserWorkItem((state) => { callback(message); });                    
                }
            }
            else if (this.queueMode == SimpleQueueMode.Queue)
            {
                lock (this)
                {
                    this.currentMessages.Enqueue(message);
                }

                this.TryGetMessageFromQueue();
            }
        }

        private void TryGetMessageFromQueue()
        {
            string message = null;
            bool checkForAnotherMessage = false;

            lock (this)
            {
                if (this.currentMessages.Count > 0)
                {
                    // Only one of the callbacks should get the message
                    Action<string> callback = null;

                    Action<string>[] callbacks = this.subscribers.ToArray();
                    if (callbacks != null && callbacks.Length > 0)
                    {
                        int nextCallbackIndex = Math.Max(0, (this.lastCallbackIndex + 1) % callbacks.Length);
                        if (nextCallbackIndex < callbacks.Length)
                        {
                            callback = callbacks[nextCallbackIndex];
                        }

                        this.lastCallbackIndex = nextCallbackIndex;
                    }

                    if (callback != null)
                    {
                        message = this.currentMessages.Dequeue();                        
                        ThreadPool.QueueUserWorkItem(
                            (state) => 
                            {
                                try
                                {
                                    callback(message);
                                }
                                finally
                                {
                                    // Check for another message.
                                    this.TryGetMessageFromQueue();
                                }
                            });

                        if (this.currentMessages.Count > 0)
                        {
                            checkForAnotherMessage = true;
                        }
                    }                                                   
                }
            }        

            if (checkForAnotherMessage)
            {
                this.TryGetMessageFromQueue();
            }
        }

        public ISimpleQueueSubscription Subscribe(Action<string> messageCallback)
        {
            lock (this)
            {
                this.subscribers.Add(messageCallback);
            }

            this.TryGetMessageFromQueue();

            return new InMemorySimpleQueueSubscription(this, messageCallback);
        }

        public void UnsubscribeAll()
        {
            lock (this)
            {
                this.subscribers.Clear();
            }
        }

        private class InMemorySimpleQueueSubscription : ISimpleQueueSubscription
        {
            private InMemorySimpleQueue queue;
            private Action<string> messageCallback;

            public InMemorySimpleQueueSubscription(InMemorySimpleQueue queue, Action<string> messageCallback)
            {
                this.queue = queue;
                this.messageCallback = messageCallback;
            }

            public void Unsubscribe()
            {
                lock (this.queue)
                {
                    this.queue.subscribers.Remove(messageCallback);
                }
            }
            
        }
    }    
}
