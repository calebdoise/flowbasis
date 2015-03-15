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
    public class JObjectRootMapperTests
    {
        [TestMethod]
        public void Should_Convert_Primitives_To_Themselves()
        {
            JObjectRootMapper classMapper = new JObjectRootMapper();

            object result;
            
            result = classMapper.ToJObject(null);
            Assert.IsNull(result);

            result = classMapper.ToJObject("hello");
            Assert.AreEqual("hello", result);

            result = classMapper.ToJObject(4);
            Assert.AreEqual(4, result);

            result = classMapper.ToJObject(4.1f);
            Assert.AreEqual(4.1f, result);

            result = classMapper.ToJObject(4.2d);
            Assert.AreEqual(4.2d, result);

            result = classMapper.ToJObject(4.3m);
            Assert.AreEqual(4.3m, result);

            result = classMapper.ToJObject(true);
            Assert.AreEqual(true, result);
        }


        [TestMethod]
        public void Should_Restore_Primitives_To_Themselves()
        {
            JObjectRootMapper classMapper = new JObjectRootMapper();

            string resultStr;
            int resultInt;
            float resultFloat;
            double resultDouble;
            decimal resultDecimal;

            resultStr = (string)classMapper.FromJObject(null, typeof(string));
            Assert.IsNull(resultStr);

            resultStr = (string)classMapper.FromJObject("hello", typeof(string));
            Assert.AreEqual("hello", resultStr);

            resultInt = (int)classMapper.FromJObject(4, typeof(int));
            Assert.AreEqual(4, resultInt);

            resultFloat = (float)classMapper.FromJObject(4.25f, typeof(float));
            Assert.AreEqual(4.25f, resultFloat);

            resultDouble = (double)classMapper.FromJObject(4.5d, typeof(double));
            Assert.AreEqual(4.5d, resultDouble);

            resultDecimal = (decimal)classMapper.FromJObject(4.75m, typeof(decimal));
            Assert.AreEqual(4.75m, resultDecimal);            
        }


        [TestMethod]
        public void Should_Restore_Floating_Points_Between_Types()
        {
            JObjectRootMapper classMapper = new JObjectRootMapper();
                        
            float resultFloat;
            double resultDouble;
            decimal resultDecimal;

            // Assigning JSON integer to various floating point target types.

            resultFloat = (float)classMapper.FromJObject(4, typeof(float));
            Assert.AreEqual(4, resultFloat);
  
            resultDouble = (double)classMapper.FromJObject(4, typeof(double));
            Assert.AreEqual(4, resultDouble);

            resultDecimal = (decimal)classMapper.FromJObject(4, typeof(decimal));
            Assert.AreEqual(4, resultDecimal);

            // Assigning JSON decimal to various floating point target types.

            resultFloat = (float)classMapper.FromJObject(4.5m, typeof(float));
            Assert.AreEqual(4.5f, resultFloat);

            resultDouble = (double)classMapper.FromJObject(4.5m, typeof(double));
            Assert.AreEqual(4.5d, resultDouble);

            // Assigning JSON float to various floating point target types.

            resultDouble = (double)classMapper.FromJObject(4.5m, typeof(double));
            Assert.AreEqual(4.5f, resultFloat);

            resultDecimal = (decimal)classMapper.FromJObject(4.5m, typeof(decimal));
            Assert.AreEqual(4.5d, resultDouble);            
        }




        [TestMethod]
        public void Should_Convert_Arrays_And_Lists_To_Array_List()
        {
            JObjectRootMapper classMapper = new JObjectRootMapper();

            ArrayList result;

            result = (ArrayList)classMapper.ToJObject(new int[] { -2, 4 });
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(-2, result[0]);
            Assert.AreEqual(4, result[1]);

            result = (ArrayList)classMapper.ToJObject(new string[] { "hello", "world" });
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("hello", result[0]);
            Assert.AreEqual("world", result[1]);

            result = (ArrayList)classMapper.ToJObject(new System.Collections.ArrayList() { 5, "green", "red" });
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(5, result[0]);
            Assert.AreEqual("green", result[1]);
            Assert.AreEqual("red", result[2]);

            result = (ArrayList)classMapper.ToJObject(new List<int>() { 34, -10, 3 });
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(34, result[0]);
            Assert.AreEqual(-10, result[1]);
            Assert.AreEqual(3, result[2]);
        }


        [TestMethod]
        public void Should_Restore_Arrays_And_Lists_From_List_Object()
        {
            JObjectRootMapper classMapper = new JObjectRootMapper();

            int[] resultIntArray;
            string[] resultStrArray;
            System.Collections.ArrayList resultArrayList;
            List<int> resultListInt;
            IList<int> resultGenericListInt;

            resultIntArray = (int[])classMapper.FromJObject(new List<object> { -2, 4 }, typeof(int[]));
            Assert.AreEqual(2, resultIntArray.Length);
            Assert.AreEqual(-2, resultIntArray[0]);
            Assert.AreEqual(4, resultIntArray[1]);

            resultStrArray = (string[])classMapper.FromJObject(new List<object> { "hello", "world" }, typeof(string[]));
            Assert.AreEqual(2, resultStrArray.Length);
            Assert.AreEqual("hello", resultStrArray[0]);
            Assert.AreEqual("world", resultStrArray[1]);

            resultArrayList = (System.Collections.ArrayList)classMapper.FromJObject(new List<object> { 5, "green", "red" }, typeof(System.Collections.ArrayList));
            Assert.AreEqual(3, resultArrayList.Count);
            Assert.AreEqual(5, resultArrayList[0]);
            Assert.AreEqual("green", resultArrayList[1]);
            Assert.AreEqual("red", resultArrayList[2]);

            resultListInt = (List<int>)classMapper.FromJObject(new List<object>() { 34, -10, 3 }, typeof(List<int>));
            Assert.AreEqual(3, resultListInt.Count);
            Assert.AreEqual(34, resultListInt[0]);
            Assert.AreEqual(-10, resultListInt[1]);
            Assert.AreEqual(3, resultListInt[2]);

            resultGenericListInt = (IList<int>)classMapper.FromJObject(new List<object>() { 35, -12, 4 }, typeof(IList<int>));
            Assert.AreEqual(3, resultGenericListInt.Count);
            Assert.AreEqual(35, resultGenericListInt[0]);
            Assert.AreEqual(-12, resultGenericListInt[1]);
            Assert.AreEqual(4, resultGenericListInt[2]);
        }



        [TestMethod]
        public void Should_Convert_Strongly_Typed_Objects_To_JObject()
        {
            JObjectRootMapper classMapper = new JObjectRootMapper();

            dynamic result;

            TestLibrary library = new TestLibrary()
            {
                Books = new List<TestBook>()
                {
                    new TestBook() 
                    {
                        Author = "Bob", 
                        Title = "Bob's Book",
                        Price = 20.5M, 
                        IgnoredTempData = "Should not be serialized"
                    },
                    new TestBook()
                    {
                        Author = "Alice", 
                        Title = "Another One", 
                        Price = 10M,
                        BookType = TestBookType.Biography
                    }
                }
            };

            result = (JObject)classMapper.ToJObject(library);         
            Assert.IsInstanceOfType(result.books, typeof(ArrayList));
            Assert.AreEqual(2, result.books.Count);

            Assert.AreEqual("Bob", result.books[0].author);
            Assert.AreEqual("Bob's Book", result.books[0].title);
            Assert.AreEqual(20.5M, result.books[0].publishedPrice);
            Assert.IsNull(result.books[0].ignoredTempData);

            Assert.AreEqual("Alice", result.books[1].author);
            Assert.AreEqual("Another One", result.books[1].title);
            Assert.AreEqual(10, result.books[1].publishedPrice);
            Assert.AreEqual("Biography", result.books[1].bookType);
        }


        [TestMethod]
        public void Should_Restore_Strongly_Typed_Objects_From_JObject()
        {
            JObjectRootMapper classMapper = new JObjectRootMapper();

            TestLibrary result;

            dynamic libraryJObject = new JObject();
            libraryJObject.books = new List<object>();
              
            libraryJObject.books.Add(new JObject()
            {
                { "author", "Bob" },
                { "title", "Bob's Book" },
                { "publishedPrice", 20.5M },
                { "ignoredTempData", "Shoud not be deserialized" },
                { "bookType", "Educational" }
            });

            libraryJObject.books.Add(new JObject()
            {
                { "author", "Alice" },
                { "title", "Another One" },
                { "publishedPrice", 10M },
                { "bookType", "ScienceFiction" }
            });

            result = (TestLibrary)classMapper.FromJObject(libraryJObject, typeof(TestLibrary));            
            Assert.AreEqual(2, result.Books.Count);

            Assert.AreEqual("Bob", result.Books[0].Author);
            Assert.AreEqual("Bob's Book", result.Books[0].Title);
            Assert.AreEqual(20.5M, result.Books[0].Price);
            Assert.IsNull(result.Books[0].IgnoredTempData);
            Assert.AreEqual(TestBookType.Educational, result.Books[0].BookType);

            Assert.AreEqual("Alice", result.Books[1].Author);
            Assert.AreEqual("Another One", result.Books[1].Title);
            Assert.AreEqual(10, result.Books[1].Price);
            Assert.AreEqual(TestBookType.ScienceFiction, result.Books[1].BookType);
        }


        [TestMethod]
        public void Should_Convert_Nullables_To_JObject()
        {
            JObjectRootMapper mapper = new JObjectRootMapper();

            TestNullableContainer container = new TestNullableContainer()
            {
                NullableInt = 1,
                NullableFloat = 2.5f,
                NullableDouble = 3.5d,
                NullableDecimal = 4.5m,
                NullableIntList = new List<int?> { 6, -2 },
                NullableEnum = TestBookType.ScienceFiction
            };

            JObject result = (JObject)mapper.ToJObject(container);

            Assert.IsInstanceOfType(result["nullableInt"], typeof(int));
            Assert.AreEqual(1, result["nullableInt"]);
            Assert.IsInstanceOfType(result["nullableFloat"], typeof(float));
            Assert.AreEqual(2.5f, result["nullableFloat"]);
            Assert.IsInstanceOfType(result["nullableDouble"], typeof(double));
            Assert.AreEqual(3.5d, result["nullableDouble"]);
            Assert.IsInstanceOfType(result["nullableDecimal"], typeof(decimal));
            Assert.AreEqual(4.5m, result["nullableDecimal"]);

            Assert.IsInstanceOfType(result["nullableEnum"], typeof(string));
            Assert.AreEqual("ScienceFiction", result["nullableEnum"]);

            Assert.IsInstanceOfType(result["nullableIntList"], typeof(ArrayList));
            var intList = (ArrayList)result["nullableIntList"];
            Assert.AreEqual(2, intList.Count);
            Assert.IsInstanceOfType(intList[0], typeof(int));
            Assert.AreEqual(6, intList[0]);
            Assert.AreEqual(-2, intList[1]);
        }


        [TestMethod]
        public void Should_Restore_Nullables_To_Strongly_Typed_Object()
        {
            JObjectRootMapper mapper = new JObjectRootMapper();

            dynamic jObject = new JObject();
            jObject["nullableInt"] = null;
            jObject["nullableFloat"] = null;
            jObject["nullableDouble"] = null;
            jObject["nullableDecimal"] = null;
            jObject["nullableIntList"] = new List<int?> { 5, null, -1 };
            jObject["nullableEnum"] = "Biography";

            var result = (TestNullableContainer)mapper.FromJObject(jObject, typeof(TestNullableContainer));
            Assert.IsFalse(result.NullableInt.HasValue);
            Assert.IsFalse(result.NullableFloat.HasValue);
            Assert.IsFalse(result.NullableDouble.HasValue);
            Assert.IsFalse(result.NullableDecimal.HasValue);

            Assert.AreEqual(TestBookType.Biography, result.NullableEnum.Value);

            Assert.AreEqual(3, result.NullableIntList.Count);
            Assert.AreEqual(5, result.NullableIntList[0].Value);
            Assert.IsFalse(result.NullableIntList[1].HasValue);
            Assert.AreEqual(-1, result.NullableIntList[2].Value);
        }


        [TestMethod]
        public void Should_Deserialize_JObject_List_To_List_Object_With_JObjects()
        {
            JObjectRootMapper mapper = new JObjectRootMapper();

            var sourceList = new List<Object>();

            dynamic obj1 = new JObject();
            obj1.hello = "world";
            obj1.someInt = 42;

            dynamic obj2 = new JObject();
            obj2.hello = "blarg";
            obj2.someFlag = true;

            sourceList.Add(obj1);
            sourceList.Add(obj2);
            sourceList.Add("some string");

            var result = (List<object>)mapper.FromJObject(sourceList, typeof(List<object>));

            Assert.AreEqual(3, result.Count);

            Assert.IsInstanceOfType(result[0], typeof(JObject));

            Assert.IsInstanceOfType(result[1], typeof(JObject));

            Assert.IsInstanceOfType(result[2], typeof(string));
            Assert.AreEqual("some string", result[2]);
        }

        [TestMethod]
        public void Should_Deserialize_JObject_List_To_List_Dynamic_With_JObjects()
        {
            JObjectRootMapper mapper = new JObjectRootMapper();

            var sourceList = new List<Object>();

            dynamic obj1 = new JObject();
            obj1.hello = "world";
            obj1.someInt = 42;

            dynamic obj2 = new JObject();
            obj2.hello = "blarg";
            obj2.someFlag = true;

            sourceList.Add(obj1);
            sourceList.Add(obj2);
            sourceList.Add("some string");

            var result = (List<dynamic>)mapper.FromJObject(sourceList, typeof(List<dynamic>));

            Assert.AreEqual(3, result.Count);

            Assert.IsInstanceOfType(result[0], typeof(JObject));

            Assert.IsInstanceOfType(result[1], typeof(JObject));

            Assert.IsInstanceOfType(result[2], typeof(string));
            Assert.AreEqual("some string", result[2]);
        }


        public class TestLibrary
        {
            public List<TestBook> Books { get; set; }
        }

        public class TestBook
        {
            public string Author { get; set; }
            public string Title { get; set; }

            public TestBookType BookType { get; set; } 

            [JsonProperty(Name = "publishedPrice")]
            public decimal Price { get; set; }

            [JsonIgnore]
            public string IgnoredTempData { get; set; }
        }

        public enum TestBookType
        {
            Unknown,
            Educational,
            ScienceFiction,
            Biography,
            Other
        }

        public class TestNullableContainer
        {
            public Nullable<int> NullableInt { get; set; }
            public Nullable<float> NullableFloat { get; set; }
            public Nullable<double> NullableDouble { get; set; }
            public Nullable<Decimal> NullableDecimal { get; set; }

            public Nullable<TestBookType> NullableEnum { get; set; }

            public List<Nullable<int>> NullableIntList { get; set; }
        }
    }
}
