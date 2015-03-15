using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowBasis.Json
{
    /// <summary>
    /// Place JsonIgnoreAttribute on a property to prevent it from being serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class JsonIgnoreAttribute : Attribute
    {
    }
}
