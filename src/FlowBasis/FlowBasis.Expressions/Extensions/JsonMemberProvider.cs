using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions.Extensions
{
    public class JsonMemberProvider : IExpressionMemberProvider
    {
        private static JsonParseExpressionCallable s_jsonParse = new JsonParseExpressionCallable();
        private static JsonStringifyExpressionCallable s_jsonStringify = new JsonStringifyExpressionCallable();


        public object EvaluateMember(string name)
        {
            switch (name)
            {
                case "parse": return s_jsonParse;
                case "stringify": return s_jsonStringify;

                default: throw new Exception($"Member does not exist: {name}");
            }
        }


        public class JsonParseExpressionCallable : IExpressionCallable
        {
            public object EvaluateCall(object[] args)
            {
                if (args.Length == 1)
                {
                    string json = args[0] as string;
                    if (json != null)
                    {
                        object result = FlowBasis.Json.JsonSerializers.Default.Parse(json);
                        return result;
                    }
                }

                return null;
            }
        }

        public class JsonStringifyExpressionCallable : IExpressionCallable
        {
            public object EvaluateCall(object[] args)
            {
                if (args.Length == 1)
                {
                    object arg = args[0];
                    if (arg != null)
                    {
                        string json = FlowBasis.Json.JsonSerializers.Default.Stringify(args[0]);
                        return json;
                    }
                }

                return null;
            }
        }
    }
}
