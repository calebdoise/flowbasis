using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowBasis.Expressions;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlowBasis.Expressions.Extensions;
using System.IO;

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

            // &&
            result = evaluator.Evaluate("false && false");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("true && false");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("false && true");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("true && true");
            Assert.AreEqual(true, result);

            // ||
            result = evaluator.Evaluate("false || false");
            Assert.AreEqual(false, result);

            result = evaluator.Evaluate("true || false");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("false || true");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("true || true");
            Assert.AreEqual(true, result);
        }


        [TestMethod]
        public void Test_String_Members()
        {
            var evaluator = new ExpressionEvaluator();

            object result = evaluator.Evaluate("('hello world').length");
            Assert.AreEqual(11, result);

            result = evaluator.Evaluate("('hello world').indexOf('o')");
            Assert.AreEqual(4, result);

            result = evaluator.Evaluate("('hello world').lastIndexOf('o')");
            Assert.AreEqual(7, result);

            result = evaluator.Evaluate("('hello world').substring(5)");
            Assert.AreEqual(" world", result);

            result = evaluator.Evaluate("('hello world').substring(5, 2)");
            Assert.AreEqual(" w", result);

            result = evaluator.Evaluate("('hello world').substring(5, 10000)");
            Assert.AreEqual(" world", result);

            result = evaluator.Evaluate("('hello world').after('el')");
            Assert.AreEqual("lo world", result);

            result = evaluator.Evaluate("('hello world').after('o')");
            Assert.AreEqual(" world", result);

            result = evaluator.Evaluate("('hello world').afterLast('o')");
            Assert.AreEqual("rld", result);
        }


        [TestMethod]
        public void Test_Evaluation_With_Scope()
        {
            string projectPath = ProjectPath;
            var env = new EnvironmentVariableMemberProvider();
            var fileAccess = new FileSystemExpressionCallable(() => projectPath);

            Func<string, object> identifierResolver = (name) =>
            {
                switch (name)
                {
                    case "env": return env;
                    case "file": return fileAccess;
                    default: return null;
                }
            };

            var evaluator = new ExpressionEvaluator(new StandardExpressionScope(null, identifierResolver));

            // env tests
            object result = evaluator.Evaluate("env.SystemRoot");
            Assert.AreEqual(Environment.GetEnvironmentVariable("SystemRoot"), result);

            result = evaluator.Evaluate("env['SystemRoot']");
            Assert.AreEqual(Environment.GetEnvironmentVariable("SystemRoot"), result);

            // file tests
            result = evaluator.Evaluate("file('Files/SampleFile.txt').exists");
            Assert.AreEqual(true, result);

            result = evaluator.Evaluate("file('Files/SampleFile.txt').firstLine");
            Assert.AreEqual("HelloLine1", result);

            result = evaluator.Evaluate("file('Files/SampleFile.txt').firstLineIfExists");
            Assert.AreEqual("HelloLine1", result);

            result = evaluator.Evaluate("file('Files/SampleFileDoesNotExist.txt').firstLineIfExists");
            Assert.AreEqual(null, result);

            result = evaluator.Evaluate("file('Files/SampleFile.txt').allText");
            var sampleLines = ((string)result).Split("\n").Select(s => s.Trim()).ToArray();
            Assert.AreEqual(2, sampleLines.Length);
            Assert.AreEqual("HelloLine1", sampleLines[0]);
            Assert.AreEqual("HelloLine2", sampleLines[1]);

            result = evaluator.Evaluate("file('Files/SampleFile.txt').allTextIfExists");
            sampleLines = ((string)result).Split("\n").Select(s => s.Trim()).ToArray();
            Assert.AreEqual(2, sampleLines.Length);
            Assert.AreEqual("HelloLine1", sampleLines[0]);
            Assert.AreEqual("HelloLine2", sampleLines[1]);

            result = evaluator.Evaluate("file('Files/SampleFileDoesNotExist.txt').allTextIfExists");
            Assert.AreEqual(null, result);


            // Test the nested variable holder temp scope.
            result = evaluator.Evaluate("temp.setValue('keyName', 4.5); temp.keyName");
            Assert.AreEqual(4.5M, result);

            result = evaluator.Evaluate("temp.setValue('keyName', 4.25); temp.setValue('keyName', 2 * temp.keyName); temp.keyName");
            Assert.AreEqual(8.5M, result);

            // Ensure temp variable does not carry over to next evaluation.
            result = evaluator.Evaluate("temp.keyName");
            Assert.AreEqual(null, result);
        }
    


        public static string ProjectPath
        {
            get
            {
                var codeBase = typeof(ExpressionEvaluatorTests).Assembly.CodeBase;
                string assemblyPath = new Uri(codeBase).LocalPath;
                string assemblyFolder = Path.GetDirectoryName(assemblyPath);

                string projectFolderPath = assemblyFolder;
                while ((projectFolderPath = Path.GetDirectoryName(projectFolderPath)) != null)
                {
                    string csProjPath = Path.Combine(projectFolderPath, "FlowBasisExpressionsUnitTests.csproj");
                    if (File.Exists(csProjPath))
                    {
                        return projectFolderPath;
                    }
                }

                throw new Exception("Project folder not found.");
            }
        }
    }
}
