using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.SimpleNodes
{
    public class SimpleNodeOptions
    {
        /// <summary>
        /// Optional: Can be set to fixed value for single-instance nodes with well-known ids. If not set, a dynamic id will be created.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Profile of node that will be registered with central management repository for describing the node. This should be a JSON serializable object.
        /// </summary>
        public object Profile { get; set; }

        /// <summary>
        /// If true, the node is assumed to be stable with a consistent id and will restart with same id if it is shutdown.
        /// </summary>
        public bool IsPersistent { get; set; }

        /// <summary>
        /// Optional: Node can be labeled with specific tags. Messages can be broadcast to active nodes matching a particular tag.
        /// </summary>
        public List<string> Tags { get; set; }


        /// <summary>
        /// NodeMessageCallback is called for any messages directly send to the node id.
        /// </summary>
        public NodeMessageCallback NodeMessageCallback { get; set; }
        /// <summary>
        /// TagMessageCallback is called for any messages sent to one of the tags to which the node belongs.
        /// </summary>
        public TagMessageCallback TagMessageCallback { get; set; }
        /// <summary>
        /// TagFanOutMessageCallback is called for any messages broadcast to all nodes with a particular tag.
        /// </summary>
        public TagMessageCallback TagFanOutMessageCallback { get; set; }


        /// <summary>
        /// Optional: Custom logger which will be handed logging messages.
        /// </summary>
        public SimpleNodeLogCallback Logger { get; set; }
    }


    public delegate void NodeMessageCallback(string message);

    public delegate void TagMessageCallback(string tag, string message);

    public delegate void SimpleNodeLogCallback(string message, SimpleNodeLogLevel level);
}
