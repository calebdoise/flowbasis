using System;
using System.Collections.Generic;
using System.Text;

namespace FlowBasis.Configuration
{
    public class ConfigurationFileIncludeLoopException : ConfigurationException
    {
        public ConfigurationFileIncludeLoopException()
        {
        }

        public ConfigurationFileIncludeLoopException(string message) : base(message)
        {
        }

        public ConfigurationFileIncludeLoopException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ConfigurationFileIncludeLoopException(string message, string[] fileIncludeStack) : base(message)
        {
            this.FileIncludeStack = fileIncludeStack;
        }
        
    }
}
