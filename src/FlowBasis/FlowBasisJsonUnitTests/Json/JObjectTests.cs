using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowBasis.Json;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowBasisJsonUnitTests.Json
{
    
    [TestClass]
    public class JObjectTests
    {
        [TestMethod]
        public void Should_Return_Null_For_Missing_Value()
        {
            dynamic jObject = new JObject();
            jObject.someValue = "hello";

            Assert.IsNull(jObject.missingValue);
            Assert.IsNull(((JObject)jObject)["missingValue"]);
        }


        [TestMethod]                               
        public void Should_Roundtrip_Strings_Ints_And_Booleans()
        {
            string json = JObject.Stringify("hello");
            object deserializedValue = JObject.Parse(json);
            Assert.AreEqual("hello", deserializedValue);

            json = JObject.Stringify("\"double quoted\"");
            deserializedValue = JObject.Parse(json);
            Assert.AreEqual("\"double quoted\"", deserializedValue);

            json = JObject.Stringify("'single quoted'");
            deserializedValue = JObject.Parse(json);
            Assert.AreEqual("'single quoted'", deserializedValue);

            json = JObject.Stringify(0);
            deserializedValue = JObject.Parse(json);
            Assert.AreEqual((Int64)0, deserializedValue);

            json = JObject.Stringify(-3);
            deserializedValue = JObject.Parse(json);
            Assert.AreEqual((Int64)(-3), deserializedValue);

            json = JObject.Stringify(5);
            deserializedValue = JObject.Parse(json);
            Assert.AreEqual((Int64)5, deserializedValue);

            json = JObject.Stringify(true);
            deserializedValue = JObject.Parse(json);
            Assert.AreEqual(true, deserializedValue);

            json = JObject.Stringify(false);
            deserializedValue = JObject.Parse(json);
            Assert.AreEqual(false, deserializedValue);
        }


        [TestMethod]        
        public void Should_Roundtrip_Floats_And_Doubles_To_Decimal()
        {
            string json = JObject.Stringify(5.5f);
            object deserializedValue = JObject.Parse(json);
            Assert.IsInstanceOfType(deserializedValue, typeof(decimal));
            Assert.AreEqual(Convert.ToDecimal(5.5f), (decimal)deserializedValue);

            json = JObject.Stringify(5.25d);
            deserializedValue = JObject.Parse(json);
            Assert.IsInstanceOfType(deserializedValue, typeof(decimal));
            Assert.AreEqual(Convert.ToDecimal(5.25d), (decimal)deserializedValue);

            json = JObject.Stringify(5.123456790123456790M);
            deserializedValue = JObject.Parse(json);
            Assert.IsInstanceOfType(deserializedValue, typeof(decimal));
            Assert.AreEqual(5.123456790123456790M, (decimal)deserializedValue);
        }


        [TestMethod]
        public void Should_Roundtrip_DateTimeUtc()
        {
            DateTime date = new DateTime(2010, 5, 4, 10, 3, 23).AddMilliseconds(256).ToUniversalTime();
            string json = JObject.Stringify(date);

            DateTime resultDate = (DateTime)JObject.Parse(json);
            Assert.AreEqual(date, resultDate);
            Assert.AreEqual(DateTimeKind.Utc, resultDate.Kind);
        }


        [TestMethod]   
        public void Should_Roundtrip_Arrays_To_Array_List()
        {
            // Array of mixed values.
            string json = JObject.Stringify(new object[] { 2, 3, "hello" });
            object deserializedValue = JObject.Parse(json);

            Assert.IsInstanceOfType(deserializedValue, typeof(ArrayList));

            var list = (ArrayList)deserializedValue;
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual((Int64)2, list[0]);
            Assert.AreEqual((Int64)3, list[1]);
            Assert.AreEqual("hello", list[2]);

            // Array of ints.
            json = JObject.Stringify(new object[] { -3, 12 });
            deserializedValue = JObject.Parse(json);

            Assert.IsInstanceOfType(deserializedValue, typeof(ArrayList));

            list = (ArrayList)deserializedValue;
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual((Int64)(-3), list[0]);
            Assert.AreEqual((Int64)12, list[1]);

            // Empty array.
            json = JObject.Stringify(new object[] {});
            deserializedValue = JObject.Parse(json);

            Assert.IsInstanceOfType(deserializedValue, typeof(ArrayList));

            list = (ArrayList)deserializedValue;
            Assert.AreEqual(0, list.Count);
        }


        [TestMethod]
        public void Should_Roundtrip_Arrays_Of_Arrays_To_Array_Lists()
        {
            // Array of mixed values.
            string json = JObject.Stringify(new object[] { -122, new int[] { 6, -1}, new object[] { "hello", "world" } });
            object deserializedValue = JObject.Parse(json);

            Assert.IsInstanceOfType(deserializedValue, typeof(ArrayList));

            var list = (ArrayList)deserializedValue;
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual((Int64)(-122), list[0]);

            Assert.IsInstanceOfType(list[1], typeof(ArrayList));
            var listChild1 = (ArrayList)list[1];
            Assert.AreEqual(2, listChild1.Count);
            Assert.AreEqual((Int64)6, listChild1[0]);
            Assert.AreEqual((Int64)(-1), listChild1[1]);

            Assert.IsInstanceOfType(list[2], typeof(ArrayList));
            var listChild2 = (ArrayList)list[2];
            Assert.AreEqual(2, listChild2.Count);
            Assert.AreEqual("hello", listChild2[0]);
            Assert.AreEqual("world", listChild2[1]);
        }

        [TestMethod]
        public void Should_Roundtrip_Array_Of_JObject()
        {
            // Array of mixed values.
            dynamic jObject = new JObject();
            jObject.someNum = 42;
            jObject.someStr = "hello";

            string json = JObject.Stringify(new object[] { jObject });
            object deserializedValue = JObject.Parse(json);

            Assert.IsInstanceOfType(deserializedValue, typeof(ArrayList));

            var list = (ArrayList)deserializedValue;
            Assert.AreEqual(1, list.Count);

            Assert.IsInstanceOfType(list[0], typeof(JObject));
            dynamic deserializedJObject = list[0];
            Assert.AreEqual(42, deserializedJObject.someNum);
            Assert.AreEqual("hello", deserializedJObject.someStr);
        }


        [TestMethod]
        public void Should_Roundtrip_Contained_Array_To_Array_List()
        {
            dynamic jObject = new JObject();
            jObject.intArray = new int[] { 2, 3, 5 };            

            string json = JObject.Stringify(jObject);
            object deserializedValue = JObject.Parse(json);
            Assert.IsInstanceOfType(deserializedValue, typeof(JObject));

            dynamic deserializedJObject = deserializedValue;

            Assert.IsInstanceOfType(deserializedJObject.intArray, typeof(ArrayList));

            ArrayList list = (ArrayList)deserializedJObject.intArray;
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual((Int64)2, list[0]);
            Assert.AreEqual((Int64)3, list[1]);
            Assert.AreEqual((Int64)5, list[2]);
        }


        [TestMethod]
        public void Should_Roundtrip_Nested_JObject()
        {
            dynamic jObject = new JObject();
            jObject.level1 = new JObject();
            jObject.level1.someNum = 12;
            jObject.level1.level2 = new JObject();
            jObject.level1.level2.someStr = "hello";

            string json = JObject.Stringify(jObject);
            object deserializedValue = JObject.Parse(json);
            Assert.IsInstanceOfType(deserializedValue, typeof(JObject));

            dynamic deserializedJObject = deserializedValue;

            Assert.IsInstanceOfType(deserializedJObject.level1, typeof(JObject));
            Assert.IsInstanceOfType(deserializedJObject.level1.level2, typeof(JObject));

            Assert.AreEqual(12, deserializedJObject.level1.someNum);
            Assert.AreEqual("hello", deserializedJObject.level1.level2.someStr);
        }


        
    }

}
