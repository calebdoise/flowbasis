using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowBasis.Expressions;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlowBasis.Configuration;
using System.IO;

namespace FlowBasisConfigurationUnitTests
{

    [TestClass]
    public class ConfigurationBuilderTests
    {
        [TestMethod]
        public void Test_Basic_Settings()
        {
            var configBuilder = new ConfigurationBuilder();

            // Direct addition of settings.
            configBuilder.AddSetting("hello", "world");
            configBuilder.AddSetting("blarg", 42);
            configBuilder.AddSetting("test", true);

            dynamic config = configBuilder.GetConfigurationObject();
            Assert.AreEqual("world", config.hello);
            Assert.AreEqual(42, config.blarg);
            Assert.AreEqual(true, config.test);
            Assert.IsNull(config.doesNotExist);

            // Command-line arg processing.
            configBuilder.AddCommandLineArgs(new[] { "--hello", "value1", "--v2", "value2", "--f1", "--v3", "value3", "--f2" });

            dynamic config2 = configBuilder.GetConfigurationObject();
            Assert.AreEqual("value1", config2.hello);
            Assert.AreEqual(42, config2.blarg);
            Assert.AreEqual(true, config2.test);
            Assert.IsNull(config2.doesNotExist);
            Assert.AreEqual("value2", config2.v2);
            Assert.AreEqual(true, config2.f1);
            Assert.AreEqual("value3", config2.v3);
            Assert.AreEqual(true, config2.f2);

            // Include JSON file.
            string projectPath = ProjectPath;
            string configPath = Path.Combine(projectPath, "Files", "SampleConfig1.json");
            configBuilder.AddJsonFile(configPath, throwIfNotExists: true);

            dynamic config3 = configBuilder.GetConfigurationObject();
            Assert.AreEqual("another new value", config3.hello);
            Assert.AreEqual(42, config3.blarg);
            Assert.AreEqual(true, config3.test);
            Assert.IsNull(config3.doesNotExist);
            Assert.AreEqual("value2", config3.v2);
            Assert.AreEqual(true, config3.f1);
            Assert.AreEqual("value3", config3.v3);
            Assert.AreEqual(true, config3.f2);
            Assert.AreEqual(34, config3.numberValue);
            Assert.AreEqual("test", config3.nestedObject.someValue);
            Assert.AreEqual(3, config3.nestedObject.someArray.Count);
            Assert.AreEqual("blah", config3.nestedObject.someArray[2]);

            Assert.AreEqual(configBuilder.BasePath, config3.theBasePath);
            Assert.AreEqual(Path.Combine(projectPath, "Files"), config3.theActiveBasePath);
        }

        [TestMethod]
        public void Test_Evaluation_With_Convert_Expressions()
        {
            var configBuilder = new ConfigurationBuilder();

            string expectedStr = "blargy blarg";
            string base64Utf8Str = Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedStr));

            // Direct addition of settings.
            configBuilder.AddSetting("hello", "eval:: convert.toBase64Utf8('blargy blarg')");
            configBuilder.AddSetting("blargFromBase64", $"eval:: convert.fromBase64Utf8('{base64Utf8Str}')");
            configBuilder.AddSetting("aNumber", "eval:: convert.toNumber('43')");

            dynamic config = configBuilder.GetConfigurationObject();
            Assert.AreEqual(base64Utf8Str, config.hello);
            Assert.AreEqual(expectedStr, config.blargFromBase64);
            Assert.AreEqual(43M, config.aNumber);

            // Test with --addJson command-line arg option.
            configBuilder = new ConfigurationBuilder();

            string argJson = FlowBasis.Json.JObject.Stringify(
                new Dictionary<string, object>()
                {
                    {
                        "sampleObj",
                        new Dictionary<string, object>()
                        {
                            { "keyExample", "someValue" },
                            { "c", "eval:: 4 + 2" }
                        }
                    }
                });
            string argJsonBase64Utf8Str = Convert.ToBase64String(Encoding.UTF8.GetBytes(argJson));

            configBuilder.AddCommandLineArgs(
                new[]
                {
                    "--addJson",
                    $"eval::convert.fromBase64Utf8('{argJsonBase64Utf8Str}')"
                });

            config = configBuilder.GetConfigurationObject();
            Assert.AreEqual("someValue", config.sampleObj.keyExample);
            Assert.AreEqual(6, config.sampleObj.c);

            // Test with --addJson command-line arg option where option is already an object.
            configBuilder = new ConfigurationBuilder();

            argJson = FlowBasis.Json.JObject.Stringify(
                new Dictionary<string, object>()
                {
                    {
                        "sampleObj2",
                        new Dictionary<string, object>()
                        {
                            { "keyExample2", "someValue2" },
                            { "c2", "eval:: 4 + 3" }
                        }
                    }
                });
            argJsonBase64Utf8Str = Convert.ToBase64String(Encoding.UTF8.GetBytes(argJson));

            configBuilder.AddCommandLineArgs(
                new[]
                {
                    "--addJson",
                    $"eval::json.parse(convert.fromBase64Utf8('{argJsonBase64Utf8Str}'))"
                });

            config = configBuilder.GetConfigurationObject();
            Assert.AreEqual("someValue2", config.sampleObj2.keyExample2);
            Assert.AreEqual(7, config.sampleObj2.c2);
        }

        [TestMethod]
        public void Test_Evaluation_Of_File_Content_Includes()
        {
            string projectPath = ProjectPath;

            // Direct addition of settings.
            var configBuilder = new ConfigurationBuilder();
            configBuilder.BasePath = projectPath;

            configBuilder.AddSetting("secret", "fileFirstLine::Files\\SampleSecret.txt");
            configBuilder.AddSetting("secretIfExists", "ifExistsFileFirstLine::Files\\SampleSecret.txt");
            configBuilder.AddSetting("secretDoesNotExist", "ifExistsFileFirstLine::DoesNotExist.txt");
            configBuilder.AddSetting("secret2DoesNotExist", "ifExistsFileAllText::DoesNotExist.txt");

            dynamic config = configBuilder.GetConfigurationObject();
            Assert.AreEqual("SampleSecretLine1", config.secret);
            Assert.AreEqual("SampleSecretLine1", config.secretIfExists);
            Assert.IsNull(config.secretDoesNotExist);
            Assert.IsNull(config.secret2DoesNotExist);


            // Through the command-line.
            var configBuilder2 = new ConfigurationBuilder();
            configBuilder2.BasePath = projectPath;

            configBuilder.AddCommandLineArgs(new[] { "--secret", "fileFirstLine::Files\\SampleSecret.txt" });
            configBuilder.AddCommandLineArgs(new[] { "--secret2", "fileAllText::Files\\SampleSecret.txt" });

            dynamic config2 = configBuilder.GetConfigurationObject();
            Assert.AreEqual("SampleSecretLine1", config2.secret);

            var secret2Lines = ((string)config2.secret2).Split("\n").Select(s => s.Trim()).ToArray();
            Assert.AreEqual(2, secret2Lines.Length);
            Assert.AreEqual("SampleSecretLine1", secret2Lines[0]);
            Assert.AreEqual("SampleSecretLine2", secret2Lines[1]);


            // Ensure that missing files cause exceptions.
            Exception caughtEx = null;
            try
            {
                configBuilder.AddSetting("secret", "fileFirstLine::DoesNotExist.txt");
            }
            catch (Exception ex)
            {
                caughtEx = ex;
            }
            Assert.IsNotNull(caughtEx);

            caughtEx = null;
            try
            {
                configBuilder.AddSetting("secret", "fileAllText::DoesNotExist.txt");
            }
            catch (Exception ex)
            {
                caughtEx = ex;
            }
            Assert.IsNotNull(caughtEx);


            // Through add file.
            var configBuilder3 = new ConfigurationBuilder();
            configBuilder3.BasePath = projectPath;

            configBuilder3.AddJsonFile("Files\\SampleConfig2.json");

            dynamic config3 = configBuilder3.GetConfigurationObject();

            Assert.AreEqual(Environment.GetEnvironmentVariable("CommonProgramFiles"), config3.computedValue);
            Assert.AreEqual(8M, config3.computedValueSumTo8);
            Assert.AreEqual("SampleSecretLine1", config3.firstSecret);
            Assert.AreEqual("hello42", config3.nestedObject.childArray[0].nestedComputedValue);

            Assert.AreEqual(13, config3.jsonObj.field1);
            Assert.AreEqual("{\"field1\":13}", config3.jsonStr);
            Assert.AreEqual(12, config3.jsonMerged.a);
            Assert.AreEqual(4, config3.jsonMerged.b);
            Assert.AreEqual(6, config3.jsonMerged.c);

            // Through add file with command line.
            var configBuilder4 = new ConfigurationBuilder();
            configBuilder4.BasePath = projectPath;

            configBuilder4.AddCommandLineArgs(new[] { "--addJsonFile", "Files\\SampleConfig2.json" });

            dynamic config4 = configBuilder4.GetConfigurationObject();

            Assert.AreEqual(Environment.GetEnvironmentVariable("CommonProgramFiles"), config4.computedValue);
            Assert.AreEqual(8M, config4.computedValueSumTo8);
            Assert.AreEqual("SampleSecretLine1", config4.firstSecret);
            Assert.AreEqual("hello42", config4.nestedObject.childArray[0].nestedComputedValue);

            // And try the strongly typed configuration.
            SampleConfig2Class typedConfig4 = configBuilder4.GetConfigurationObject<SampleConfig2Class>();
            Assert.AreEqual("SampleSecretLine1", typedConfig4.FirstSecret);
            Assert.AreEqual(8M, typedConfig4.ComputedValueSumTo8);
        }


        [TestMethod]
        public void Test_Evaluation_Of_Config_References()
        {
            string projectPath = ProjectPath;

            // Direct addition of settings.
            var configBuilder = new ConfigurationBuilder();
            configBuilder.BasePath = projectPath;

            configBuilder.AddSetting("myValue", 34);
            configBuilder.AddSetting("myValue2", "eval:: config.myValue * 2");

            object complexObj = new FlowBasis.Json.JObject();
            ((dynamic)complexObj).foo = "hello";
            configBuilder.AddSetting("complex", complexObj);
            configBuilder.AddSetting("complexFoo", "eval:: config.complex.foo + ' world'");

            dynamic config = configBuilder.GetConfigurationObject();
            Assert.AreEqual(34M, config.myValue);
            Assert.AreEqual(68M, config.myValue2);
            Assert.AreEqual("hello world", config.complexFoo);
        }

        [TestMethod]
        public void Test_Json_Includes()
        {
            string projectPath = ProjectPath;

            var configBuilder = new ConfigurationBuilder();
            configBuilder.BasePath = projectPath;

            configBuilder.AddJsonFile("Files/SampleConfigWithInclude.json");

            dynamic config = configBuilder.GetConfigurationObject();
            Assert.AreEqual("this is my imported value", config.includedFromAnotherFile);
            Assert.AreEqual("here", config.yetAnotherValue);

            var filesIncluded = configBuilder.GetListOfFilesIncluded();
            Assert.AreEqual(2, filesIncluded.Count);
            Assert.IsTrue(filesIncluded[0].EndsWith("SampleConfigWithInclude.json"));
            Assert.IsTrue(filesIncluded[1].EndsWith("SampleConfigToInclude.json"));
        }

        [TestMethod]
        public void Test_Json_Include_Circular_Loop()
        {
            string projectPath = ProjectPath;

            var configBuilder = new ConfigurationBuilder();
            configBuilder.BasePath = projectPath;

            Exception caughtEx = null;
            try
            {
                configBuilder.AddJsonFile("Files/Loop1.json");
            }
            catch (Exception ex)
            {
                caughtEx = ex;
            }

            Assert.IsNotNull(caughtEx);
            Assert.IsTrue(caughtEx.Message.Contains("loop detected"));
            Assert.IsInstanceOfType(caughtEx, typeof(ConfigurationFileIncludeLoopException));
            Assert.IsTrue(((ConfigurationFileIncludeLoopException)caughtEx).FileIncludeStack.Length > 0);
        }


        [TestMethod]
        public void Test_Json_Include_Str_Eval_Error()
        {
            string projectPath = ProjectPath;

            var configBuilder = new ConfigurationBuilder();
            configBuilder.BasePath = projectPath;

            Exception caughtEx = null;
            try
            {
                configBuilder.AddJsonFile("Files/StrEvalError1.json");
            }
            catch (Exception ex)
            {
                caughtEx = ex;
            }

            Assert.IsNotNull(caughtEx);
            Assert.IsTrue(caughtEx.Message.Contains("String evaluation failed"));
            Assert.IsInstanceOfType(caughtEx, typeof(ConfigurationException));

            var configEx = (ConfigurationException)caughtEx;
            Assert.IsTrue(configEx.File.Contains("StrEvalError1.json"));
            Assert.AreEqual("eval:: (3 + 4", configEx.StringExpression);
            Assert.AreEqual(4, configEx.PropertyPath.Length);
            Assert.AreEqual("root", configEx.PropertyPath[0]);
            Assert.AreEqual("blarg", configEx.PropertyPath[1]);
            Assert.AreEqual("[2]", configEx.PropertyPath[2]);
            Assert.AreEqual("p2", configEx.PropertyPath[3]);
        }



        private class SampleConfig2Class
        {
            public string FirstSecret { get; set; }
            public decimal ComputedValueSumTo8 { get; set; }
        }

        public static string ProjectPath
        {
            get
            {
                var codeBase = typeof(ConfigurationBuilderTests).Assembly.CodeBase;
                string assemblyPath = new Uri(codeBase).LocalPath;
                string assemblyFolder = Path.GetDirectoryName(assemblyPath);

                string projectFolderPath = assemblyFolder;
                while ((projectFolderPath = Path.GetDirectoryName(projectFolderPath)) != null)
                {
                    string csProjPath = Path.Combine(projectFolderPath, "FlowBasisConfigurationUnitTests.csproj");
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
