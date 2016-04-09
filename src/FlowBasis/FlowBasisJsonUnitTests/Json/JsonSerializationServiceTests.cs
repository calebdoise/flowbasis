using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowBasis.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowBasisJsonUnitTests.Json
{
    [TestClass]
    public class JsonSerializationServiceTests
    {
        [TestMethod]
        public void Test_Stringify()
        {
            Func<IJObjectRootMapper> rootMapperFactory = () => new JObjectRootMapper();
            var jsonSerializer = new JsonSerializationService(rootMapperFactory);

            Assert.AreEqual("null", jsonSerializer.Stringify(null));
            Assert.AreEqual("4", jsonSerializer.Stringify(4));
            Assert.AreEqual("\"Hello\"", jsonSerializer.Stringify("Hello"));

            // We won't have value ("someString": null) in the output because we exclude null values by default.
            Assert.AreEqual("{\"someNumber\":42}", jsonSerializer.Stringify(new TestObject { SomeNumber = 42 }));
        }

        [TestMethod]
        public void Test_Stringify_No_Camel_Case()
        {
            var rootMapper = new JObjectRootMapper(
                new DefaultJObjectMapperProvider(
                    new DefaultJObjectMapperProviderOptions
                    {
                        DefaultClassMappingOptions = new DefaultClassMappingOptions
                        {
                            UseCamelCase = false
                        }
                    }));
            
            var jsonSerializer = new JsonSerializationService(() => rootMapper);

            // We won't value ("someString": null) in the output because we exclude null values by default.
            Assert.AreEqual("{\"SomeNumber\":42}", jsonSerializer.Stringify(new TestObject { SomeNumber = 42 }));
        }

        [TestMethod]
        public void Test_Stringify_Advanced()
        {
            Func<IJObjectRootMapper> rootMapperFactory = () => new JObjectRootMapper();
            var jsonSerializer = new JsonSerializationService(rootMapperFactory);

            Assert.AreEqual(
                "{\"someDate\":1399075200000}",
                jsonSerializer.Stringify(
                    new TestObjectEx
                    {
                        SomeDate = new DateTime(2014, 5, 3),
                        NumberThatWillNotBeSerialized = 452342
                    }));
        }


        [TestMethod]
        public void Test_Parse()
        {
            Func<IJObjectRootMapper> rootMapperFactory = () => new JObjectRootMapper();
            var jsonSerializer = new JsonSerializationService(rootMapperFactory);

            Assert.IsNull(jsonSerializer.Parse("null"));
            Assert.AreEqual((Int64)4, jsonSerializer.Parse("4"));
            Assert.AreEqual("Hello", jsonSerializer.Parse("\"Hello\""));

            JObject resultJObject = (JObject)jsonSerializer.Parse("{\"someNumber\":42}");
            Assert.AreEqual((Int64)42, resultJObject["someNumber"]);

            TestObject resultTestObject = (TestObject)jsonSerializer.Parse("{\"someNumber\":43}", typeof(TestObject));
            Assert.AreEqual(43, resultTestObject.SomeNumber);

            resultTestObject = jsonSerializer.Parse<TestObject>("{\"someNumber\":44}");
            Assert.AreEqual(44, resultTestObject.SomeNumber);

            resultTestObject = jsonSerializer.Parse<TestObject>("{\"someNumber\":45,\"someString\":\"hello world\"}");
            Assert.AreEqual(45, resultTestObject.SomeNumber);
            Assert.AreEqual("hello world", resultTestObject.SomeString);

            resultTestObject = jsonSerializer.Parse<TestObject>("{\"someNumber\":45,\"someString\":\"hello world\",\"someNullableByte\":46}");
            Assert.AreEqual(45, resultTestObject.SomeNumber);
            Assert.AreEqual("hello world", resultTestObject.SomeString);
            Assert.AreEqual((byte)46, resultTestObject.SomeNullableByte);
        }

        [TestMethod]
        public void Test_Parse_Advanced()
        {
            Func<IJObjectRootMapper> rootMapperFactory = () => new JObjectRootMapper();
            var jsonSerializer = new JsonSerializationService(rootMapperFactory);

            TestObjectEx resultTestObject = jsonSerializer.Parse<TestObjectEx>("{\"someDate\":1399075200000}");
            Assert.AreEqual(new DateTime(2014, 5, 3), resultTestObject.SomeDate);            
        }


        public class TestObject
        {
            public int SomeNumber { get; set; }

            public string SomeString { get; set; }

            public byte? SomeNullableByte { get; set; }
        }


        public class TestObjectEx
        {
            [JsonDateTimeAsEpochMillisecondsAttribute]
            public DateTime? SomeDate { get; set; }

            [FlowBasisJsonUnitTests.Json.JsonSerializationServiceTests.JsonIgnore]
            public int NumberThatWillNotBeSerialized { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class JsonDateTimeAsEpochMillisecondsAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class JsonIgnoreAttribute : Attribute
        {
        }
    }
}
