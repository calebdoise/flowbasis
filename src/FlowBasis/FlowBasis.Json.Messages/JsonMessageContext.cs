using FlowBasis.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Messages
{
    public class JsonMessageContext
    {
        private string action;

        public JsonMessageContext(string action, object body) 
            : this(
                  new List<JsonMessageHeader> { new JsonMessageHeader { Name = "action", Value = action } },
                  body
                  )
        {
        }

        public JsonMessageContext(List<JsonMessageHeader> headers, object body)
        {
            this.Headers = headers;
            this.Body = body;

            this.action = this.Headers?.FirstOrDefault(h => h.Name == "action")?.Value;
        }

        public List<JsonMessageHeader> Headers { get; private set; }

        public object Body { get; private set; }

        
        public string Action
        {
            get { return this.action; }
        }

        public string GetHeaderValue(string headerName)
        {
            return this.Headers?.FirstOrDefault(h => h.Name == headerName)?.Value;
        }
    }
    
}
