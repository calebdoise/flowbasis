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
    public class ExpressionEvaluatorTests
    {
        [TestMethod]
        public void Test_Evaluation_Basics()
        {
            var evaluator = new ExpressionEvaluator();

            // NOTE: Numbers should always be decimals.

            object result = evaluator.Evaluate("4");
            Assert.AreEqual(4M, result);

            result = evaluator.Evaluate("-2");
            Assert.AreEqual(-2M, result);

            result = evaluator.Evaluate("4 + 3");
            Assert.AreEqual(7M, result);

            result = evaluator.Evaluate("5 * 3");
            Assert.AreEqual(15M, result);

            result = evaluator.Evaluate("2 - 7");
            Assert.AreEqual(-5M, result);

            result = evaluator.Evaluate("12 / 5");
            Assert.AreEqual(2.4M, result);

            result = evaluator.Evaluate("(4 + 5) * 2");
            Assert.AreEqual(18M, result);

            result = evaluator.Evaluate("'hello' + \" world\"");
            Assert.AreEqual("hello world", result);

            result = evaluator.Evaluate("4 > 6");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("6 > 6");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("6 >= 6");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("7 > 6");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("7 >= 6");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("4 < 6");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("4 < 4");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("4 <= 4");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("4 <= 6");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("6 <= 6");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("4 == 6");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("4 == 4");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("4 != 6");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("4 != 4");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("!(4 != 6)");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("!(4 != 4)");
            Assert.AreEqual(true, result);
        }
    }
}
