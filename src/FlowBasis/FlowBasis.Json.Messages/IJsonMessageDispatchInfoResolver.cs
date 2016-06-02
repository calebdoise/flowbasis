using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Messages
{
    public interface IJsonMessageDispatchInfoResolver
    {
        IJsonMessageDispatcher GetDispatcher(JsonMessageContext messageContext);
    }
}
