using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{
    public enum JsepNodeType
    {
        Compound,
        Identifier,
        MemberExpression,
        Literal,
        ThisExpression,
        CallExpression,
        UnaryExpression,
        BinaryExpression,
        LogicalExpression,
        ConditionalExpression,
        ArrayExpression
    }
}
