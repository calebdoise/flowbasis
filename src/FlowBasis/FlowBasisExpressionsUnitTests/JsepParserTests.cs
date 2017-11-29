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
    public class JsepParserTests
    {
        [TestMethod]
        public void Should_Return_Null_For_Missing_Value()
        {
            var jsepParser = new JsepParser();
            object result = jsepParser.Parse("3 + 4");
            Assert.IsNotNull(result);
        }
      
        
    }

}
