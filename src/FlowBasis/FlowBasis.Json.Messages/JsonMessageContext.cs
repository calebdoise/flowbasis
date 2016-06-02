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

        public JsonMessageContext(List<JsonMessageHeader> headers, JObject body)
        {
            this.Headers = headers;
            this.Body = body;

            this.action = this.Headers?.FirstOrDefault(h => h.Name == "action")?.Value;
        }

        public List<JsonMessageHeader> Headers { get; private set; }

        public JObject Body { get; private set; }

        
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
