using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Expressions.Extensions
{
    public class ConvertMemberProvider : IExpressionMemberProvider
    {        
        private static ToNumberExpressionCallable s_toNumber = new ToNumberExpressionCallable();
        private static ToBase64Utf8ExpressionCallable s_toBase64Utf8 = new ToBase64Utf8ExpressionCallable();
        private static FromBase64Utf8ExpressionCallable s_fromBase64Utf8 = new FromBase64Utf8ExpressionCallable();

        public object EvaluateMember(string name)
        {
            switch (name)
            {
                case "toNumber": return s_toNumber;
                case "toBase64Utf8": return s_toBase64Utf8;
                case "fromBase64Utf8": return s_fromBase64Utf8;

                default: throw new Exception($"Member does not exist: {name}");
            }
        }

        public class ToNumberExpressionCallable : IExpressionCallable
        {
            public object EvaluateCall(object[] args)
            {
                if (args.Length == 1)
                {                    
                    decimal d = Convert.ToDecimal(args[0]);
                    return d;
                }

                return null;
            }
        }


        public class ToBase64Utf8ExpressionCallable : IExpressionCallable
        {
            public object EvaluateCall(object[] args)
            {
                if (args.Length == 1)
                {
                    string strToEncode = args[0] as string;
                    if (strToEncode != null)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(strToEncode);
                        string base64Str = Convert.ToBase64String(bytes);
                        return base64Str;
                    }
                }

                return null;
            }
        }

        public class FromBase64Utf8ExpressionCallable : IExpressionCallable
        {
            public object EvaluateCall(object[] args)
            {
                if (args.Length == 1)
                {
                    string base64Str = args[0] as string;
                    if (base64Str != null)
                    {
                        byte[] bytes = Convert.FromBase64String(base64Str);
                        string encodedStr =  Encoding.UTF8.GetString(bytes);
                        return encodedStr;
                    }                    
                }

                return null;
            }
        }
    }
}
