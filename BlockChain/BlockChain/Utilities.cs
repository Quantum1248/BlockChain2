using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{

    //classe contenente varie funzioni utili
    class Utilities
    {
        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        ///<summary>
        ///Calcola hash su stringa, ritorna string esadecimale
        ///</summary>
        public static string SHA2Hash(string strToHash)
        {
            return Utilities.ByteArrayToHexString(SHA256Managed.Create().ComputeHash(Utilities.StringToByteArrary(strToHash)));
        }

        public static string Base64SHA2Hash(string strToHash)
        {
            return Utilities.ByteArrayToHexString(SHA256Managed.Create().ComputeHash(Utilities.StringToBase64ByteArray(strToHash)));
        }
        ///<summary>
        ///Calcola hash su stringa, ritorna byte[] esadecimale
        ///</summary>
        public static byte[] SHA2HashBytes(string strToHash)
        {
            return SHA256Managed.Create().ComputeHash(Utilities.StringToByteArrary(strToHash));
        }

        public static string ByteArrayToBase64String(byte[] base64ByteArray)
        {
            return Convert.ToBase64String(base64ByteArray);
        }

        public static byte[] StringToBase64ByteArray(string base64String)
        {
            return Convert.FromBase64String(base64String);
        }

        public static byte[] StringToByteArrary(string str)
        {
            return ASCIIEncoding.ASCII.GetBytes(str);
        }
    }
}
