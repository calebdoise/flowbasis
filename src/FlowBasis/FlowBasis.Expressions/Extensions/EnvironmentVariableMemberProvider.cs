using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions.Extensions
{
    public class EnvironmentVariableMemberProvider : IExpressionMemberProvider
    {
        public object EvaluateMember(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return value;
        }

    }
}
