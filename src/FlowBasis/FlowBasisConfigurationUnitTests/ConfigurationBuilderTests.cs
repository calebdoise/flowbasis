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
            Assert.AreEqual(null, config.doesNotExist);

            // Command-line arg processing.
            configBuilder.AddCommandLineArgs(new[] { "--hello", "value1", "--v2", "value2", "--f1", "--v3", "value3", "--f2" });

            dynamic config2 = configBuilder.GetConfigurationObject();
            Assert.AreEqual("value1", config2.hello);
            Assert.AreEqual(42, config2.blarg);
            Assert.AreEqual(true, config2.test);
            Assert.AreEqual(null, config2.doesNotExist);
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
            Assert.AreEqual(null, config3.doesNotExist);
            Assert.AreEqual("value2", config3.v2);
            Assert.AreEqual(true, config3.f1);
            Assert.AreEqual("value3", config3.v3);
            Assert.AreEqual(true, config3.f2);
            Assert.AreEqual(34, config3.numberValue);
            Assert.AreEqual("test", config3.nestedObject.someValue);
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
