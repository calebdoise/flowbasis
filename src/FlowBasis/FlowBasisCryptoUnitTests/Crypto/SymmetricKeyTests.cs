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
    public class SymmetricKeyTests
    {

        [TestMethod]
        public void Should_Rountrip_Encrypted_Strings()
        {
            TestRoundtripEncryption(SymmetricKeyType.AES, "hello#world!", 128, "Hello, world!");
            TestRoundtripEncryption(SymmetricKeyType.AES, "hello#world! 2", 256, "Hello, world! Again");
            TestRoundtripEncryptionWithRandomKey(SymmetricKeyType.AES, 128, "Hello, world! 22");
            TestRoundtripEncryptionWithRandomKey(SymmetricKeyType.AES, 256, "Hello, world! Again 22");
  
            TestRoundtripEncryption(SymmetricKeyType.TripleDES, "hello#world! 5", 128, "Hello, world! 3");            

            TestRoundtripEncryption(SymmetricKeyType.Rijndael, "hello#world! 7", 128, "Hello, world! 4");
            TestRoundtripEncryption(SymmetricKeyType.Rijndael, "hello#world! 8", 256, "Hello, world! Again 4");
        }


        private void TestRoundtripEncryption(SymmetricKeyType keyType, string password, int keySize, string phrase)
        {
            var key = SymmetricKey.FromPassword(keyType, password, keySize);
            var iv = key.GenerateIV();

            byte[] data = Encoding.UTF8.GetBytes(phrase);
            byte[] encryptedData = key.Encrypt(data, iv);

            var decryptionKey = SymmetricKey.FromPassword(keyType, password, keySize);
            byte[] decryptedData = decryptionKey.Decrypt(encryptedData, iv);

            string decryptedPhrase = Encoding.UTF8.GetString(decryptedData);

            Assert.AreEqual(phrase, decryptedPhrase);
        }

        private void TestRoundtripEncryptionWithRandomKey(SymmetricKeyType keyType, int keySize, string phrase)
        {
            byte[] keyBytes = SecureRandomBytes.GetRandomBytesFromBitSize(keySize);

            var key = new SymmetricKey(keyType, keyBytes);
            var iv = key.GenerateIV();

            byte[] data = Encoding.UTF8.GetBytes(phrase);
            byte[] encryptedData = key.Encrypt(data, iv);

            var decryptionKey = new SymmetricKey(keyType, keyBytes);
            byte[] decryptedData = decryptionKey.Decrypt(encryptedData, iv);

            string decryptedPhrase = Encoding.UTF8.GetString(decryptedData);

            Assert.AreEqual(phrase, decryptedPhrase);
        }
    }
}
