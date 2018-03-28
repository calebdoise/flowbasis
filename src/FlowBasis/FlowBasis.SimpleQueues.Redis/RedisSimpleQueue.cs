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

        private QueueMode queueMode;
        private RedisSimpleQueueOptions options;

        private string queueName;
        private string queueListName;
        private string queueNotificationChannelName;

        private IDatabase db;
        private ISubscriber sub;

        public RedisSimpleQueue(ConnectionMultiplexer redisConnection, string queueName, QueueMode queueMode = QueueMode.Queue, RedisSimpleQueueOptions options = null)
        {
            this.options = options ?? new RedisSimpleQueueOptions();

            this.queueMode = queueMode;

            this.redisConnection = redisConnection;
            this.db = this.redisConnection.GetDatabase();
            this.sub = this.redisConnection.GetSubscriber();

            this.queueName = queueName;

            string namespaceChannelPrefix = (this.options.Namespace != null) ? (this.options.Namespace + "--") : String.Empty;
            if (this.queueMode == QueueMode.Queue)
            {                
                this.queueListName = namespaceChannelPrefix + "queue--" + queueName;
                this.queueNotificationChannelName = namespaceChannelPrefix + "queueChannel--" + queueName;
            }
            else if (this.queueMode == QueueMode.FanOut)
            {                
                this.queueNotificationChannelName = namespaceChannelPrefix + "topicChannel--" + queueName;
            }
            else
            {
                throw new Exception($"Unexpected queue mode: {this.queueMode}");
            }
        }

        public void Publish(string message)
        {
            if (this.queueMode == QueueMode.Queue)
            {
                this.db.ListLeftPush(this.queueListName, message, flags: options.PublishCommandFlags);

                // This will tell listeners to check for a message.
                this.sub.Publish(this.queueNotificationChannelName, "");
            }
            else if (this.queueMode == QueueMode.FanOut)
            {
                this.sub.Publish(this.queueNotificationChannelName, message, options.PublishCommandFlags);
            }
        }

        public async Task PublishAsync(string message)
        {
            if (this.queueMode == QueueMode.Queue)
            {
                await this.db.ListLeftPushAsync(this.queueListName, message, flags: options.PublishCommandFlags);

                // This will tell listeners to check for a message.
                await this.sub.PublishAsync(this.queueNotificationChannelName, "");
            }
            else if (this.queueMode == QueueMode.FanOut)
            {
                await this.sub.PublishAsync(this.queueNotificationChannelName, message, options.PublishCommandFlags);
            }
        }

        public IQueueSubscription Subscribe(Action<string> messageCallback)
        {
            if (this.queueMode == QueueMode.Queue)
            {
                Action<RedisChannel, RedisValue> subCallback = null;
                subCallback = async (channel, value) =>
                {
                    string message = await db.ListRightPopAsync(this.queueListName);
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
            else if (this.queueMode == QueueMode.FanOut)
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

        public async Task<IQueueSubscription> SubscribeAsync(Action<string> messageCallback)
        {            
            if (this.queueMode == QueueMode.Queue)
            {
                Action<RedisChannel, RedisValue> subCallback = null;
                subCallback = async (channel, value) =>
                {
                    string message = await db.ListRightPopAsync(this.queueListName);                    
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
                
                await this.sub.SubscribeAsync(this.queueNotificationChannelName, subCallback);

                // Check to see if there is an initial message in the queue.
                ThreadPool.QueueUserWorkItem((state) => subCallback("", ""));                

                return new RedisSimpleQueueSubscription(this, subCallback);
            }
            else if (this.queueMode == QueueMode.FanOut)
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

                await this.sub.SubscribeAsync(this.queueNotificationChannelName, subCallback);

                return new RedisSimpleQueueSubscription(this, subCallback);
            }
            else
            {
                throw new Exception($"Unexpected queue mode: {this.queueMode}");
            }
        }        


        private class RedisSimpleQueueSubscription : IQueueSubscription
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

            public async Task UnsubscribeAsync()
            {
                await this.queue.sub.UnsubscribeAsync(this.queue.queueNotificationChannelName, this.subCallback);
            }
        }
    }

    public class RedisSimpleQueueOptions
    {
        public RedisSimpleQueueOptions()
        {
            this.PublishCommandFlags = CommandFlags.FireAndForget;
        }

        /// <summary>
        /// If non-null, the namespace will be used as a prefix
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// PublishCommandFlags defaults to CommandFlags.FireAndForget. Change to CommandFlags.None if you wish to wait for a response.
        /// </summary>
        public CommandFlags PublishCommandFlags { get; set; }
    }
}
