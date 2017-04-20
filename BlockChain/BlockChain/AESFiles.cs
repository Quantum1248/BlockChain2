using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security;
using System.IO;
namespace BlockChain
{
    class AESFiles
    {
        public static string Encrypt(string plainText, string sKey)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (sKey == null || sKey.Length <= 0)
                throw new ArgumentNullException("Key");
            if(sKey.Length != 16)
            {
                sKey = sKey.PadRight(16, '0');
            }
            
            byte[] bKey = ASCIIEncoding.ASCII.GetBytes(sKey);
            byte[] bIV = ASCIIEncoding.ASCII.GetBytes(sKey);
            byte[] encrypted;

            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.KeySize = 128;
                rijAlg.Key = bKey;
                rijAlg.IV = bIV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }

                
            }

            return Utilities.ByteArrayToBase64String(encrypted);
        }

        public static string Decrypt(string cipherText, string sKey)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (sKey == null || sKey.Length <= 0)
                throw new ArgumentNullException("Key");
            if (sKey.Length != 16)
            {
                sKey = sKey.PadRight(16, '0');
            }

            byte[] bKey = ASCIIEncoding.ASCII.GetBytes(sKey);
            byte[] bIV = ASCIIEncoding.ASCII.GetBytes(sKey);
            // Declare the string used to hold 
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.KeySize = 128;
                rijAlg.Key = bKey;
                rijAlg.IV = bIV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(Utilities.StringToBase64ByteArray(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
    }
}
