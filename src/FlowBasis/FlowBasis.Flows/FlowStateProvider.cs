using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Flows
{
    public abstract class FlowStateProvider
    {
        public virtual string GetNewFlowStateId(Dictionary<string, string> fixedProperties)
        {
            return Guid.NewGuid().ToString("N");
        }

        public abstract FlowStateHandle CreateFlowState(NewFlowStateOptions options);

        public abstract FlowStateHandle GetFlowState(
            string flowStateId,
            OpenFlowStateOptions options = null);
    }

    public class NewFlowStateOptions
    {
        public Dictionary<string, string> FixedProperties { get; set; }

        public ProgressState ProgressState { get; set; }

        /// <summary>
        /// Only one of State and StateJson should be set.
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// Only one of State and StateJson should be set.
        /// </summary>
        public string StateJson { get; set; }
        
        public DateTime? ExpiresAtUtc { get; set; }

        /// <summary>
        /// Should the flow state handle be initially created in the locked state.
        /// </summary>
        public bool Lock { get; set; }

        /// <summary>
        /// If locked, how long should the state be locked.
        /// </summary>
        public TimeSpan? LockDuration { get; set; }
    }


    public class OpenFlowStateOptions
    {
        public bool Lock { get; set; }

        /// <summary>
        /// If locked, how long should the state be locked.
        /// </summary>
        public TimeSpan? LockDuration { get; set; }
    }
    
}
