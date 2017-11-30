using FlowBasis.Json;
using System;
using System.IO;

namespace FlowBasis.Configuration
{
    public class ConfigurationBuilder
    {       
        private string basePath;

        public ConfigurationBuilder()
        {
            this.basePath = Environment.CurrentDirectory;
        }

        public void AddCommandLineArgs(string[] args)
        {
            throw new NotImplementedException();
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

                if (result is JObject resultJObject)
                {
                    // TODO: Process the settings.
                    throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
