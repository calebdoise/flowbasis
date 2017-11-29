using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowBasis.Expressions;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowBasisExpressionsUnitTests
{
    
    [TestClass]
    public class JsepParserTests
    {
        [TestMethod]
        public void Test_Basic_Math_Expressions()
        {
            var jsepParser = new JsepParser();

            // 3 + 4
            JsepNode result = jsepParser.Parse("3 + 4");

            Assert.AreEqual(JsepNodeType.BinaryExpression, result.Type);
            Assert.AreEqual("+", result.Operator);
            AssertNodeIsLiteralNumber(result.Left, 3);
            AssertNodeIsLiteralNumber(result.Right, 4);

            // 3 + 4 * 7
            result = jsepParser.Parse("3 + 4 * 7");

            Assert.AreEqual(JsepNodeType.BinaryExpression, result.Type);
            Assert.AreEqual("+", result.Operator);
            AssertNodeIsLiteralNumber(result.Left, 3);
            AssertNodeIsBinaryExpression(result.Right, "*");
            AssertNodeIsLiteralNumber(result.Right.Left, 4);
            AssertNodeIsLiteralNumber(result.Right.Right, 7);

            // 3 + (1 - 12) * 7
            result = jsepParser.Parse("3 + (1 - 12) * 7");

            Assert.AreEqual(JsepNodeType.BinaryExpression, result.Type);
            Assert.AreEqual("+", result.Operator);
            AssertNodeIsLiteralNumber(result.Left, 3);
            AssertNodeIsBinaryExpression(result.Right, "*");
            AssertNodeIsBinaryExpression(result.Right.Left, "-");
            AssertNodeIsLiteralNumber(result.Right.Left.Left, 1);
            AssertNodeIsLiteralNumber(result.Right.Left.Right, 12);
            AssertNodeIsLiteralNumber(result.Right.Right, 7);
        }


        [TestMethod]
        public void Test_Basic_Call_Expressions()
        {
            var jsepParser = new JsepParser();

            // myFunc(4)
            JsepNode result = jsepParser.Parse("myFunc(4)");

            Assert.AreEqual(JsepNodeType.CallExpression, result.Type);

            Assert.AreEqual(JsepNodeType.Identifier, result.Callee.Type);
            Assert.AreEqual("myFunc", result.Callee.Name);

            Assert.AreEqual(1, result.Arguments.Count);
            AssertNodeIsLiteralNumber(result.Arguments[0], 4);

            // myFunc(4, "foo", 'blarg', someField)
            result = jsepParser.Parse("myFunc(4, \"foo\", 'blarg', someField)");

            Assert.AreEqual(JsepNodeType.CallExpression, result.Type);

            Assert.AreEqual(JsepNodeType.Identifier, result.Callee.Type);
            Assert.AreEqual("myFunc", result.Callee.Name);

            Assert.AreEqual(4, result.Arguments.Count);
            AssertNodeIsLiteralNumber(result.Arguments[0], 4);
            AssertNodeIsLiteralString(result.Arguments[1], "foo");
            AssertNodeIsLiteralString(result.Arguments[2], "blarg");
            AssertNodeIsNamedIdentifier(result.Arguments[3], "someField");

            // someObj.myFunc(someField + 3, "foo")
            result = jsepParser.Parse("someObj.myFunc(someField + 3, \"foo\")");

            Assert.AreEqual(JsepNodeType.CallExpression, result.Type);

            Assert.AreEqual(JsepNodeType.MemberExpression, result.Callee.Type);
            AssertNodeIsNamedIdentifier(result.Callee.Object, "someObj");
            AssertNodeIsNamedIdentifier(result.Callee.Property, "myFunc");            

            Assert.AreEqual(2, result.Arguments.Count);

            Assert.AreEqual(JsepNodeType.BinaryExpression, result.Arguments[0].Type);
            Assert.AreEqual("+", result.Arguments[0].Operator);
            AssertNodeIsNamedIdentifier(result.Arguments[0].Left, "someField");
            AssertNodeIsLiteralNumber(result.Arguments[0].Right, 3);

            AssertNodeIsLiteralString(result.Arguments[1], "foo");           
        }


        [TestMethod]
        public void Test_Basic_Array_Expressions()
        {
            var jsepParser = new JsepParser();

            // [1, 2, "hello", -pi]
            JsepNode result = jsepParser.Parse("[23, 6, \"hello\", -pi]");

            Assert.AreEqual(JsepNodeType.ArrayExpression, result.Type);

            Assert.AreEqual(4, result.Elements.Count);
            AssertNodeIsLiteralNumber(result.Elements[0], 23);
            AssertNodeIsLiteralNumber(result.Elements[1], 6);
            AssertNodeIsLiteralString(result.Elements[2], "hello");

            var negPi = result.Elements[3];
            Assert.AreEqual(JsepNodeType.UnaryExpression, negPi.Type);
            Assert.AreEqual("-", negPi.Operator);
            AssertNodeIsNamedIdentifier(negPi.Argument, "pi");
        }


        [TestMethod]
        public void Test_Basic_Compound_Expressions()
        {
            var jsepParser = new JsepParser();

            // 10 + -2; this.x - 1 == false; 4 <= 8;
            JsepNode result = jsepParser.Parse("10 + -2; this.x == false; 4 && 8;");

            Assert.AreEqual(JsepNodeType.Compound, result.Type);
            Assert.AreEqual(3, result.Body.Count);

            Assert.AreEqual(JsepNodeType.BinaryExpression, result.Body[0].Type);
            Assert.AreEqual("+", result.Body[0].Operator);
            AssertNodeIsLiteralNumber(result.Body[0].Left, 10);
            Assert.AreEqual(JsepNodeType.UnaryExpression, result.Body[0].Right.Type);
            Assert.AreEqual("-", result.Body[0].Right.Operator);
            AssertNodeIsLiteralNumber(result.Body[0].Right.Argument, 2);

            Assert.AreEqual(JsepNodeType.BinaryExpression, result.Body[1].Type);
            Assert.AreEqual("==", result.Body[1].Operator);
            Assert.AreEqual(JsepNodeType.MemberExpression, result.Body[1].Left.Type);
            Assert.AreEqual(JsepNodeType.ThisExpression, result.Body[1].Left.Object.Type);
            AssertNodeIsNamedIdentifier(result.Body[1].Left.Property, "x");
            Assert.AreEqual(JsepNodeType.Literal, result.Body[1].Right.Type);
            Assert.AreEqual(false, result.Body[1].Right.Value);

            Assert.AreEqual(JsepNodeType.LogicalExpression, result.Body[2].Type);
            Assert.AreEqual("&&", result.Body[2].Operator);
            AssertNodeIsLiteralNumber(result.Body[2].Left, 4);
            AssertNodeIsLiteralNumber(result.Body[2].Right, 8);
        }


        [TestMethod]
        public void Test_Basic_Conditional_Expressions()
        {
            var jsepParser = new JsepParser();

            // 3 > 4 ? 6 : 5
            JsepNode result = jsepParser.Parse("3 > 4 ? 6 : 5");

            Assert.AreEqual(JsepNodeType.ConditionalExpression, result.Type);

            Assert.AreEqual(JsepNodeType.BinaryExpression, result.Test.Type);
            Assert.AreEqual(">", result.Test.Operator);
            AssertNodeIsLiteralNumber(result.Test.Left, 3);
            AssertNodeIsLiteralNumber(result.Test.Right, 4);

            AssertNodeIsLiteralNumber(result.Consequent, 6);
            AssertNodeIsLiteralNumber(result.Alternate, 5);
        }

        private void AssertNodeIsLiteralNumber(JsepNode node, decimal expectedNumber)
        {
            Assert.AreEqual(JsepNodeType.Literal, node.Type);
            Assert.AreEqual(expectedNumber, node.Value);
        }

        private void AssertNodeIsLiteralString(JsepNode node, string expectedString)
        {
            Assert.AreEqual(JsepNodeType.Literal, node.Type);
            Assert.AreEqual(expectedString, node.Value);
        }

        private void AssertNodeIsNamedIdentifier(JsepNode node, string identifierName)
        {
            Assert.AreEqual(JsepNodeType.Identifier, node.Type);
            Assert.AreEqual(identifierName, node.Name);
        }

        private void AssertNodeIsBinaryExpression(JsepNode node, string expectedOperator)
        {
            Assert.AreEqual(JsepNodeType.BinaryExpression, node.Type);
            Assert.AreEqual(expectedOperator, node.Operator);
        }
    }

}
