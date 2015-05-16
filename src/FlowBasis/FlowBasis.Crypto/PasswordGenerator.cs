using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBasis.Crypto
{
    public static class PasswordGenerator
    {
        public static string NewPassword(int length = 20)
        {
            string allowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@$*.+-_";

            byte[] bytes = SecureRandomBytes.GetRandomBytes(length);

            StringBuilder sb = new StringBuilder();

            for (int co = 0; co < length; co++)
            {
                byte b = bytes[co];
                char ch = allowedCharacters[b % allowedCharacters.Length];
                sb.Append(ch);
            }

            return sb.ToString();
        }
    }
}
