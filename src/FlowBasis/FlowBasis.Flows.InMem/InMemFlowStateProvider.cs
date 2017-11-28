using FlowBasis.Flows;
using FlowBasis.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Flows.InMem
{
    public class InMemFlowStateProvider : FlowStateProvider
    {
        private object syncObject = new Object();
        private Dictionary<string, InMemFlowStateData> idToFlowStateMap = new Dictionary<string, InMemFlowStateData>();


        public override FlowStateHandle CreateFlowState(NewFlowStateOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            DateTime utcNow = DateTime.UtcNow;

            var stateData = new InMemFlowStateData
            {
                Id = Guid.NewGuid().ToString("N"),
                ExpiresAtUtc = options.ExpiresAtUtc
            };

            stateData.FixedPropertiesJson = this.ToJson(options.FixedProperties);
            stateData.ProgressStateJson = this.ToJson(options.ProgressState);

            if (options.State != null)
            {
                stateData.StateJson = this.ToJson(options.State);
            }
            else
            {
                stateData.StateJson = options.StateJson;
            }

            string handleLockCode = null;
            if (options.Lock)
            {
                handleLockCode = Guid.NewGuid().ToString("N");
                stateData.LockCode = handleLockCode;

                if (options?.LockDuration != null)
                {
                    stateData.LockExpiresAtUtc = utcNow.Add(options.LockDuration.Value);
                }
                else
                {
                    stateData.LockExpiresAtUtc = null;
                }
            }

            InMemFlowStateData stateDataCopy;
            lock (this.syncObject)
            {
                this.idToFlowStateMap[stateData.Id] = stateData;
                stateDataCopy = stateData.Clone();
            }
            
            stateDataCopy.LockCode = handleLockCode;
            var handle = new InMemFlowStateHandle(this, stateDataCopy);            

            return handle;
        }


        public override FlowStateHandle GetFlowState(string flowStateId, OpenFlowStateOptions options = null)
        {
            lock (this.syncObject)
            {
                InMemFlowStateData stateData;
                if (this.idToFlowStateMap.TryGetValue(flowStateId, out stateData))
                {
                    DateTime utcNow = DateTime.UtcNow;

                    if (stateData.ExpiresAtUtc.HasValue)
                    {
                        if (utcNow > stateData.ExpiresAtUtc.Value)
                        {
                            // Expired so we remove it and treat it as though it did not exist.
                            this.idToFlowStateMap.Remove(flowStateId);

                            throw new Exception("Flow state not found for id: " + flowStateId);
                        }
                    }

                    string handleLockCode = null;

                    if (options?.Lock == true)
                    {
                        if (stateData.LockCode != null)
                        {
                            if (stateData.LockExpiresAtUtc != null && stateData.LockExpiresAtUtc.Value < utcNow)
                            {
                                // Other consumers lock expired.
                                stateData.LockCode = null;
                                stateData.LockExpiresAtUtc = null;
                            }
                            else
                            {
                                throw new Exception("Flow state is already locked elsewhere: " + stateData.Id);
                            }
                        }

                        handleLockCode = Guid.NewGuid().ToString("N");
                        stateData.LockCode = handleLockCode;
                                                             
                        if (options?.LockDuration != null)
                        {
                            stateData.LockExpiresAtUtc = utcNow.Add(options.LockDuration.Value);
                        }
                        else
                        {
                            stateData.LockExpiresAtUtc = null;
                        }
                    }

                    var stateDataCopy = stateData.Clone();
                    stateData.LockCode = handleLockCode;

                    var handle = new InMemFlowStateHandle(this, stateDataCopy);
                    return handle;
                }
                else
                {
                    throw new Exception("Flow state not found for id: " + flowStateId);
                }
            }
        }


        private string ToJson(object value)
        {
            if (value == null)
            {
                return null;
            }

            return FlowBasis.Json.JsonSerializers.Default.Stringify(value);
        }

        private T FromJson<T>(string value)
        {
            if (value == null)
            {
                return default(T);
            }

            if (typeof(T) == typeof(Dictionary<string, string>))
            {
                var result = new Dictionary<string, string>();

                // TODO: Support Dictionary<> type serialization/deserialization.
                var jObject = FlowBasis.Json.JsonSerializers.Default.Parse<object>(value) as JObject;
                if (jObject != null)
                {
                    foreach (var pair in (IDictionary<string, object>)jObject)
                    {
                        string key = pair.Key;
                        string strValue = pair.Value?.ToString();

                        result[key] = strValue;
                    }
                }

                return (T)((object)result);
            }
            else
            {
                return FlowBasis.Json.JsonSerializers.Default.Parse<T>(value);
            }
        }


        private class InMemFlowStateHandle : FlowStateHandle
        {
            private InMemFlowStateProvider stateProvider;
            private InMemFlowStateData flowStateData;

            private IDictionary<string, string> fixedProperties;

            public InMemFlowStateHandle(InMemFlowStateProvider stateProvider, InMemFlowStateData flowStateData)
            {
                this.stateProvider = stateProvider;
                this.flowStateData = flowStateData;
            }

            public override void Dispose()
            {
                if (this.flowStateData.LockCode != null)
                {
                    this.Update(new UpdateFlowStateOptions
                    {
                        UpdateLockCommand = UpdateLockCommand.ReleaseLock
                    });
                }
            }

            public override string Id
            {
                get
                {
                    return this.flowStateData.Id;
                }
            }

            public override IDictionary<string, string> FixedProperties
            {
                get
                {
                    if (this.fixedProperties == null)
                    {
                        this.fixedProperties = this.stateProvider.FromJson<Dictionary<string, string>>(this.flowStateData.FixedPropertiesJson);
                        if (this.fixedProperties == null)
                        {
                            this.fixedProperties = new Dictionary<string, string>();
                        }
                    }

                    return this.fixedProperties;
                }
            }

            public override ProgressState ProgressState
            {
                get
                {
                    return this.stateProvider.FromJson<ProgressState>(this.flowStateData.ProgressStateJson);
                }
            }

            public override string StateJson
            {
                get
                {
                    return this.flowStateData.StateJson;
                }
            }

            public override T GetState<T>()
            {
                return this.stateProvider.FromJson<T>(this.flowStateData.StateJson);
            }

            public override DateTime? ExpiresAtUtc
            {
                get
                {
                    return this.flowStateData.ExpiresAtUtc.Value;
                }
            }

            public override void Update(UpdateFlowStateOptions options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                lock (this.stateProvider.syncObject)
                {
                    InMemFlowStateData stateData;
                    if (this.stateProvider.idToFlowStateMap.TryGetValue(this.flowStateData.Id, out stateData))
                    {
                        DateTime utcNow = DateTime.UtcNow;

                        if (stateData.ExpiresAtUtc.HasValue)
                        {
                            if (utcNow > stateData.ExpiresAtUtc.Value)
                            {
                                // Expired so we remove it and treat it as though it did not exist.
                                this.stateProvider.idToFlowStateMap.Remove(this.flowStateData.Id);

                                throw new Exception("Flow state not found for id: " + this.flowStateData.Id);
                            }
                        }        

                        if (stateData.LockCode != null && stateData.LockCode != this.flowStateData.LockCode)
                        {
                            if (stateData.LockExpiresAtUtc != null && stateData.LockExpiresAtUtc.Value < utcNow)
                            {
                                // Other consumers lock expired.
                                stateData.LockCode = null;
                                stateData.LockExpiresAtUtc = null;                                
                            }
                            else
                            {
                                throw new Exception("Flow state is already locked elsewhere: " + this.flowStateData.Id);
                            }
                        }
                        
                        if (options.HasNewState)
                        {
                            if (stateData.StateVersion != this.flowStateData.StateVersion)
                            {
                                throw new Exception("Flow state version on server does not match client view.");
                            }

                            string newStateJson = this.stateProvider.ToJson(options.NewState);
                            this.flowStateData.StateJson = newStateJson;
                            stateData.StateJson = newStateJson;

                            stateData.StateVersion++;
                            this.flowStateData.StateVersion = stateData.StateVersion;
                        }                

                        if (options.HasNewProgressState)
                        {
                            if (stateData.ProgressStateVersion != this.flowStateData.ProgressStateVersion)
                            {
                                throw new Exception("Flow state version on server does not match client view.");
                            }

                            string newProgressStateJson = this.stateProvider.ToJson(options.NewProgressState);
                            this.flowStateData.ProgressStateJson = newProgressStateJson;
                            stateData.ProgressStateJson = newProgressStateJson;

                            stateData.ProgressStateVersion++;
                            this.flowStateData.ProgressStateVersion = stateData.ProgressStateVersion;
                        }
 
                        if (options.UpdateLockCommand != null)
                        {
                            if (options.UpdateLockCommand == UpdateLockCommand.ReleaseLock)
                            {
                                stateData.LockCode = null;
                                stateData.LockExpiresAtUtc = null;
                                this.flowStateData.LockCode = null;
                                this.flowStateData.LockExpiresAtUtc = null;
                            }
                            else if (options.UpdateLockCommand == UpdateLockCommand.AcquireOrExtendLock)
                            {
                                DateTime? newLockExpiresAtUtc = options.NewLockDuration.HasValue ? utcNow.Add(options.NewLockDuration.Value) : (DateTime?)null;
                                stateData.LockExpiresAtUtc = newLockExpiresAtUtc;
                                this.flowStateData.LockExpiresAtUtc = newLockExpiresAtUtc;
                            }
                        }

                        if (options.HasNewExpiresAtUtc)
                        {
                            stateData.ExpiresAtUtc = options.NewExpiresAtUtc;
                            this.flowStateData.ExpiresAtUtc = options.NewExpiresAtUtc;
                        }                        
                    }
                }
            }


            public override void Delete()
            {
                lock (this.stateProvider.syncObject)
                {
                    if (this.stateProvider.idToFlowStateMap.ContainsKey(this.flowStateData.Id))
                    {
                        this.stateProvider.idToFlowStateMap.Remove(this.flowStateData.Id);
                    }

                    // If we delete it, then we clear LockCode so that we don't try to release lock in Dispose method.
                    this.flowStateData.LockCode = null;
                }
            }
        }
    }


    internal class InMemFlowStateData
    {
        public string Id { get; set; }

        public string FixedPropertiesJson { get; set; }

        public string ProgressStateJson { get; set; }
        public string StateJson { get; set; }

        public long ProgressStateVersion { get; set; }
        public long StateVersion { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public string LockCode { get; set; }
        public DateTime? LockExpiresAtUtc { get; set; }


        public InMemFlowStateData Clone()
        {
            return new InMemFlowStateData
            {
                Id = this.Id,
                FixedPropertiesJson = this.FixedPropertiesJson,
                ProgressStateJson = this.ProgressStateJson,
                StateJson = this.StateJson,
                ExpiresAtUtc = this.ExpiresAtUtc,
                LockCode = this.LockCode,
                LockExpiresAtUtc = this.LockExpiresAtUtc
            };
        }
    }
}
