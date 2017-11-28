using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Flows
{

    /// <summary>
    /// ProgressState defines a set of data that can be used to report processing status
    /// of a flow. This will typically be a much smaller amount of data than the full state.
    /// </summary>
    public sealed class ProgressState
    {
        /// <summary>
        /// This indicates a number of items processed or portion of Total.
        /// </summary>
        public long? Current { get; set; }

        /// <summary>
        /// If known, total is the total number of items being processed, or it could
        /// represent an abstract total such as 100%.
        /// </summary>
        public long? Total { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// A status flag specific to the flow state to indicate its status. 
        /// By convention, error statuses should be represented by negative numbers.
        /// </summary>
        public int? StatusFlag { get; set; }
    }

}
