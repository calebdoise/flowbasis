using FlowBasis.Json;
using FlowBasis.Json.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasisJsonUnitTests.Json.Messages
{
    [TestClass]
    public class JsonMessageDispatcherTests
    {
 
        [TestMethod]
        public void Test_Method_Registration_And_Invocation()
        {
            var dispatchResolver = new JsonMessageDispatchInfoResolver();
            dispatchResolver.RegisterDispatchControllerTypePublicMethods(typeof(TestMessageDispatcher));

            var messageContext = new JsonMessageContext(
                new List<JsonMessageHeader>
                {
                    new JsonMessageHeader { Name = "action", Value = "Test/DoSomethingWithMessageContext" }
                },
                (JObject)JsonSerializers.Default.Parse("{\"someInt\":4, \"someStr\": \"foo1\"}"));

            var dispatcher = dispatchResolver.GetDispatcher(messageContext);

            // Test with JsonMessageContext parameter.
            TestMessageDispatcher.ClearValues();
            dispatcher.Dispatch(messageContext);
            Assert.AreEqual("Called with message context: true", TestMessageDispatcher.SomeStringValue);
            Assert.AreEqual(4, TestMessageDispatcher.SomeIntValue);

            // Test with Json            
            messageContext = new JsonMessageContext(
                new List<JsonMessageHeader>
                {
                    new JsonMessageHeader { Name = "action", Value = "Test/DoSomethingWithMessageBody" }
                },
                (JObject)JsonSerializers.Default.Parse("{\"someInt\":5, \"someStr\": \"foo2\"}"));

            dispatcher = dispatchResolver.GetDispatcher(messageContext);

            TestMessageDispatcher.ClearValues();
            dispatcher.Dispatch(messageContext);
            Assert.AreEqual("Called with message body: foo2", TestMessageDispatcher.SomeStringValue);
            Assert.AreEqual(null, TestMessageDispatcher.SomeIntValue);
        }        
    }

    public class TestMessageDispatcher
    {
        public static string SomeStringValue { get; set; }
        public static int? SomeIntValue { get; set; }

        public static void ClearValues()
        {
            SomeStringValue = null;
            SomeIntValue = null;
        }

        public void DoSomethingWithMessageContext(JsonMessageContext messageContext, int? someInt)
        {
            SomeStringValue = "Called with message context: " + (messageContext != null ? "true" : "false");
            SomeIntValue = someInt;
        }

        public void DoSomethingWithMessageBody(TestDispatchBody messageBody)
        {
            SomeStringValue = "Called with message body: " + (messageBody.SomeStr);
        }

        public class TestDispatchBody
        {
            public string SomeStr { get; set; }
            public int? SomeInt { get; set; }
        }
    }
}
