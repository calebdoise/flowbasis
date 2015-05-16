using FlowBasis.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasisCryptoUnitTests.Crypto
{
    [TestClass]
    public class PasswordGeneratorTests
    {
        [TestMethod]
        public void Generated_Passwords_Sanity_Check()
        {
            string pass1 = PasswordGenerator.NewPassword();
            string pass2 = PasswordGenerator.NewPassword();

            Assert.AreNotEqual(pass1, pass2);
            Assert.AreEqual(20, pass1.Length, "Default length is 20");
            Assert.AreEqual(20, pass2.Length, "Default length is 20");

            string pass3 = PasswordGenerator.NewPassword(40);
            Assert.AreEqual(40, pass3.Length);
        }
    }
}
