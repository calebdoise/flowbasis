using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowBasis.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlowBasis.Json.Mappers;

namespace FlowBasisJsonUnitTests.Json
{
    [TestClass]
    public class JsonPropertyAttributeTests
    {

        [TestMethod]
        public void Should_Roundtrip_Customize_Property_Name()
        {
            JObjectRootMapper mapper = new JObjectRootMapper();

            TestObject testObject = new TestObject()
            {
                StringOfOneName = "MyValue"
            };

            dynamic resultObject = mapper.ToJObject(testObject);
            Assert.IsNull(resultObject.StringOfOneName);
            Assert.AreEqual("MyValue", resultObject.StringOfAnotherName);

            TestObject resultTestObject = (TestObject)mapper.FromJObject(resultObject, typeof(TestObject));
            Assert.AreEqual("MyValue", resultTestObject.StringOfOneName);
        }


        [TestMethod]
        public void Should_Roundtrip_Property_With_Custom_Mapper_Type()
        {
            JObjectRootMapper mapper = new JObjectRootMapper();

            TestObject testObject = new TestObject()
            {
                Name = new TestNameHolder() { FirstName = "Bob", LastName = "Whoever" },
                NumberAsString = 4.56M
            };

            dynamic resultObject = mapper.ToJObject(testObject);
            Assert.IsInstanceOfType(resultObject.name, typeof(string));
            Assert.AreEqual("Bob Whoever", resultObject.name);
            Assert.AreEqual("4.56", resultObject.numberAsString);

            TestObject resultTestObject = (TestObject)mapper.FromJObject(resultObject, typeof(TestObject));
            Assert.AreEqual("Bob", resultTestObject.Name.FirstName);
            Assert.AreEqual("Whoever", resultTestObject.Name.LastName);
            Assert.AreEqual(4.56M, resultTestObject.NumberAsString);
        }
        


        public class TestObject
        {
            [JsonProperty(Name = "StringOfAnotherName")]
            public string StringOfOneName { get; set; }

            [JsonProperty(MapperType = typeof(TestNameHolderMapper))]
            public TestNameHolder Name { get; set; }

            [JsonProperty(MapperType = typeof(NumberAsStringMapper))]
            public decimal? NumberAsString { get; set; }
        }

        public class TestNameHolder
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class TestNameHolderMapper : IJObjectMapper
        {            
            public object ToJObject(object instance, IJObjectRootMapper rootMapper)
            {
                if (instance == null)
                    return null;

                return ((TestNameHolder)instance).FirstName + " " + ((TestNameHolder)instance).LastName;
            }

            public object FromJObject(object jObject, Type targetType, IJObjectRootMapper rootMapper)
            {
                if (jObject == null)
                    return null;

                if (targetType != typeof(TestNameHolder))
                {
                    throw new Exception("Only TestNameHolder supported.");
                }

                string[] parts = ((string)jObject).Split(' ');

                return new TestNameHolder()
                    {
                        FirstName = parts[0],
                        LastName = parts[1]
                    };
            }
        }
    }
}
