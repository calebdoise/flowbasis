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

            Assert.AreEqual(
                "{\"color\":2}",
                jsonSerializer.Stringify(
                    new TestObjectEx
                    {
                        Color = TestEnumColors.Green                        
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

            resultTestObject = jsonSerializer.Parse<TestObjectEx>("{\"color\":2}");
            Assert.AreEqual(TestEnumColors.Green, resultTestObject.Color);
        }

        [TestMethod]
        public void Test_Default_Serializers()
        {
            Assert.AreEqual("{\"someNumber\":42}", JsonSerializers.Default.Stringify(new TestObject { SomeNumber = 42 }));
            Assert.AreEqual("{\"SomeNumber\":42}", JsonSerializers.NoCamelCasing.Stringify(new TestObject { SomeNumber = 42 }));
        }

        [TestMethod]
        public void Test_JsonSerializer_Map()
        {
            TestObject result = JsonSerializers.Default.Map<TestObject>(JObject.Parse("{\"someNumber\":42}"));
            Assert.AreEqual(42, result.SomeNumber);
        }


        [TestMethod]
        public void Test_Serialize_Dictionary_Members()
        {
            // Test with Dictionary<,>.
            var testGenericDictionary = new TestObjectWithDictionary
            {
                GenericDictionary = new Dictionary<string, object>
                {
                    { "hello", 42 }
                }
            };

            string json = JsonSerializers.Default.Stringify(testGenericDictionary);

            Assert.AreEqual("{\"genericDictionary\":{\"hello\":42}}", json);

            var resultGenericDictionary = JsonSerializers.Default.Parse<TestObjectWithDictionary>(json);

            Assert.IsNotNull(resultGenericDictionary?.GenericDictionary);
            Assert.AreEqual((long)42, resultGenericDictionary.GenericDictionary["hello"]);

            // Test with IDictionary<,>.
            var testGenericInterfaceDictionary = new TestObjectWithDictionary
            {
                GenericInterfaceDictionary = new Dictionary<string, object>
                {
                    { "hello", 42 }
                }
            };

            json = JsonSerializers.Default.Stringify(testGenericInterfaceDictionary);

            Assert.AreEqual("{\"genericInterfaceDictionary\":{\"hello\":42}}", json);

            var resultGenericInterfaceDictionary = JsonSerializers.Default.Parse<TestObjectWithDictionary>(json);

            Assert.IsNotNull(resultGenericInterfaceDictionary?.GenericInterfaceDictionary);
            Assert.AreEqual((long)42, resultGenericInterfaceDictionary.GenericInterfaceDictionary["hello"]);

            // Test with IDictionary<string,string>.
            var testStringStringDictionary = new TestObjectWithDictionary
            {
                StringStringDictionary = new Dictionary<string, string>
                {
                    { "hello", "world" }
                }
            };

            json = JsonSerializers.Default.Stringify(testStringStringDictionary);

            Assert.AreEqual("{\"stringStringDictionary\":{\"hello\":\"world\"}}", json);

            var resultStringStringDictionary = JsonSerializers.Default.Parse<TestObjectWithDictionary>(json);

            Assert.IsNotNull(resultStringStringDictionary?.StringStringDictionary);
            Assert.AreEqual("world", resultStringStringDictionary.StringStringDictionary["hello"]);
        }


        public class TestObject
        {
            public int SomeNumber { get; set; }

            public string SomeString { get; set; }

            public byte? SomeNullableByte { get; set; }
        }


        public class TestObjectEx
        {
            [FlowBasisJsonUnitTests.Json.JsonSerializationServiceTests.JsonDateTimeAsEpochMillisecondsAttribute]
            public DateTime? SomeDate { get; set; }

            [FlowBasisJsonUnitTests.Json.JsonSerializationServiceTests.JsonEnumAsInteger]
            public TestEnumColors? Color { get; set; }

            [FlowBasisJsonUnitTests.Json.JsonSerializationServiceTests.JsonIgnore]
            public int NumberThatWillNotBeSerialized { get; set; }
        }

        public class TestObjectWithDictionary
        {
            public Dictionary<string, object> GenericDictionary { get; set; }
            public IDictionary<string, object> GenericInterfaceDictionary { get; set; }
            public Dictionary<string, string> StringStringDictionary { get; set; }
        }

        public enum TestEnumColors : int
        {
            Red = 1,
            Green = 2,
            Blue = 3
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class JsonDateTimeAsEpochMillisecondsAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class JsonEnumAsIntegerAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class JsonIgnoreAttribute : Attribute
        {
        }
    }
}
