using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.SimpleNodes.Redis.Util
{
    public static class RedisKeyHelper
    {
        public static string GetPropNameToUse(string propName, string redisNamespace)
        {
            string prefix = (redisNamespace != null) ? (redisNamespace + "--") : String.Empty;
            return prefix + propName;
        }
    }
}
