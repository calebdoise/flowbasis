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
            var simpleQueueManager = new QueueManager<ISimpleQueue>();
            simpleQueueManager.RegisterQueue("JsonTest", new InMemorySimpleQueue(QueueMode.Queue));

            var jsonQueueManager = new QueueManager<IJsonMessageQueue>();
            jsonQueueManager.RegisterQueue("JsonTest",
                new JsonMessageQueueViaSimpleQueue(
                    new JsonMessageQueueViaSimpleQueueOptions
                    {
                        QueueManager = simpleQueueManager,
                        QueueName = "JsonTest"                        
                    }));

            var dispatcherResolver = new JsonMessageDispatcherResolver();
            dispatcherResolver.RegisterDispatchControllerTypePublicMethods(typeof(JsonMessageQueueTester));

            JsonMessageQueueListener.Subscribe(jsonQueueManager.GetQueue("JsonTest"), dispatcherResolver);

            var jsonQueue = jsonQueueManager.GetQueue("JsonTest");

            JsonMessageQueueTester.Result = null;

            jsonQueue.Publish(
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
