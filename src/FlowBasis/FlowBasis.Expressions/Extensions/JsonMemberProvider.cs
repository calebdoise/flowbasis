using FlowBasis.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions.Extensions
{
    public class JsonMemberProvider : IExpressionMemberProvider
    {
        private static JsonParseExpressionCallable s_jsonParse = new JsonParseExpressionCallable();
        private static JsonStringifyExpressionCallable s_jsonStringify = new JsonStringifyExpressionCallable();
        private static JsonMergeExpressionCallable s_jsonMerge = new JsonMergeExpressionCallable();
        private static JsonFieldExpressionCallable s_jsonField = new JsonFieldExpressionCallable();

        public object EvaluateMember(string name)
        {
            switch (name)
            {
                case "parse": return s_jsonParse;
                case "stringify": return s_jsonStringify;
                case "merge": return s_jsonMerge;
                case "field": return s_jsonField;

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

        public class JsonMergeExpressionCallable : IExpressionCallable
        {
            public object EvaluateCall(object[] args)
            {
                var mergedObject = new JObject();

                foreach (object arg in args)
                { 
                    if (arg is IDictionary<string, object> dictionary)
                    {
                        foreach (var pair in dictionary)
                        {
                            mergedObject[pair.Key] = pair.Value;
                        }
                    }
                }

                return mergedObject;
            }
        }
        
        public class JsonFieldExpressionCallable : IExpressionCallable
        {
            public object EvaluateCall(object[] args)
            {
                if (args.Length > 0)
                {
                    string fieldName = args[0]?.ToString();
                    if (fieldName != null)
                    {
                        object value = null;
                        if (args.Length > 1)
                        {
                            value = args[1];
                        }

                        var jObject = new JObject();
                        jObject[fieldName] = value;

                        return jObject;
                    }
                }

                return null;
            }
        }
    }
}
