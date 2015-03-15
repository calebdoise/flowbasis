using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Crypto
{
    public static class RandomBytes
    {
        public static byte[] GetRandomBytesFromBitSize(int bitSize)
        {
            if (bitSize % 8 != 0)
            {
                throw new ArgumentException("bitSize must be divisible by 8", "bitSize");
            }

            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] byteArray = new byte[bitSize / 8];
                rng.GetBytes(byteArray);
                return byteArray;
            }            
        }
    }
}
