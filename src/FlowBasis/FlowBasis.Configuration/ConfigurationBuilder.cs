using FlowBasis.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FlowBasis.Configuration
{
    public class ConfigurationBuilder
    {       
        private string basePath;

        private object syncObject = new object();
        private JObject configObject = new JObject();

        public ConfigurationBuilder()
        {
            this.basePath = Environment.CurrentDirectory;
        }

        public void AddSetting(string name, object value)
        {
            lock (this.syncObject)
            {
                this.configObject[name] = value;
            }
        }

        public void AddCommandLineArgs(string[] args)
        {
            const string argNamePrefix = "--";
        
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
                        string value = arg;
                        this.AddSetting(lastArgName, value);
                        lastArgName = null;
                    }
                }
            }

            if (lastArgName != null)
            {
                this.AddSetting(lastArgName, true);
            }
        }

        public void AddSettings(IDictionary<string, object> settings)
        {
            foreach (var pair in settings)
            {
                this.AddSetting(pair.Key, pair.Value);
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

                if (result is IDictionary<string, object> resultDictionary)
                {
                    this.AddSettings(resultDictionary);
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
    }
}
