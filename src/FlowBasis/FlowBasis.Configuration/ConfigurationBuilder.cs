using FlowBasis.Expressions;
using FlowBasis.Expressions.Extensions;
using FlowBasis.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace FlowBasis.Configuration
{
    public class ConfigurationBuilder
    {
        private string basePath;

        private object syncObject = new object();
        private JObject configObject = new JObject();

        private ExpressionEvaluator expressionEvaluator;
        private EnvironmentVariableMemberProvider environmentVariableMemberProvider;
        private FileSystemExpressionCallable fileSystemExpressionCallable;


        public ConfigurationBuilder()
        {
            this.basePath = Environment.CurrentDirectory;

            // Setup expression evaluator.
            this.environmentVariableMemberProvider = new EnvironmentVariableMemberProvider();
            this.fileSystemExpressionCallable = new FileSystemExpressionCallable(() => this.basePath);

            this.expressionEvaluator = new ExpressionEvaluator(
                new StandardExpressionScope(this, this.InternalExpressionIdentifierProvider));            
        }

        public string BasePath
        {
            get { return this.basePath; }
            set { this.basePath = value; }
        }

        private object InternalExpressionIdentifierProvider(string name)
        {
            switch (name)
            {
                case "env": return this.environmentVariableMemberProvider;
                case "file": return this.fileSystemExpressionCallable;
                default: return null;
            }

            throw new Exception($"Unknown expression identifier: {name}");
        }

        public void AddSetting(string name, object value, bool suppressEvaluation = false)
        {
            lock (this.syncObject)
            {
                if (suppressEvaluation)
                {
                    this.configObject[name] = value;
                }
                else
                {
                    object processedValue = this.ProcessSettingValue(value);
                    this.configObject[name] = processedValue;
                }
            }
        }

        public void AddCommandLineArgs(string[] args)
        {
            const string argNamePrefix = "--";
            const string addJsonFileArgName = "addJsonFile";

            string lastArgName = null;

            foreach (string arg in args)
            {
                if (arg.StartsWith(argNamePrefix))
                {
                    if (lastArgName != null)
                    {
                        this.AddSetting(lastArgName, true);
                    }

                    lastArgName = arg.Substring(argNamePrefix.Length);
                }
                else
                {
                    if (lastArgName != null)
                    {
                        string value = this.ProcessSettingValue(arg) as string;

                        if (lastArgName == addJsonFileArgName)
                        {
                            this.AddJsonFile(value);
                        }
                        else
                        {
                            this.AddSetting(lastArgName, value, suppressEvaluation: true);
                        }

                        lastArgName = null;
                    }
                }
            }

            if (lastArgName != null)
            {
                this.AddSetting(lastArgName, true);
            }
        }

        public void AddSettings(IDictionary<string, object> settings, bool suppressEvaluation = false)
        {
            foreach (var pair in settings)
            {
                this.AddSetting(pair.Key, pair.Value, suppressEvaluation: suppressEvaluation);
            }
        }

        public void AddJsonFile(string path, bool throwIfNotExists = false)
        {
            string fullPath;
            if (Path.IsPathRooted(path))
            {
                fullPath = path;
            }
            else
            {
                fullPath = Path.Combine(this.basePath, path);
            }

            if (File.Exists(fullPath))
            {
                string json = File.ReadAllText(fullPath);
                object result = JObject.Parse(json);

                object processedResult = this.ProcessSettingValue(result);

                if (processedResult is IDictionary<string, object> resultDictionary)
                {
                    this.AddSettings(resultDictionary, suppressEvaluation: true);
                }
            }
            else
            {
                if (throwIfNotExists)
                {
                    throw new Exception($"Configuration file not found: {path}");
                }
            }
        }

        /// <summary>
        /// Retrieve the finalized configuration object.
        /// </summary>
        /// <returns></returns>
        public JObject GetConfigurationObject()
        {
            lock (this.syncObject)
            {
                string json = FlowBasis.Json.JsonSerializers.Default.Stringify(this.configObject);
                var clonedConfigObject = FlowBasis.Json.JsonSerializers.Default.Parse(json) as JObject;
                return clonedConfigObject;
            }
        }


        /// <summary>
        /// Retrieve the finalized configuration object as strongly typed object.
        /// </summary>
        /// <returns></returns>
        public T GetConfigurationObject<T>()
        {
            JObject clonedConfigObject = this.GetConfigurationObject();
            T typedConfigObject = FlowBasis.Json.JsonSerializers.Default.Map<T>(clonedConfigObject);
            return typedConfigObject;
        }


        protected virtual object ProcessSettingValue(object value)
        {
            if (value is string strValue)
            {
                return this.EvaluateString(strValue);
            }
            else if (value is IDictionary<string, object> dictionary)
            {
                var jObject = new JObject();
                foreach (var pair in dictionary)
                {
                    var processedValue = this.ProcessSettingValue(pair.Value);
                    jObject[pair.Key] = processedValue;
                }
                return jObject;
            }
            else if (value is IEnumerable enumerable)
            {
                var arrayList = new ArrayList();
                foreach (var entry in enumerable)
                {
                    var processedEntry = this.ProcessSettingValue(entry);
                    arrayList.Add(processedEntry);
                }
                return arrayList;
            }

            return value;
        }

        protected virtual object EvaluateString(string strValue)
        {
            const string fileFirstLinePrefix = "fileFirstLine::";
            const string fileAllTextPrefix = "fileAllText::";
            const string ifExistsFileFirstLinePrefix = "ifExistsFileFirstLine::";
            const string ifExistsFileAllTextPrefix = "ifExistsFileAllText::";
            const string evalPrefix = "eval::";

            string GetFileFirstLine(string path)
            {
                using (var fs = OpenFileForSharedRead(path))
                using (var reader = new StreamReader(fs))
                {
                    string line = reader.ReadLine();
                    return line;
                }
            }

            string GetFileAllText(string path)
            {
                using (var fs = OpenFileForSharedRead(path))
                using (var reader = new StreamReader(fs))
                {
                    string allText = reader.ReadToEnd();
                    return allText;
                }
            }

            if (strValue == null)
            {
                return null;
            }
            else if (strValue.StartsWith(fileFirstLinePrefix))
            {
                string path = strValue.Substring(fileFirstLinePrefix.Length);
                return GetFileFirstLine(path);
            }
            else if (strValue.StartsWith(fileAllTextPrefix))
            {
                string path = strValue.Substring(fileAllTextPrefix.Length);
                return GetFileAllText(path);
            }
            else if (strValue.StartsWith(ifExistsFileFirstLinePrefix))
            {
                string path = this.EvaluateFullFilePath(strValue.Substring(ifExistsFileFirstLinePrefix.Length));
                if (File.Exists(path))
                {
                    return GetFileFirstLine(path);
                }
                else
                {
                    return null;
                }
            }
            else if (strValue.StartsWith(ifExistsFileAllTextPrefix))
            {
                string path = this.EvaluateFullFilePath(strValue.Substring(ifExistsFileAllTextPrefix.Length));
                if (File.Exists(path))
                {
                    return GetFileAllText(path);
                }
                else
                {
                    return null;
                }
            }
            else if (strValue.StartsWith(evalPrefix))
            {
                string expression = strValue.Substring(evalPrefix.Length);
                object result = this.expressionEvaluator.Evaluate(expression);
                return result;
            }

            return strValue;
        }


        private string EvaluateFullFilePath(string path)
        {
            string fullPath;
            if (Path.IsPathRooted(path))
            {
                fullPath = path;
            }
            else
            {
                fullPath = Path.Combine(this.basePath, path);
            }

            return fullPath;
        }

        private FileStream OpenFileForSharedRead(string path)
        {
            string fullPath = this.EvaluateFullFilePath(path);
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }        
    }
}
