using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Crypto
{
    public static class SecureRandomBytes
    {
        public static byte[] GetRandomBytes(int length)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] byteArray = new byte[length];
                rng.GetBytes(byteArray);
                return byteArray;
            }
        }

        public static byte[] GetRandomBytesFromBitSize(int bitSize)
        {
            if (bitSize % 8 != 0)
            {
                throw new ArgumentException("bitSize must be divisible by 8", "bitSize");
            }

            return GetRandomBytes(bitSize / 8);            
        }
    }
}
