using FlowBasis.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasisCryptoUnitTests.Crypto
{
    [TestClass]
    public class RsaAsymmetricKeyTests
    {
        [TestMethod]
        public void Dynamic_Rsa_Key_Should_Verify_Signatures()
        {
            var rsaKey = RsaAsymmetricKeyPair.CreateKey(2048);
            var rsaPublicKey = RsaPublicKey.FromXmlString(rsaKey.PublicKeyXmlString);         

            byte[] dataToSign = new byte[500];
            var random = new Random(23421);
            random.NextBytes(dataToSign);

            byte[] signature = rsaKey.SignData(dataToSign, DigestType.SHA1);
            byte[] md5Signature = rsaKey.SignData(dataToSign, DigestType.MD5);

            // Verify that full key and verify its own signature.
            bool verified = rsaKey.VerifyData(dataToSign, DigestType.SHA1, signature);
            Assert.AreEqual(true, verified);

            // Verify that public key and verify signature.
            verified = rsaPublicKey.VerifyData(dataToSign, DigestType.SHA1, signature);
            Assert.AreEqual(true, verified);

            verified = rsaPublicKey.VerifyData(dataToSign, DigestType.MD5, md5Signature);
            Assert.AreEqual(true, verified);

            // Change a byte and make sure it is no longer verified.
            dataToSign[234] = (byte)(dataToSign[234] + 1);

            verified = rsaKey.VerifyData(dataToSign, DigestType.SHA1, signature);
            Assert.AreEqual(false, verified);

            verified = rsaPublicKey.VerifyData(dataToSign, DigestType.SHA1, signature);
            Assert.AreEqual(false, verified);

            verified = rsaPublicKey.VerifyData(dataToSign, DigestType.MD5, md5Signature);
            Assert.AreEqual(false, verified);
        }

        [TestMethod]
        public void Fixed_Rsa_Key_Should_Verify_Signatures()
        {
            string pairXmlString = "<RSAKeyValue><Modulus>xRoZRWKeU183l+qOnxhsE/BRVplp01dPIoyF7OVqg3ZN74AYybSQtzLkr8ozKSERZ7Q24rR0+VI0zSCp1hamu2XYWWhO/bLQiruLvjn+W6bmqkor/deEjZQjNRUeLMHdzjUDI5lXnF0XcRkrGtkMo0iV+OyjgeHzgFiLYNkd/o9CNFfPEwHKEnmoP2yilM6n94v3qgfgzVaJPQkknCQ1Ssp95zeMunD8HbFBFfMWsVHOp5YHMuRGGI2Rnq0TUit0ygwBJnIQU7xtkoO7bciDBF3GASr01uJyAEMR/piPY7SZFdvLgT5jOEaa9Q8zQknLTEb7aVicIGSNlakKmtmq2w==</Modulus><Exponent>AQAB</Exponent><P>5IHh3jCRU3yl+Qi7m/2l0a2PUFPhHkOd5HlwRBs0N4Mn3qOqQZKAFxN25yZbY5XTT4JUrnXAdRNmybG218MXTZddicxp38AkvvulPxVVp5Q78jU2LK7Tu32l8PtNucOpZ9PYQ23+HL1ito4Go4SYosVmE4flKUVygFd7uEdjjjc=</P><Q>3NDq288EEmM1/vGQwf6qkkQPAKV+9Vc/NfPPrXhLsaF8/SUft2YCVBPGdXOkKT8wYfohF8E8ZRpbym7XNkLpsOrgrzESCXHoxBwOytN94wL9q3C33a6fj9IdiBhs5rKNY9ubWxO1m1Jb38Y2MfFiFIq6kltWiNg+bPpsjbjfln0=</Q><DP>I7wwH3hG8eB3cEeuV0nGidDzraNAfGQkqBtZtDzw4JGRRZ8gvBp2D6XKnYGBH0TKBBAkwBfIHkcHdxlkt79ZwJegWDFwiT5aQMfH4uKqP4fODCXIMBqzIAoZTmNul1ODBaq6kmj8KXBwpI33edD9sc7fFMTW0Kp8qpeD0KDRb+c=</DP><DQ>vnEIPS8bqegax5f7avSCk8dS0RHqnxnyEEwIjumzDq3iKEl+QyQdWfn8LYvgxxoSVk3tgJlNxzymcb6KqeenuMe5pB7EGZU+VPSF5XPlnIYV1WhPi0dxog5rHddDBOx1eOwL3s7uz9iGGEbQst4l4uWK53MS/M7TeBW6zbfmTdk=</DQ><InverseQ>s8XP93KlpP6FnLrfSL9U+Lt2BccIuXJEZS+N9U3uEv6bZbINdfHKwlG08Ts6XkCv4ydLSp5BqGfhtdvc4BvsIYDT7O7P+7jmQWBn8WdNRDp3sbzApzp8q+cb04bPoWyhbY319dAGPiBhdAvpsztwKm3sXdTAeFTxVRhYpHELuGo=</InverseQ><D>PScfTxlNcSWaPI6gSHlN0xPdUKaRoGo22cvKo5j8ZqRWguf2COL2gXiPXoE4RVsGqOvPmaAOqOpaCojHWO63NW5gZUEJPQp1TI3qyg75PZt3dr9DjeMHs9uR3t7Z+V3/AQMOocVqWs/BPaxm5NIR6zlSmqRlCJ+/qoMOX3KNrVRe99tsoxWestuc/J2J2txWI5a8AJRXhIELoflVVamQHMfRWGp8DRAFNwn3CoQhslIkF6TOtLl1EKiBZNegD6UfBKEyqI7Szk8UXh1BgTR2PXqZ8NDiGOIlGV7FZZ2NC29cQpkw2Oo/t3gYI4ORaZnhY/rKzyz6Z/MuVscg2Ku6HQ==</D></RSAKeyValue>";
            string publicKeyXmlString = "<RSAKeyValue><Modulus>xRoZRWKeU183l+qOnxhsE/BRVplp01dPIoyF7OVqg3ZN74AYybSQtzLkr8ozKSERZ7Q24rR0+VI0zSCp1hamu2XYWWhO/bLQiruLvjn+W6bmqkor/deEjZQjNRUeLMHdzjUDI5lXnF0XcRkrGtkMo0iV+OyjgeHzgFiLYNkd/o9CNFfPEwHKEnmoP2yilM6n94v3qgfgzVaJPQkknCQ1Ssp95zeMunD8HbFBFfMWsVHOp5YHMuRGGI2Rnq0TUit0ygwBJnIQU7xtkoO7bciDBF3GASr01uJyAEMR/piPY7SZFdvLgT5jOEaa9Q8zQknLTEb7aVicIGSNlakKmtmq2w==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

            var rsaKey = RsaAsymmetricKeyPair.FromXmlString(pairXmlString);
            var rsaPublicKey = RsaPublicKey.FromXmlString(publicKeyXmlString);

            byte[] dataToSign = new byte[500];
            var random = new Random(651321);
            random.NextBytes(dataToSign);

            byte[] signature = rsaKey.SignData(dataToSign, DigestType.SHA1);

            // Verify that full key and verify its own signature.
            bool verified = rsaKey.VerifyData(dataToSign, DigestType.SHA1, signature);
            Assert.AreEqual(true, verified);

            // Verify that public key and verify signature.
            verified = rsaPublicKey.VerifyData(dataToSign, DigestType.SHA1, signature);
            Assert.AreEqual(true, verified);

            // Change a byte and make sure it is no longer verified.
            dataToSign[234] = (byte)(dataToSign[234] + 1);

            verified = rsaKey.VerifyData(dataToSign, DigestType.SHA1, signature);
            Assert.AreEqual(false, verified);

            verified = rsaPublicKey.VerifyData(dataToSign, DigestType.SHA1, signature);
            Assert.AreEqual(false, verified);

            // Check the public key hash.
            string publicKeyHash = rsaPublicKey.PublicKeyHash;
            Assert.AreEqual("4fe1e99406cbcb0364ec8b23dc8b9f81f8d22335", publicKeyHash);
        }


        [TestMethod]
        public void Should_Rountrip_RSA_Encrypted_Strings()
        {
            string pairXmlString = "<RSAKeyValue><Modulus>xRoZRWKeU183l+qOnxhsE/BRVplp01dPIoyF7OVqg3ZN74AYybSQtzLkr8ozKSERZ7Q24rR0+VI0zSCp1hamu2XYWWhO/bLQiruLvjn+W6bmqkor/deEjZQjNRUeLMHdzjUDI5lXnF0XcRkrGtkMo0iV+OyjgeHzgFiLYNkd/o9CNFfPEwHKEnmoP2yilM6n94v3qgfgzVaJPQkknCQ1Ssp95zeMunD8HbFBFfMWsVHOp5YHMuRGGI2Rnq0TUit0ygwBJnIQU7xtkoO7bciDBF3GASr01uJyAEMR/piPY7SZFdvLgT5jOEaa9Q8zQknLTEb7aVicIGSNlakKmtmq2w==</Modulus><Exponent>AQAB</Exponent><P>5IHh3jCRU3yl+Qi7m/2l0a2PUFPhHkOd5HlwRBs0N4Mn3qOqQZKAFxN25yZbY5XTT4JUrnXAdRNmybG218MXTZddicxp38AkvvulPxVVp5Q78jU2LK7Tu32l8PtNucOpZ9PYQ23+HL1ito4Go4SYosVmE4flKUVygFd7uEdjjjc=</P><Q>3NDq288EEmM1/vGQwf6qkkQPAKV+9Vc/NfPPrXhLsaF8/SUft2YCVBPGdXOkKT8wYfohF8E8ZRpbym7XNkLpsOrgrzESCXHoxBwOytN94wL9q3C33a6fj9IdiBhs5rKNY9ubWxO1m1Jb38Y2MfFiFIq6kltWiNg+bPpsjbjfln0=</Q><DP>I7wwH3hG8eB3cEeuV0nGidDzraNAfGQkqBtZtDzw4JGRRZ8gvBp2D6XKnYGBH0TKBBAkwBfIHkcHdxlkt79ZwJegWDFwiT5aQMfH4uKqP4fODCXIMBqzIAoZTmNul1ODBaq6kmj8KXBwpI33edD9sc7fFMTW0Kp8qpeD0KDRb+c=</DP><DQ>vnEIPS8bqegax5f7avSCk8dS0RHqnxnyEEwIjumzDq3iKEl+QyQdWfn8LYvgxxoSVk3tgJlNxzymcb6KqeenuMe5pB7EGZU+VPSF5XPlnIYV1WhPi0dxog5rHddDBOx1eOwL3s7uz9iGGEbQst4l4uWK53MS/M7TeBW6zbfmTdk=</DQ><InverseQ>s8XP93KlpP6FnLrfSL9U+Lt2BccIuXJEZS+N9U3uEv6bZbINdfHKwlG08Ts6XkCv4ydLSp5BqGfhtdvc4BvsIYDT7O7P+7jmQWBn8WdNRDp3sbzApzp8q+cb04bPoWyhbY319dAGPiBhdAvpsztwKm3sXdTAeFTxVRhYpHELuGo=</InverseQ><D>PScfTxlNcSWaPI6gSHlN0xPdUKaRoGo22cvKo5j8ZqRWguf2COL2gXiPXoE4RVsGqOvPmaAOqOpaCojHWO63NW5gZUEJPQp1TI3qyg75PZt3dr9DjeMHs9uR3t7Z+V3/AQMOocVqWs/BPaxm5NIR6zlSmqRlCJ+/qoMOX3KNrVRe99tsoxWestuc/J2J2txWI5a8AJRXhIELoflVVamQHMfRWGp8DRAFNwn3CoQhslIkF6TOtLl1EKiBZNegD6UfBKEyqI7Szk8UXh1BgTR2PXqZ8NDiGOIlGV7FZZ2NC29cQpkw2Oo/t3gYI4ORaZnhY/rKzyz6Z/MuVscg2Ku6HQ==</D></RSAKeyValue>";
            string publicKeyXmlString = "<RSAKeyValue><Modulus>xRoZRWKeU183l+qOnxhsE/BRVplp01dPIoyF7OVqg3ZN74AYybSQtzLkr8ozKSERZ7Q24rR0+VI0zSCp1hamu2XYWWhO/bLQiruLvjn+W6bmqkor/deEjZQjNRUeLMHdzjUDI5lXnF0XcRkrGtkMo0iV+OyjgeHzgFiLYNkd/o9CNFfPEwHKEnmoP2yilM6n94v3qgfgzVaJPQkknCQ1Ssp95zeMunD8HbFBFfMWsVHOp5YHMuRGGI2Rnq0TUit0ygwBJnIQU7xtkoO7bciDBF3GASr01uJyAEMR/piPY7SZFdvLgT5jOEaa9Q8zQknLTEb7aVicIGSNlakKmtmq2w==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

            var rsaKey = RsaAsymmetricKeyPair.FromXmlString(pairXmlString);
            var rsaPublicKey = RsaPublicKey.FromXmlString(publicKeyXmlString);

            TestRoundtripEncryption(rsaKey, rsaPublicKey, "Hello, world!");
            TestRoundtripEncryption(rsaKey, rsaPublicKey, "Hello, world! Again");
        }

        private void TestRoundtripEncryption(RsaAsymmetricKeyPair keyPair, RsaPublicKey publicKey, string phrase)
        {            
            byte[] data = Encoding.UTF8.GetBytes(phrase);
            byte[] encryptedData = publicKey.Encrypt(data);

            byte[] decryptedData = keyPair.Decrypt(encryptedData);

            string decryptedPhrase = Encoding.UTF8.GetString(decryptedData);

            Assert.AreEqual(phrase, decryptedPhrase);

            // Ensure that the public key alone cannot decrypt the phrase.
            RSACryptoServiceProvider rsaKeyProvider = new RSACryptoServiceProvider();
            rsaKeyProvider.FromXmlString(publicKey.XmlString);

            Exception caughtEx = null;
            try
            {
                decryptedData = rsaKeyProvider.Decrypt(encryptedData, true);
            }
            catch (Exception ex)
            {
                caughtEx = ex;
            }

            Assert.IsNotNull(caughtEx);
        }
    }
}
