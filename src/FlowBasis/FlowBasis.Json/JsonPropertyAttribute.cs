using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JsonPropertyAttribute : Attribute
    {
        public string Name { get; set; }

        /// <summary>
        /// Type of IJObjectMapper to use for converting the property.
        /// </summary>
        public Type MapperType { get; set; }
    }
}
