using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Json.Messages
{
    public class JsonMessageDispatchInfoResolver : IJsonMessageDispatchInfoResolver
    {
        private Dictionary<string, JsonMessageDispatcher> actionToHandlerMap = new Dictionary<string, JsonMessageDispatcher>();
        private IServiceProvider serviceProvider;

        public JsonMessageDispatchInfoResolver(IServiceProvider serviceProvider = null)
        {
            this.serviceProvider = serviceProvider;
        }

        public void RegisterDispatchControllerTypePublicMethods(Type dispatchControllerType)
        {
            string actionPrefix = dispatchControllerType.Name;
            if (actionPrefix.EndsWith("MessageDispatcher"))
            {
                actionPrefix = actionPrefix.Substring(0, actionPrefix.Length - "MessageDispatcher".Length);
            }
            else if (actionPrefix.EndsWith("Dispatcher"))
            {
                actionPrefix = actionPrefix.Substring(0, actionPrefix.Length - "Dispatcher".Length);
            }

            MethodInfo[] methods = dispatchControllerType.GetMethods(BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public);
            foreach (MethodInfo method in methods)
            {
                bool includeMethod = true;

                if (method.Name == "ToString" || method.Name == "GetHashCode")
                {
                    includeMethod = false;
                }

                if (includeMethod == true)
                {
                    string action = actionPrefix + "/" + method.Name;

                    var dispatchInfo = new JsonMessageDispatchInfo()
                    {
                        DispatchControllerType = dispatchControllerType,
                        DispatchMethod = method                        
                    };

                    this.RegisterDispatcher(action, dispatchInfo);
                }
            }
        }

        public void RegisterDispatcher(string action, JsonMessageDispatchInfo dispatchInfo)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (dispatchInfo == null)
                throw new ArgumentNullException(nameof(dispatchInfo));

            lock (this)
            {
                if (this.actionToHandlerMap.ContainsKey(action))
                {
                    throw new ArgumentException($"action already registered: {action}");
                }

                var dispatcher = new JsonMessageDispatcher(dispatchInfo, this.serviceProvider);
                this.actionToHandlerMap[action] = dispatcher;
            }
        }

        public IJsonMessageDispatcher GetDispatcher(JsonMessageContext messageContext)
        {
            string action = messageContext.Action;

            JsonMessageDispatcher dispatcher;
            if (this.actionToHandlerMap.TryGetValue(action, out dispatcher))
            {
                return dispatcher;
            }

            throw new Exception($"Dispatcher not found for action: {action}");
        }
    }


    public class JsonMessageDispatchInfo
    {
        // If set, this should be called directly with the JsonMessageContext.
        public Action<JsonMessageContext> DispatchCallback { get; set; }

        public Type DispatchControllerType { get; set; }
        public MethodInfo DispatchMethod { get; set; }
    }
}
