using FlowBasis.Json;
using FlowBasis.SimpleNodes.Redis.Util;
using FlowBasis.SimpleQueues;
using FlowBasis.SimpleQueues.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlowBasis.SimpleNodes.Redis
{
    public class SimpleNodeForRedis
    {
        /// <summary>
        /// Key for the set of node ids that are part of the cluster.
        /// </summary>
        internal const string SNodesKey = "s-nodes";
        internal const string SNodeDescriptorHashKey = "s-nodes-desc";
        internal const string SNodeLifeHashKey = "s-nodes-life";        

        private SimpleNodeForRedisOptions options;

        private ConnectionMultiplexer connection;
        private IDatabase db;
        private string id;

        private bool startHasBeenCalled = false;
        private bool stopHasBeenCalled = false;
        private CancellationTokenSource stopCancellationTokenSource = new CancellationTokenSource();

        private QueueState nodeQueueState;

        private Dictionary<string, QueueState> tagFanOutListenerMap;
        private Dictionary<string, QueueState> tagListenerMap;

        public SimpleNodeForRedis(ConnectionMultiplexer connection, SimpleNodeForRedisOptions options = null)
        {
            this.options = options ?? new SimpleNodeForRedisOptions();

            this.connection = connection;
            this.id = this.options.Id ?? Guid.NewGuid().ToString();
        }

        public string Id
        {
            get { return this.id; }
        }

        public void Start()
        {
            if (this.startHasBeenCalled)
            {
                throw new Exception("Start has already been called.");
            }

            if (this.options.Logger != null)
            {
                this.options.Logger($"SimpleNode started: {this.id}", SimpleNodeLogLevel.Information);

                if (this.options.Tags != null)
                {
                    this.options.Logger($"SimpleNode tags for {this.id}: {String.Join(", ", this.options.Tags)}", SimpleNodeLogLevel.Information);
                }
            }

            this.startHasBeenCalled = true;

            try
            {
                this.db = this.connection.GetDatabase();

                // Add to teh overall set of nodes.
                this.db.SetAdd(this.GetPropNameToUse(SNodesKey), this.id);

                // Publish descriptor.
                var descriptor = new SimpleNodeDescriptor
                {
                    Id = id,
                    Profile = this.options.Profile,
                    IsPersistent = this.options.IsPersistent,
                    StartUtcTimestamp = FlowBasis.Json.Util.TimeHelper.ToEpochMilliseconds(DateTime.UtcNow),
                    Labels = this.options.Tags?.ToArray()
                };
                string descriptorJson = this.SerializeJson(descriptor);
                this.db.HashSet(this.GetPropNameToUse(SNodeDescriptorHashKey), this.id, descriptorJson);

                var nodeQueue = new RedisSimpleQueue(
                    this.connection, this.GetNodeQueueName(this.id), QueueMode.Queue,
                    new RedisSimpleQueueOptions
                    {
                        Namespace = this.options.RedisNamespace
                    });
                var nodeQueueSubscription = nodeQueue.Subscribe(this.NodeMessageCallback);

                this.nodeQueueState = new QueueState(nodeQueue, nodeQueueSubscription);

                this.tagFanOutListenerMap = new Dictionary<string, QueueState>();
                this.tagListenerMap = new Dictionary<string, QueueState>();
                if (this.options.Tags != null)
                {
                    foreach (string tag in this.options.Tags)
                    {
                        var tagFanOutQueue = new RedisSimpleQueue(
                            this.connection, this.GetTagFanOutQueueName(tag), QueueMode.FanOut,
                            new RedisSimpleQueueOptions
                            {
                                Namespace = this.options.RedisNamespace
                            });
                        var tagFanOutQueueSubscription = tagFanOutQueue.Subscribe((message) => this.TagFanOutMessageCallback(tag, message));

                        this.tagFanOutListenerMap[tag] = new QueueState(tagFanOutQueue, tagFanOutQueueSubscription);

                        var tagQueue = new RedisSimpleQueue(
                            this.connection, this.GetTagQueueName(tag), QueueMode.Queue,
                            new RedisSimpleQueueOptions
                            {
                                Namespace = this.options.RedisNamespace
                            });
                        var tagQueueSubscription = tagQueue.Subscribe((message) => this.TagMessageCallback(tag, message));

                        this.tagListenerMap[tag] = new QueueState(tagQueue, tagQueueSubscription);
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.options.Logger != null)
                {
                    this.options.Logger($"SimpleNode startup failed ({this.Id}): {ex.Message}", SimpleNodeLogLevel.Error);
                }
                throw;
            }

            // This should be called once when the node is initialized.
            var heartbeatThread = new Thread(new ThreadStart(this.Heartbeat));
            heartbeatThread.IsBackground = true;
            heartbeatThread.Start();
        }

        private void Heartbeat()
        {
            while (!this.stopHasBeenCalled)
            {
                try
                {
                    if (this.options.Logger != null)
                    {
                        this.options.Logger($"SimpleNode heartbeat for node: {this.Id}", SimpleNodeLogLevel.Verbose);
                    }

                    this.UpdateLastHeartbeat();

                    Task.Delay(this.options.HeartbeatInterval, this.stopCancellationTokenSource.Token).ContinueWith(tsk => { }).Wait();
                }
                catch (Exception ex)
                {
                    if (this.options.Logger != null)
                    {
                        this.options.Logger($"SimpleNode heartbeat set failure ({this.Id}): {ex.Message}", SimpleNodeLogLevel.Error);
                    }
                }
            }

            if (this.options.Logger != null)
            {
                this.options.Logger($"SimpleNode heartbeat stopped: {this.Id}", SimpleNodeLogLevel.Information);
            }
        }

        private void UpdateLastHeartbeat()
        {
            this.db.HashSet(this.GetPropNameToUse(SNodeLifeHashKey), this.id, GetMillisecondsSince1970().ToString());
        }
        

        public void Stop()
        {
            if (this.startHasBeenCalled && !this.stopHasBeenCalled)
            {
                try
                {
                    this.stopHasBeenCalled = true;
                    this.stopCancellationTokenSource.Cancel();

                    if (this.nodeQueueState != null)
                    {
                        this.nodeQueueState.QueueSubscription.Unsubscribe();
                        this.nodeQueueState = null;
                    }

                    foreach (var queueState in this.tagFanOutListenerMap)
                    {
                        queueState.Value.QueueSubscription.Unsubscribe();
                    }

                    foreach (var queueState in this.tagListenerMap)
                    {
                        queueState.Value.QueueSubscription.Unsubscribe();
                    }

                    // TODO: Optionally cleanup queues specific to this node (it's probably better to do this separately; be sure to consider nodes that use stable ids).
                }
                finally
                {
                    var inspector = new SimpleNodeInspectorForRedis(this.connection, this.options.RedisNamespace);
                    inspector.TryCleanupStateForNode(this.id);
                }
            }
        }


        private void NodeMessageCallback(string message)
        {
            if (this.options.Logger != null)
            {
                this.options.Logger($"Received Node Message: {message}", SimpleNodeLogLevel.Verbose);
            }

            if (this.options.NodeMessageCallback != null)
            {
                this.options.NodeMessageCallback(message);
            }
        }

        private void TagMessageCallback(string tag, string message)
        {
            if (this.options.Logger != null)
            {
                this.options.Logger($"Received Tag Message ({tag}): {message}", SimpleNodeLogLevel.Verbose);
            }

            if (this.options.TagMessageCallback != null)
            {
                this.options.TagMessageCallback(tag, message);
            }
        }

        private void TagFanOutMessageCallback(string tag, string message)
        {
            if (this.options.Logger != null)
            {
                this.options.Logger($"Received Tag Fan-Out Message ({tag}): {message}", SimpleNodeLogLevel.Verbose);
            }

            if (this.options.TagFanOutMessageCallback != null)
            {
                this.options.TagFanOutMessageCallback(tag, message);
            }
        }

        /// <summary>
        /// Only the target node will see the message.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="message"></param>
        public void PublishToNode(string nodeId, string message)
        {
            var otherNodeQueue = new RedisSimpleQueue(
                this.connection, this.GetNodeQueueName(nodeId), QueueMode.Queue,
                new RedisSimpleQueueOptions
                {
                    Namespace = this.options.RedisNamespace
                });
            otherNodeQueue.Publish(message);
        }

        /// <summary>
        /// Only one tag member will see the message.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void PublishToSingleTagMember(string tag, string message)
        {
            var labelQueue = new RedisSimpleQueue(
                this.connection, this.GetTagQueueName(tag), QueueMode.Queue,
                new RedisSimpleQueueOptions
                {
                    Namespace = this.options.RedisNamespace
                });
            labelQueue.Publish(message);
        }

        /// <summary>
        /// All actively listening members of the tag wil see the message. If no nodes are actively listening, then no nodes will ever see the message.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void BroadcastToTag(string tag, string message)
        {
            var labelQueue = new RedisSimpleQueue(
                this.connection, this.GetTagFanOutQueueName(tag), QueueMode.FanOut,
                new RedisSimpleQueueOptions
                {
                    Namespace = this.options.RedisNamespace
                });
            labelQueue.Publish(message);
        }

        private string GetNodeQueueName(string nodeId)
        {
            return "s-node:" + nodeId;
        }

        private string GetTagFanOutQueueName(string tag)
        {
            return "s-node-label-fan:" + tag;
        }

        private string GetTagQueueName(string tag)
        {
            return "s-node-label:" + tag;
        }

        private string GetPropNameToUse(string propName)
        {
            return RedisKeyHelper.GetPropNameToUse(propName, this.options.RedisNamespace);            
        }


        private static readonly long StartTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
        private long GetMillisecondsSince1970()
        {
            DateTime dateTime = DateTime.UtcNow;
            return (Int64)((dateTime.Ticks - StartTicks) / 10000);
        }


        private class QueueState
        {
            public QueueState(ISimpleQueue queue, IQueueSubscription queueSubscription)
            {
                this.Queue = queue;
                this.QueueSubscription = queueSubscription;
            }

            public ISimpleQueue Queue { get; private set; }
            public IQueueSubscription QueueSubscription { get; private set; }
        }


        protected virtual string SerializeJson(object obj)
        {
            string json = FlowBasis.Json.JObject.Stringify(obj);
            return json;
        }
    }

    public class SimpleNodeForRedisOptions : SimpleNodeOptions
    {
        public SimpleNodeForRedisOptions()
        {
            this.HeartbeatInterval = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Namespace to use for scoping data properties within Redis.
        /// </summary>
        public string RedisNamespace { get; set; }        
        

        /// <summary>
        /// Amount of time to pause between heartbeats to node coordinator.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; }
    }

    
    
}
