using FlowBasis.SimpleNodes.Redis.Util;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.SimpleNodes.Redis
{
    public class SimpleNodeInspectorForRedis
    {
        private ConnectionMultiplexer connection;
        private IDatabase db;
        private string redisNamespace;

        public SimpleNodeInspectorForRedis(ConnectionMultiplexer connection, string redisNamespace)
        {
            this.redisNamespace = redisNamespace;

            this.connection = connection;
            this.db = this.connection.GetDatabase();
        }

        public List<string> GetAllRegisteredNodeIds()
        {
            var nodeIdList = new List<string>();

            foreach (var id in this.db.SetScan(SimpleNodeForRedis.SNodesKey, "*", 1000))
            {
                nodeIdList.Add(id);
            }

            return nodeIdList;
        }

        public SimpleNodeDescriptor TryGetSimpleNodeDescriptor(string nodeId)
        {
            string descriptorJson = this.db.HashGet(RedisKeyHelper.GetPropNameToUse(SimpleNodeForRedis.SNodeDescriptorHashKey, this.redisNamespace), nodeId);
            if (descriptorJson != null)
            {
                return FlowBasis.Json.JsonSerializers.Default.Parse<SimpleNodeDescriptor>(descriptorJson);
            }

            return null;
        }

        public long? TryGetNodeLastHeartbeatUtcTimestamp(string nodeId)
        {
            string timestampStr = this.db.HashGet(RedisKeyHelper.GetPropNameToUse(SimpleNodeForRedis.SNodeLifeHashKey, this.redisNamespace), nodeId);
            if (timestampStr != null)
            {
                return Convert.ToInt64(timestampStr);
            }

            return null;
        }


        public void TryCleanupStateForNode(string nodeId)
        {
            // Delete heartbeat entry.
            this.db.HashDelete(RedisKeyHelper.GetPropNameToUse(SimpleNodeForRedis.SNodeLifeHashKey, this.redisNamespace), nodeId);

            // Delete descriptor.
            this.db.HashDelete(RedisKeyHelper.GetPropNameToUse(SimpleNodeForRedis.SNodeDescriptorHashKey, this.redisNamespace), nodeId);

            // Remove from overall node set.
            this.db.SetRemove(RedisKeyHelper.GetPropNameToUse(SimpleNodeForRedis.SNodesKey, this.redisNamespace), nodeId);
        }
        
    }
}
