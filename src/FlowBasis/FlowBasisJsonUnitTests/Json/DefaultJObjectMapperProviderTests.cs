using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowBasis.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowBasisJsonUnitTests.Json
{

    [TestClass]
    public class DefaultJObjectMapperProviderTests
    {

        [TestMethod]
        public void Test_RegisterJObjectMapperForType()
        {
            DefaultJObjectMapperProvider mapperProvider = new DefaultJObjectMapperProvider();
            mapperProvider.RegisterJObjectMapperForType(typeof(TestObject), new TestObjectMapper());

            var jsonSerializer = new JsonSerializationService(() => new JObjectRootMapper(mapperProvider));

            // TestObject should get converted into reversed case string when serialized with custom mapper.
            string json = jsonSerializer.Stringify(new TestObject() { SomeStr = "Abc" });
            Assert.AreEqual("\"aBC\"", json);

            // And it should be deserialized back into TestObject from a string.
            var result = jsonSerializer.Parse<TestObject>(json);
            Assert.AreEqual("Abc", result.SomeStr);
        }

        [TestMethod]
        public void Test_RegisterJObjectMapperForType_AfterMappingHasAlreadyOccurred()
        {
            DefaultJObjectMapperProvider mapperProvider = new DefaultJObjectMapperProvider();

            var jsonSerializer = new JsonSerializationService(() => new JObjectRootMapper(mapperProvider));

            // TestObject should get serialized and deserialzied in the default way.
            string json = jsonSerializer.Stringify(new TestObject() { SomeStr = "Abc" });
            Assert.AreEqual("{\"someStr\":\"Abc\"}", json);

            var result = jsonSerializer.Parse<TestObject>(json);
            Assert.AreEqual("Abc", result.SomeStr);

            // Now we register a custom mapping and ensure it gets used.
            mapperProvider.RegisterJObjectMapperForType(typeof(TestObject), new TestObjectMapper());

            // TestObject should get converted into reversed case string when serialized with custom mapper.
            json = jsonSerializer.Stringify(new TestObject() { SomeStr = "Abc" });
            Assert.AreEqual("\"aBC\"", json);

            // And it should be deserialized back into TestObject from a string.
            result = jsonSerializer.Parse<TestObject>(json);
            Assert.AreEqual("Abc", result.SomeStr);
        }


        public class TestObject
        {
            public string SomeStr { get; set; }
        }

        public class TestObjectMapper : IJObjectMapper
        {
            public object ToJObject(object instance, IJObjectRootMapper rootMapper)
            {
                TestObject testObject = (TestObject)instance;

                StringBuilder sb = new StringBuilder();
                foreach (char ch in testObject.SomeStr)
                {
                    if (Char.IsLower(ch))
                    {
                        sb.Append(Char.ToUpper(ch));
                    }
                    else
                    {
                        sb.Append(Char.ToLower(ch));
                    }
                }

                return sb.ToString();
            }

            public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
            {
                string str = (string)jObject;

                StringBuilder sb = new StringBuilder();
                foreach (char ch in str)
                {
                    if (Char.IsLower(ch))
                    {
                        sb.Append(Char.ToUpper(ch));
                    }
                    else
                    {
                        sb.Append(Char.ToLower(ch));
                    }
                }

                return new TestObject() { SomeStr = sb.ToString() };
            }
        }
    }

}
