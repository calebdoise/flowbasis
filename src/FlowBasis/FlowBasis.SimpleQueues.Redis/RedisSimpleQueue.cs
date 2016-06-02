using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowBasis.SimpleQueues.Redis
{
    public class RedisSimpleQueue : ISimpleQueue
    {
        private ConnectionMultiplexer redisConnection;

        private SimpleQueueMode queueMode;

        private string queueName;
        private string queueListName;
        private string queueNotificationChannelName;

        private IDatabase db;
        private ISubscriber sub;

        public RedisSimpleQueue(ConnectionMultiplexer redisConnection, string queueName, SimpleQueueMode queueMode = SimpleQueueMode.Queue)
        {
            this.queueMode = queueMode;

            this.redisConnection = redisConnection;
            this.db = this.redisConnection.GetDatabase();
            this.sub = this.redisConnection.GetSubscriber();

            this.queueName = queueName;

            if (this.queueMode == SimpleQueueMode.Queue)
            {                
                this.queueListName = "queue--" + queueName;
                this.queueNotificationChannelName = "queueChannel--" + queueName;
            }
            else if (this.queueMode == SimpleQueueMode.FanOut)
            {                
                this.queueNotificationChannelName = "topicChannel--" + queueName;
            }
            else
            {
                throw new Exception($"Unexpected queue mode: {this.queueMode}");
            }
        }

        public void Publish(string message)
        {
            if (this.queueMode == SimpleQueueMode.Queue)
            {
                this.db.ListLeftPush(this.queueListName, message, flags: CommandFlags.FireAndForget);

                // This will tell listeners to check for a message.
                this.sub.Publish(this.queueNotificationChannelName, "");
            }
            else if (this.queueMode == SimpleQueueMode.FanOut)
            {
                this.sub.Publish(this.queueNotificationChannelName, message, CommandFlags.FireAndForget);
            }
        }

        public ISimpleQueueSubscription Subscribe(Action<string> messageCallback)
        {
            if (this.queueMode == SimpleQueueMode.Queue)
            {
                Action<RedisChannel, RedisValue> subCallback = null;
                subCallback = (channel, value) =>
                {
                    string message = db.ListRightPop(this.queueListName);
                    if (message != null)
                    {
                        try
                        {
                            messageCallback(message);
                        }
                        finally
                        {
                            // See if there is another message.
                            subCallback("", "");
                        }
                    }
                };

                this.sub.Subscribe(this.queueNotificationChannelName, subCallback);

                // Check to see if there is an initial message in the queue.
                ThreadPool.QueueUserWorkItem((state) => subCallback("", ""));

                return new RedisSimpleQueueSubscription(this, subCallback);
            }
            else if (this.queueMode == SimpleQueueMode.FanOut)
            {
                Action<RedisChannel, RedisValue> subCallback = null;
                subCallback = (channel, value) =>
                {
                    string message = value;
                    if (message != null)
                    {
                        messageCallback(message);
                    }
                };

                this.sub.Subscribe(this.queueNotificationChannelName, subCallback);

                return new RedisSimpleQueueSubscription(this, subCallback);
            }
            else
            {
                throw new Exception($"Unexpected queue mode: {this.queueMode}");
            }
        }

        public void UnsubscribeAll()
        {
            this.sub.UnsubscribeAll();
        }


        private class RedisSimpleQueueSubscription : ISimpleQueueSubscription
        {
            private RedisSimpleQueue queue;
            private Action<RedisChannel, RedisValue> subCallback;

            public RedisSimpleQueueSubscription(
                RedisSimpleQueue queue,
                Action<RedisChannel, RedisValue> subCallback)
            {
                this.queue = queue;
                this.subCallback = subCallback;
            }

            public void Unsubscribe()
            {
                this.queue.sub.Unsubscribe(this.queue.queueNotificationChannelName, this.subCallback);
            }
        }
    }
}
