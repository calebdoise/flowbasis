using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions
{
    public interface IExpressionMemberProvider
    {
        object EvaluateMember(string name);
    }

    public class ExpressionMemberProvider : IExpressionMemberProvider
    {
        private Func<string, object> nameToMemberMap;

        public ExpressionMemberProvider(Func<string, object> nameToMemberMap)
        {
            this.nameToMemberMap = nameToMemberMap;
        }

        public object EvaluateMember(string name)
        {
            object value = this.nameToMemberMap(name);
            return value;
        }
    }

}
