using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FlowBasis.Crypto
{

    public class RsaAsymmetricKeyPair : IDisposable
    {
        private RSACryptoServiceProvider rsaKey;
        
        public RsaAsymmetricKeyPair(RSACryptoServiceProvider rsaKey)
        {
            this.rsaKey = rsaKey;
        }

        public void Dispose()
        {
            if (this.rsaKey != null)
            {
                this.rsaKey.Dispose();
                this.rsaKey = null;
            }
        }

        public static RsaAsymmetricKeyPair FromXmlString(string xmlKeyStr)
        {
            RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider();
            rsaKey.FromXmlString(xmlKeyStr);

            return new RsaAsymmetricKeyPair(rsaKey);
        }

        public static RsaAsymmetricKeyPair CreateKey(int keySize)
        {
            RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider(keySize);

            return new RsaAsymmetricKeyPair(rsaKey);
        }


        public string PairXmlString
        {
            get { return this.rsaKey.ToXmlString(true); }
        }

        public string PublicKeyXmlString
        {
            get { return this.rsaKey.ToXmlString(false); }
        }

        public RsaPublicKey PublicKey
        {
            get { return RsaPublicKey.FromXmlString(this.rsaKey.ToXmlString(false)); }
        }


        public byte[] SignData(byte[] data, DigestType digestType)
        {
            using (IDisposable hash = (IDisposable)CryptographicHashHelper.GetClrHashObject(digestType))
            {
                return this.rsaKey.SignData(data, hash);
            }
        }

        public bool VerifyData(byte[] data, DigestType digestType, byte[] signature)
        {
            using (IDisposable hash = (IDisposable)CryptographicHashHelper.GetClrHashObject(digestType))
            {
                return this.rsaKey.VerifyData(data, hash, signature);
            }
        }

    }



    public class RsaPublicKey : IDisposable
    {
        private RSACryptoServiceProvider rsaKey;
        private string publicKeyHash;

        public RsaPublicKey(RSACryptoServiceProvider rsaKey)
        {
            this.rsaKey = rsaKey;                       
        }

        public static RsaPublicKey FromXmlString(string xmlKeyStr)
        {
            RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider();
            rsaKey.FromXmlString(xmlKeyStr);              

            return new RsaPublicKey(rsaKey);
        }

        public void Dispose()
        {
            if (this.rsaKey != null)
            {
                this.rsaKey.Dispose();
                this.rsaKey = null;
            }
        }

        public string PublicKeyHash
        {
            get
            {
                if (this.publicKeyHash == null)
                {
                    byte[] publicKeyBytes = RsaKeyHelper.GetPublicKeyBytesFromXmlString(this.rsaKey.ToXmlString(false));
                    this.publicKeyHash = RsaKeyHelper.GetPublicKeySHA1HashString(publicKeyBytes);
                }

                return this.publicKeyHash;
            }
        }

        public bool VerifyData(byte[] data, DigestType digestType, byte[] signature)
        {
            using (IDisposable hash = (IDisposable)CryptographicHashHelper.GetClrHashObject(digestType))
            {
                return rsaKey.VerifyData(data, hash, signature);
            }
        }
    }


    internal class RsaKeyHelper
    {
        internal static byte[] GetPublicKeyBytesFromXmlString(string rsaXmlStr)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(rsaXmlStr);

            string modulus = xml.SelectSingleNode("/RSAKeyValue/Modulus").InnerText;
            string exponent = xml.SelectSingleNode("/RSAKeyValue/Exponent").InnerText;

            byte[] modulusBytes = Convert.FromBase64String(modulus);
            byte[] exponentBytes = Convert.FromBase64String(exponent);

            int totalLength = 2 + modulusBytes.Length + 2 + exponentBytes.Length;

            byte[] bytes = new byte[totalLength];

            int pos = 0;
            WriteUInt16((UInt16)modulusBytes.Length, bytes, pos);
            pos += 2;

            Buffer.BlockCopy(modulusBytes, 0, bytes, pos, modulusBytes.Length);
            pos += modulusBytes.Length;

            WriteUInt16((UInt16)exponentBytes.Length, bytes, pos);
            pos += 2;

            Buffer.BlockCopy(exponentBytes, 0, bytes, pos, exponentBytes.Length);

            return bytes;
        }

        internal static string GetPublicKeySHA1HashString(byte[] publicKeyBytes)
        {
            var keyId = new byte[20];

            using (SHA1Managed shaHash = new SHA1Managed())
            {
                byte[] keyHash = shaHash.ComputeHash(publicKeyBytes);
                
                int i = 0;
                for (int co = 0; co < keyHash.Length; co++)
                {
                    keyId[i++] = keyHash[co];
                }

                keyId[0] = (byte)((keyId[0] & 0xF) | 0x4F);
            }

            StringBuilder hex = new StringBuilder(keyId.Length * 2);
            foreach (byte b in keyId)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }
       


        private static void WriteUInt16(UInt16 value, byte[] data, int offset)
        {
            data[offset] = (byte)(value & 0xFF);
            data[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

    } 
}
