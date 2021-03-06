﻿using FlowBasis.Expressions;
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

        /// <summary>
        /// Maintain list of files included while building settings (could allow for monitoring of those files to reload configuration).
        /// </summary>
        private List<string> filesIncluded = new List<string>();

        /// <summary>
        /// Maintain list of stack of file includes currently being processed to allow us to check for circular loops.
        /// </summary>
        private Stack<string> fileIncludeStack = new Stack<string>();

        private ExpressionEvaluator expressionEvaluator;
        private IDictionary<string, object> expressionIdentifiers;


        public ConfigurationBuilder()
        {
            this.basePath = Environment.CurrentDirectory;
            
            this.expressionIdentifiers = new Dictionary<string, object>();
            this.expressionEvaluator = this.SetupExpressionEvaluator();
        }

        public string BasePath
        {
            get { return this.basePath; }
            set { this.basePath = value; }
        }

        public IDictionary<string, object> ExpressionIdentifiers
        {
            get { return this.expressionIdentifiers; }
        }

        protected virtual ExpressionEvaluator SetupExpressionEvaluator()
        {
            var environmentVariableMemberProvider = new EnvironmentVariableMemberProvider();
            var fileSystemExpressionCallable = new FileSystemExpressionCallable(() => this.GetActiveBasePath());
            var configMemberProvider = new ConfigMemberProvider(this);
            var jsonMemberProvider = new JsonMemberProvider();
            var convertMemberProvider = new ConvertMemberProvider();

            var evaluator = new ExpressionEvaluator(
                new StandardExpressionScope(this, this.GetExpressionIdentifierValue));

            this.expressionIdentifiers["env"] = environmentVariableMemberProvider;
            this.expressionIdentifiers["file"] = fileSystemExpressionCallable;
            this.expressionIdentifiers["config"] = configMemberProvider;
            this.expressionIdentifiers["json"] = jsonMemberProvider;
            this.expressionIdentifiers["convert"] = convertMemberProvider;

            return evaluator;
        }

        protected virtual object GetExpressionIdentifierValue(string name)
        {
            switch (name)
            {
                case "basePath": return this.basePath;
                case "activeBasePath": return this.GetActiveBasePath();
            }

            if (this.expressionIdentifiers.TryGetValue(name, out object value))
            {
                return value;
            }            

            throw new Exception($"Unknown expression identifier: {name}");
        }

        public virtual void PerformAction(string actionName, string actionValue, string sourceFilePath = null)
        {
            string InterpretActionValueAsFilePath()
            {
                if (sourceFilePath == null)
                {
                    return actionValue;
                }
                else
                {
                    string sourceFileDirectory = Path.GetDirectoryName(sourceFilePath);
                    string actualPath = this.EvaluateFullFilePath(actionValue, basePathOverride: sourceFileDirectory);
                    return actualPath;
                }
            }

            switch (actionName)
            {
                case "addJsonFile":
                    {
                        this.AddJsonFile(InterpretActionValueAsFilePath(), throwIfNotExists: true);
                        return;
                    }

                case "addJsonFileIfExists":
                    {
                        this.AddJsonFile(InterpretActionValueAsFilePath(), throwIfNotExists: false);
                        return;
                    }
            }

            throw new Exception($"Unknown action: {actionName}");
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
            const string addJsonArgName = "addJson";

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
                        object valueObj = this.ProcessSettingValue(arg);
                        string value = valueObj as string;

                        if (lastArgName == addJsonFileArgName)
                        {
                            this.AddJsonFile(value);
                        }
                        else if (lastArgName == addJsonArgName)
                        {                            
                            if (valueObj is IDictionary<string, object> valueObjDictionary)
                            {                                
                                this.AddSettings(valueObjDictionary, suppressEvaluation: false);
                            }
                            else if (value != null)
                            {
                                var extraSettingsJObject = FlowBasis.Json.JObject.Parse(value) as FlowBasis.Json.JObject;
                                this.AddSettings(extraSettingsJObject, suppressEvaluation: false);
                            }                            
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

            string[] fileIncludeStackBeforeThisFile;

            lock (this.syncObject)
            {
                fileIncludeStackBeforeThisFile = this.fileIncludeStack.ToArray();

                // Check to see if we have a circular loop of include files.
                if (this.fileIncludeStack.Contains(fullPath))
                {
                    throw new ConfigurationFileIncludeLoopException($"Circular loop detected in file includes: {fullPath}", this.fileIncludeStack.ToArray())
                    {
                        File = fullPath
                    };
                }
                
                this.fileIncludeStack.Push(fullPath);
            }

            try
            {
                bool doesNotExist;
                string json = this.ObtainConfigurationFileText(fullPath, fileIncludeStackBeforeThisFile, out doesNotExist);

                if (doesNotExist)
                {
                    if (throwIfNotExists)
                    {
                        throw new ConfigurationException($"Configuration file not found: {path}")
                        {
                            File = fullPath
                        };
                    }
                }
                else
                {
                    lock (this.syncObject)
                    {
                        this.filesIncluded.Add(fullPath);
                    }

                    try
                    {
                        object result = JObject.Parse(json);

                        object processedResult = this.ProcessSettingValue(result);

                        if (processedResult is JObject jObject)
                        {
                            object actions = jObject["__actions"];
                            if (actions != null)
                            {
                                jObject.Remove("__actions");

                                if (actions is IEnumerable enumerable)
                                {
                                    foreach (object action in enumerable)
                                    {
                                        var actionJObject = action as JObject;
                                        if (actionJObject != null)
                                        {
                                            string actionName = actionJObject["name"] as string;
                                            string actionValue = actionJObject["value"] as string;

                                            if (!String.IsNullOrWhiteSpace(actionName) && !String.IsNullOrWhiteSpace(actionValue))
                                            {
                                                this.PerformAction(actionName, actionValue, sourceFilePath: fullPath);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (processedResult is IDictionary<string, object> resultDictionary)
                        {
                            this.AddSettings(resultDictionary, suppressEvaluation: true);
                        }
                    }
                    catch (ConfigurationException configEx)
                    {
                        if (configEx.File == null)
                        {
                            configEx.File = fullPath;
                            configEx.FileIncludeStack = fileIncludeStackBeforeThisFile;
                        }

                        throw configEx;
                    }
                    catch (Exception ex)
                    {                        
                        throw new ConfigurationException($"Error in configuration file: {fullPath}: {ex.Message}", ex)
                        {
                            File = fullPath,
                            FileIncludeStack = fileIncludeStackBeforeThisFile
                        };
                    }
                }
            }            
            finally
            {
                lock (this.syncObject)
                {
                    this.fileIncludeStack.Pop();
                }
            }           
        }


        public virtual string ObtainConfigurationFileText(string fullPath, string[] fileIncludeStack, out bool doesNotExist)
        {
            if (File.Exists(fullPath))
            {
                doesNotExist = false;
                return File.ReadAllText(fullPath);
            }
            else
            {
                doesNotExist = true;
                return null;
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

        public IList<string> GetListOfFilesIncluded()
        {
            lock (this.syncObject)
            {
                var copy = new List<string>();
                copy.AddRange(this.filesIncluded);
                return copy;
            }
        }


        protected virtual object ProcessSettingValue(object value, Stack<string> propertyPathStack = null)
        {
            if (propertyPathStack == null)
            {
                propertyPathStack = new Stack<string>();
            }

            if (value is string strValue)
            {
                try
                {
                    return this.EvaluateString(strValue);
                }
                catch (Exception ex)
                {
                    string[] propertyPathStackArray = propertyPathStack.ToArray();
                    Array.Reverse(propertyPathStackArray);

                    throw new ConfigurationException($"String evaluation failed: {ex.Message}", ex)
                    {
                        StringExpression = strValue,
                        PropertyPath = propertyPathStackArray
                    };
                }
            }
            else if (value is IDictionary<string, object> dictionary)
            {
                var jObject = new JObject();
                foreach (var pair in dictionary)
                {
                    object processedValue = null;
                    try
                    {
                        propertyPathStack.Push(pair.Key);
                        processedValue = this.ProcessSettingValue(pair.Value, propertyPathStack);
                    }
                    finally
                    {
                        propertyPathStack.Pop();
                    }

                    jObject[pair.Key] = processedValue;
                }
                return jObject;
            }
            else if (value is IEnumerable enumerable)
            {
                int index = 0;
                var arrayList = new ArrayList();
                foreach (var entry in enumerable)
                {
                    object processedEntry = null;
                    try
                    {
                        propertyPathStack.Push("[" + index + "]");
                        processedEntry = this.ProcessSettingValue(entry, propertyPathStack);
                    }
                    finally
                    {
                        propertyPathStack.Pop();
                    }
                    
                    arrayList.Add(processedEntry);

                    index++;
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


        private string EvaluateFullFilePath(string path, string basePathOverride = null)
        {
            string fullPath;
            if (Path.IsPathRooted(path))
            {
                fullPath = path;
            }
            else
            {
                string basePathToUse = basePathOverride ?? this.GetActiveBasePath();
                fullPath = Path.Combine(basePathToUse, path);
            }

            return fullPath;
        }

        private FileStream OpenFileForSharedRead(string path)
        {
            string fullPath = this.EvaluateFullFilePath(path);
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }        


        /// <summary>
        /// Determine the base path to use based on file include being processed if possible.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetActiveBasePath()
        {
            lock (this.syncObject)
            {
                if (this.fileIncludeStack.Count > 0)
                {
                    string top = this.fileIncludeStack.Peek();
                    string activeBasePath = Path.GetDirectoryName(top);
                    return activeBasePath;
                }
                else
                {
                    return this.basePath;
                }
            }
        }


        private class ConfigMemberProvider : IExpressionMemberProvider
        {
            private ConfigurationBuilder configBuilder;

            public ConfigMemberProvider(ConfigurationBuilder configBuilder)
            {
                this.configBuilder = configBuilder;
            }

            public object EvaluateMember(string name)
            {
                lock (this.configBuilder.syncObject)
                {
                    object value = this.configBuilder.configObject[name];
                    return value;
                }
            }
        }
    }
}
