using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Configuration
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException()
        {
        }

        public ConfigurationException(string message) : base(message)
        {
        }

        public ConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }


        public string[] PropertyPath { get; internal set; }

        public string File { get; internal set; }

        public string[] FileIncludeStack { get; internal set; }


        /// <summary>
        /// If exception was caused by evaluation of a string expression, the expression may be included here.
        /// </summary>
        public string StringExpression { get; internal set; }
    }
}
