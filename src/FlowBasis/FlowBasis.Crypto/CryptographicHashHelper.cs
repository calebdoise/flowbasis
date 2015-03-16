using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Crypto
{
    internal static class CryptographicHashHelper
    {
        
        internal static object GetClrHashObject(DigestType digestType)
        {
            switch (digestType)
            {
                case DigestType.SHA1: return new SHA1Managed();
                case DigestType.MD5: return new MD5CryptoServiceProvider();

                default: throw new Exception(String.Format("Hash object could not be loaded for {0}.", digestType));
            }
        }

    } 
}
