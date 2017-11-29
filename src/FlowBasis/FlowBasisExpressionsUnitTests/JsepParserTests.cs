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
        public void Test_Basic_Binary_Expressions()
        {
            var jsepParser = new JsepParser();
            JsepNode result = jsepParser.Parse("3 + 4");

            Assert.AreEqual(JsepNodeType.BinaryExpression, result.Type);
            Assert.AreEqual("+", result.Operator);
            AssertNodeIsLiteralNumber(result.Left, 3);
            AssertNodeIsLiteralNumber(result.Right, 4);

            Assert.IsNotNull(result);
        }
      
        private void AssertNodeIsLiteralNumber(JsepNode node, decimal expectedNumber)
        {
            Assert.AreEqual(JsepNodeType.Literal, node.Type);
            Assert.AreEqual(expectedNumber, node.Value);
        }
        
    }

}
