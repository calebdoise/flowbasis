using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{    

    public interface IExpressionCallable
    {        
        object EvaluateCall(object[] args);
    }


    public class ExpressionCallable : IExpressionCallable
    {
        private Func<object[], object> func;

        public ExpressionCallable(Func<object[], object> func)
        {            
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            this.func = func;
        }

        public virtual object EvaluateCall(object[] args)
        {
            return this.func(args);
        }
    }
    
}
