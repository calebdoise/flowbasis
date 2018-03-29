using FlowBasis.Json;
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
        private const string SNodesKey = "s-nodes";
        private const string SNodeDescriptorHashKey = "s-nodes-desc";
        private const string SNodeLifeHashKey = "s-nodes-life";

        private SimpleNodeForRedisOptions options;

        private ConnectionMultiplexer connection;
        private IDatabase db;
        private string id;

        private bool startHasBeenCalled = false;
        private bool stopHasBeenCalled = false;
        private CancellationTokenSource stopCancellationTokenSource = new CancellationTokenSource();

        private QueueState nodeQueueState;

        private Dictionary<string, QueueState> labelFanOutListenerMap;
        private Dictionary<string, QueueState> labelListenerMap;

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

                if (this.options.Labels != null)
                {
                    this.options.Logger($"SimpleNode labels for {this.id}: {String.Join(", ", this.options.Labels)}", SimpleNodeLogLevel.Information);
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
                    StartUtcTimestamp = FlowBasis.Json.Util.TimeHelper.ToEpochMilliseconds(DateTime.UtcNow)
                };
                string descriptorJson = this.SerializeJson(descriptor);
                this.db.HashSet(this.GetPropNameToUse(SNodeDescriptorHashKey), this.id, descriptorJson);

                var nodeQueue = new RedisSimpleQueue(
                    this.connection, this.GetNodeQueueName(this.id), QueueMode.Queue,
                    new RedisSimpleQueueOptions
                    {
                        Namespace = this.options.Namespace
                    });
                var nodeQueueSubscription = nodeQueue.Subscribe(this.NodeMessageCallback);

                this.nodeQueueState = new QueueState(nodeQueue, nodeQueueSubscription);

                this.labelFanOutListenerMap = new Dictionary<string, QueueState>();
                this.labelListenerMap = new Dictionary<string, QueueState>();
                if (this.options.Labels != null)
                {
                    foreach (string label in this.options.Labels)
                    {
                        var labelFanOutQueue = new RedisSimpleQueue(
                            this.connection, this.GetLabelFanOutQueueName(label), QueueMode.FanOut,
                            new RedisSimpleQueueOptions
                            {
                                Namespace = this.options.Namespace
                            });
                        var labelFanOutQueueSubscription = labelFanOutQueue.Subscribe((message) => this.LabelFanOutMessageCallback(label, message));

                        this.labelFanOutListenerMap[label] = new QueueState(labelFanOutQueue, labelFanOutQueueSubscription);

                        var labelQueue = new RedisSimpleQueue(
                            this.connection, this.GetLabelQueueName(label), QueueMode.Queue,
                            new RedisSimpleQueueOptions
                            {
                                Namespace = this.options.Namespace
                            });
                        var labelQueueSubscription = labelQueue.Subscribe((message) => this.LabelMessageCallback(label, message));

                        this.labelListenerMap[label] = new QueueState(labelQueue, labelQueueSubscription);
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

        private void DeleteHeartbeat()
        {
            this.db.HashDelete(this.GetPropNameToUse(SNodeLifeHashKey), this.id);
        }


        public void Stop()
        {
            if (this.startHasBeenCalled && !this.stopHasBeenCalled)
            {
                this.stopHasBeenCalled = true;
                this.stopCancellationTokenSource.Cancel();

                if (this.nodeQueueState != null)
                {
                    this.nodeQueueState.QueueSubscription.Unsubscribe();
                    this.nodeQueueState = null;
                }

                foreach (var queueState in this.labelFanOutListenerMap)
                {
                    queueState.Value.QueueSubscription.Unsubscribe();
                }

                foreach (var queueState in this.labelListenerMap)
                {
                    queueState.Value.QueueSubscription.Unsubscribe();
                }

                // TODO: Optionally cleanup queues specific to this node (it's probably better to do this separately; be sure to consider nodes that use stable ids).

                this.DeleteHeartbeat();
                this.db.HashDelete(this.GetPropNameToUse(SNodeDescriptorHashKey), this.id);
                this.db.SetRemove(this.GetPropNameToUse(SNodesKey), this.id);                
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

        private void LabelMessageCallback(string label, string message)
        {
            if (this.options.Logger != null)
            {
                this.options.Logger($"Received Label Message ({label}): {message}", SimpleNodeLogLevel.Verbose);
            }

            if (this.options.LabelMessageCallback != null)
            {
                this.options.LabelMessageCallback(label, message);
            }
        }

        private void LabelFanOutMessageCallback(string label, string message)
        {
            if (this.options.Logger != null)
            {
                this.options.Logger($"Received Label Fan-Out Message ({label}): {message}", SimpleNodeLogLevel.Verbose);
            }

            if (this.options.LabelFanOutMessageCallback != null)
            {
                this.options.LabelFanOutMessageCallback(label, message);
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
                    Namespace = this.options.Namespace
                });
            otherNodeQueue.Publish(message);
        }

        /// <summary>
        /// Only one label member will see the message.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="message"></param>
        public void PublishToSingleLabelMember(string label, string message)
        {
            var labelQueue = new RedisSimpleQueue(
                this.connection, this.GetLabelQueueName(label), QueueMode.Queue,
                new RedisSimpleQueueOptions
                {
                    Namespace = this.options.Namespace
                });
            labelQueue.Publish(message);
        }

        /// <summary>
        /// All actively listening members of the label wil see the message. If no nodes are actively listening, then no nodes will ever see the message.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="message"></param>
        public void BroadcastToLabel(string label, string message)
        {
            var labelQueue = new RedisSimpleQueue(
                this.connection, this.GetLabelFanOutQueueName(label), QueueMode.FanOut,
                new RedisSimpleQueueOptions
                {
                    Namespace = this.options.Namespace
                });
            labelQueue.Publish(message);
        }

        private string GetNodeQueueName(string nodeId)
        {
            return "s-node:" + nodeId;
        }

        private string GetLabelFanOutQueueName(string label)
        {
            return "s-node-label-fan:" + label;
        }

        private string GetLabelQueueName(string label)
        {
            return "s-node-label:" + label;
        }

        private string GetPropNameToUse(string propName)
        {
            string prefix = (this.options.Namespace != null) ? (this.options.Namespace + "--") : String.Empty;
            return prefix + propName;
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
        public string Namespace { get; set; }        
        

        /// <summary>
        /// Amount of time to pause between heartbeats to node coordinator.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; }
    }

    
    
}
