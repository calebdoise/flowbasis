using FlowBasis.Json.Messages;
using FlowBasis.SimpleQueues;
using FlowBasis.SimpleQueues.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowBasisJsonUnitTests.Json.Messages
{
    [TestClass]
    public class JsonMessageQueueTests
    {    
        [TestMethod]
        public void Test_JsonMessageQueues_Basics()
        {
            var queueManager = new SimpleQueueManager(
                new SimpleQueueManagerOptions
                {
                    CreateQueueHandler = (string queueName, SimpleQueueMode queueMode, CreateQueueOptions options) =>
                    {
                        return new InMemorySimpleQueue(queueMode);
                    }
                });

            queueManager.RegisterQueue("JsonTest", SimpleQueueMode.Queue);

            var dispatchResolver = new JsonMessageDispatcherResolver();
            dispatchResolver.RegisterDispatchControllerTypePublicMethods(typeof(JsonMessageQueueTester));

            JsonMessageQueueListener.Subscribe(
                queueManager.GetQueue("JsonTest"),
                new JsonMessageQueueListenerOptions
                {
                    DispatcherResolver = dispatchResolver
                });

            var jsonQueueClient = new JsonMessageQueueClient(
                new JsonMessageQueueClientOptions
                {
                    QueueName = "JsonTest",
                    QueueManager = queueManager                                        
                });

            JsonMessageQueueTester.Result = null;

            jsonQueueClient.SendMessage(
                new JsonMessageContext(                
                    action: "JsonMessageQueueTester/Foo",
                    body: new JsonMessageQueueTester.FooRequest { SomeStr = "hello world 232" }));

            Thread.Sleep(100);

            Assert.AreEqual("hello world 232", JsonMessageQueueTester.Result);
        }
    }

    public class JsonMessageQueueTester
    {
        public static string Result { get; set; }

        public void Foo(FooRequest messageBody)
        {
            Result = messageBody.SomeStr;
        }

        public class FooRequest
        {
            public string SomeStr { get; set; }
        }
    }
}
