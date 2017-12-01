using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{
    public class ExpressionScope
    {
        public virtual object EvaluateThis()
        {
            return null;
        }

        public virtual object EvaluateIdentifier(string name)
        {
            return null;
        }         
    }

    public class StandardExpressionScope : ExpressionScope
    {
        private object thisObject;
        private Func<string, object> identifierResolver;

        public StandardExpressionScope(
            object thisObject,
            Func<string, object> identifierResolver)
        {
            this.thisObject = thisObject;
            this.identifierResolver = identifierResolver;
        }

        public override object EvaluateThis()
        {
            return this.thisObject;
        }

        public override object EvaluateIdentifier(string name)
        {
            var value = this.identifierResolver(name);
            return value;
        }        
    }
    
}
