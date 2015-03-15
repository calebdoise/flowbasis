using FlowBasis.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowBasisJsonUnitTests.Json
{
    [TestClass]
    public class NullablePrimitiveTests
    {
        [TestMethod]
        public void TestNullablePrimitiveDeserializationWhenValueIsNull()
        {
            IJObjectRootMapper rootMapper = new JObjectRootMapper();
            
            dynamic testObjectData = new JObject();
            testObjectData.decimalValue = null;
            testObjectData.intValue = null;
            testObjectData.boolValue = null;

            var result = (TestObject)rootMapper.FromJObject(testObjectData, typeof(TestObject));

            Assert.IsNull(result.DecimalValue);
            Assert.IsNull(result.IntValue);
            Assert.IsNull(result.BoolValue);
        }

        [TestMethod]
        public void TestNullablePrimitiveDeserializationWhenValueIsEmptyString()
        {
            IJObjectRootMapper rootMapper = new JObjectRootMapper();

            dynamic testObjectData = new JObject();
            testObjectData.decimalValue = "";
            testObjectData.intValue = "";
            testObjectData.boolValue = "";

            var result = (TestObject)rootMapper.FromJObject(testObjectData, typeof(TestObject));

            Assert.IsNull(result.DecimalValue);
            Assert.IsNull(result.IntValue);
            Assert.IsNull(result.BoolValue);
        }

        [TestMethod]
        public void TestNullablePrimitiveDeserializationWhenValueIsString()
        {
            IJObjectRootMapper rootMapper = new JObjectRootMapper();

            dynamic testObjectData = new JObject();
            testObjectData.decimalValue = "4.678";
            testObjectData.intValue = "32";
            testObjectData.boolValue = "true";

            var result = (TestObject)rootMapper.FromJObject(testObjectData, typeof(TestObject));
            
            Assert.AreEqual(4.678M, result.DecimalValue);
            Assert.AreEqual(32, result.IntValue);
            Assert.IsTrue(result.BoolValue.Value);
        }

        public class TestObject
        {
            public decimal? DecimalValue { get; set; }
            public int? IntValue { get; set; }
            public bool? BoolValue { get; set; }
        }
    }
}
