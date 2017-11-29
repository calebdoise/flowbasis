using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{
    public class JsepNode
    {
        public JsepNodeType Type { get; set; }
        public string Name { get; set; }
        public int? Prec { get; set; }
        public object Value { get; set; }
        public object Raw { get; set; }

        /// <summary>
        /// Body consists of the list of nodes for a compound node.
        /// </summary>
        public List<JsepNode> Body { get; set; }

        public JsepNode Argument { get; set; }
        public List<JsepNode> Arguments { get; set; }
        public List<JsepNode> Elements { get; set; }
        public JsepNode Callee { get; set; }
        public bool? Prefix { get; set; }
        public string Operator { get; set; }
        public JsepNode Left { get; set; }
        public JsepNode Right { get; set; }

        /// <summary>
        /// For a conditional expression (i.e. "3 > 4 ? 5 : 6"), this is the test to perform.
        /// </summary>
        public JsepNode Test { get; set; }
        /// <summary>
        /// For a conditional expression (i.e. "3 > 4 ? 5 : 6"), this is the value to use if the test comes back true.
        /// </summary>
        public JsepNode Consequent { get; set; }
        /// <summary>
        /// For an alternate expression (i.e. "3 > 4 ? 5 : 6"), this is the value to use if the test comes back false.
        /// </summary>
        public JsepNode Alternate { get; set; }

        public bool? Computed { get; set; }

        public JsepNode Object { get; set; }
        public JsepNode Property { get; set; }
    }
}
