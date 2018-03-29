using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.SimpleNode
{

    /// <summary>
    /// JSON serializable object that will be used as the node descriptor in central registry.
    /// </summary>
    public class SimpleNodeDescriptor
    {
        public string Id { get; set; }

        /// <summary>
        /// Profile must be a JSON serializable object that describes the capabilities that the node wishes to advertise.
        /// </summary>
        public object Profile { get; set; }

        /// <summary>
        /// Milliseconds since 1970 at which the node was started.
        /// </summary>
        public long? StartUtcTimestamp { get; set; }

        /// <summary>
        /// If true, the node is assumed to be stable with a consistent id and will restart with same id if it is shutdown.
        /// </summary>
        public bool IsPersistent { get; set; }
    }

}
