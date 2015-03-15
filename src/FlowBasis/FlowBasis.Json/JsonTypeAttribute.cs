using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class JsonTypeAttribute : Attribute
    {
        /// <summary>
        /// Type of IJObjectMapper to use for converting the type.
        /// </summary>
        public Type MapperType { get; set; }
    }

}
