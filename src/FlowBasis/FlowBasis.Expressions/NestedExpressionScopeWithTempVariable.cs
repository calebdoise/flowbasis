using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{
    public class NestedExpressionScopeWithTempVariable : ExpressionScope
    {
        private ExpressionScope scope;

        private TempDictionaryMemberEvaluator temp;

        public NestedExpressionScopeWithTempVariable(ExpressionScope scope)
        {
            this.scope = scope;
        }

        public override object EvaluateThis()
        {
            return this.scope.EvaluateThis();
        }

        public override object EvaluateIdentifier(string name)
        {
            if (name == "temp")
            {
                if (this.temp == null)
                {
                    this.temp = new TempDictionaryMemberEvaluator();
                }

                return this.temp;
            }

            return this.scope.EvaluateIdentifier(name);
        }


        private class TempDictionaryMemberEvaluator : IExpressionMemberProvider
        {
            private IDictionary<string, object> dictionary;

            public TempDictionaryMemberEvaluator()
            {
                this.dictionary = new Dictionary<string, object>();
            }            

            public object EvaluateMember(string name)
            {
                if (name == "setValue")
                {
                    return new SetExpressionCallable(this);
                }

                if (this.dictionary.TryGetValue(name, out object value))
                {
                    return value;
                }

                return value;
            }


            private class SetExpressionCallable : IExpressionCallable
            {
                private TempDictionaryMemberEvaluator tempHolder;

                public SetExpressionCallable(TempDictionaryMemberEvaluator tempHolder)
                {
                    this.tempHolder = tempHolder;
                }

                public object EvaluateCall(object[] args)
                {
                    if (args.Length == 2)
                    {
                        string name = args[0] as string;
                        object value = args[1];

                        if (name != null)
                        {
                            this.tempHolder.dictionary[name] = value;
                            return value;
                        }
                    }

                    return null;
                }
            }
        }
    }
}
