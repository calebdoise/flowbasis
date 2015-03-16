using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Crypto
{

    public interface ISymmetricKey
    {
        byte[] Encrypt(byte[] data, byte[] iv);
        byte[] Decrypt(byte[] encryptedData, byte[] iv);

        byte[] GenerateIV();

    } 

    public enum SymmetricKeyType
    {
        Rijndael = 0,
        TripleDES = 1,        
        AES = 2
    }


    public class SymmetricKey : ISymmetricKey
    {
        private SymmetricKeyType keyType;
        private byte[] keyBytes;


        private SymmetricKey()
        {
        }

        public SymmetricKey(SymmetricKeyType keyType, byte[] keyBytes)
        {
            this.keyType = keyType;
            this.keyBytes = keyBytes;
        }        

        public static SymmetricKey FromPassword(SymmetricKeyType keyType, string password, int keySize)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            if (keySize == 128)
            {
                using (SHA1Managed shaHash = new SHA1Managed())
                {
                    byte[] hashBytes = shaHash.ComputeHash(passwordBytes);
                    byte[] keyBytes = new byte[16];
                    Array.Copy(hashBytes, 0, keyBytes, 0, keyBytes.Length);

                    return new SymmetricKey(keyType, keyBytes);
                }
            }
            else if (keySize == 256)
            {
                using (SHA256Managed shaHash = new SHA256Managed())
                {
                    byte[] hashBytes = shaHash.ComputeHash(passwordBytes);
                    byte[] keyBytes = new byte[32];
                    Array.Copy(hashBytes, 0, keyBytes, 0, keyBytes.Length);

                    return new SymmetricKey(keyType, keyBytes);
                }
            }
            else
            {
                throw new Exception("Unable to create key of size: " + keySize);
            }
        }


        public SymmetricKeyType KeyType
        {
            get { return this.keyType; }
        }

        public byte[] KeyBytes
        {
            get { return this.keyBytes; }
        }


        public byte[] Encrypt(byte[] data, byte[] iv)
        {
            using (SymmetricAlgorithm key = this.GetClrSymmetricAlgoritm())
            {
                MemoryStream encryptedStream = new MemoryStream();

                using (var cryptoStream = new CryptoStream(encryptedStream, key.CreateEncryptor(this.keyBytes, iv), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();

                    encryptedStream.Position = 0;
                    return encryptedStream.ToArray();
                }               
            }
        }


        public byte[] Decrypt(byte[] encryptedData, byte[] iv)
        {
            using (SymmetricAlgorithm key = GetClrSymmetricAlgoritm())
            {
                MemoryStream decryptedStream = new MemoryStream();

                using (var cryptoStream = new CryptoStream(decryptedStream, key.CreateDecryptor(this.keyBytes, iv), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(encryptedData, 0, encryptedData.Length);
                    cryptoStream.FlushFinalBlock();

                    decryptedStream.Position = 0;
                    return decryptedStream.ToArray();
                }                                
            }
        }


        public byte[] GenerateIV()
        {
            using (SymmetricAlgorithm key = this.GetClrSymmetricAlgoritm())
            {
                key.GenerateIV();
                return key.IV;
            }
        }
       

        private SymmetricAlgorithm GetClrSymmetricAlgoritm()
        {
            return GetClrSymmetricAlgoritm(this.keyType);            
        }

        private static SymmetricAlgorithm GetClrSymmetricAlgoritm(SymmetricKeyType keyType)
        {
            switch (keyType)
            {
                case SymmetricKeyType.Rijndael: return new RijndaelManaged();
                case SymmetricKeyType.TripleDES: return new TripleDESCryptoServiceProvider();                
                case SymmetricKeyType.AES: return new System.Security.Cryptography.AesCryptoServiceProvider();

                default: throw new Exception("Unknown symmetric key type: " + keyType);
            }
        }
    }
}
