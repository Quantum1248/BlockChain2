﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    //TODO
    static class RSA
    {
        internal static byte[] Encrypt(byte[] v, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
        {
            try
            {
                byte[] encryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {

                    //Import the RSA Key information. This only needs
                    //toinclude the public key information.
                    RSA.ImportParameters(RSAKeyInfo);

                    //Encrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    encryptedData = RSA.Encrypt(v, DoOAEPPadding);
                }
                return encryptedData;
            }
            //Catch and display a CryptographicException  
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);

                return null;
            }

        }

        internal static byte[] Decrypt(byte[] v, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
        {
            try
            {
                byte[] decryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This needs
                    //to include the private key information.
                    RSA.ImportParameters(RSAKeyInfo);

                    //Decrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    decryptedData = RSA.Decrypt(v, DoOAEPPadding);
                }
                return decryptedData;
            }
            //Catch and display a CryptographicException  
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());

                return null;
            }

        }

        public static string Sign(byte[] v, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
        {
            try
            {
                byte[] signedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This needs
                    //to include the private key information.
                    RSA.ImportParameters(RSAKeyInfo);

                    //Decrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    signedData = RSA.SignData(v, new SHA256CryptoServiceProvider());
                }
                return Convert.ToBase64String(signedData);
            }
            //Catch and display a CryptographicException  
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());

                return null;
            }
        }

        public static bool VerifySignature(byte[] hash, byte[] v, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
        {
                bool verifiedSig;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {

                    //Import the RSA Key information. This only needs
                    //toinclude the public key information.
                    RSA.ImportParameters(RSAKeyInfo);

                    //Encrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    verifiedSig = RSA.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256"), v);
                }
                return verifiedSig;



        }

        public static void HashSignTransaction(Transaction unsignedTx, RSACryptoServiceProvider rsaKeyPair)
        {
            byte[] tx = Encoding.Unicode.GetBytes(Transaction.Serialize(unsignedTx));
            unsignedTx.Signature = RSA.Sign(tx, rsaKeyPair.ExportParameters(true), false);
        }

        public static bool VerifySignedTransaction(Transaction signedTx, RSACryptoServiceProvider rsaKeyPair)
        {
            byte[] bUnsignedTx = Encoding.Unicode.GetBytes(Transaction.Serialize(signedTx));
            SHA256Managed sha = new SHA256Managed();
            byte[] digest = sha.ComputeHash(bUnsignedTx);

            return RSA.VerifySignature(digest, Convert.FromBase64String(signedTx.Signature), rsaKeyPair.ExportParameters(false), false);
        }

        public static string ExportPubKey(RSACryptoServiceProvider csp) //esporta la chiave pubblica del csp dato in una stringa codificata in base64
        {
            byte[] blob = csp.ExportCspBlob(false);
            string pubKey = Convert.ToBase64String(blob);
            return pubKey;
        }

        public static void ImportPubKey(string base64PubKey, RSACryptoServiceProvider csp)//importa la chiave pubblica nell'oggetto specificato data una stringa base64
        {
            byte[] blob = Convert.FromBase64String(base64PubKey);
            csp.ImportCspBlob(blob);

        }

        internal static RSACryptoServiceProvider GenRSAKey()
        {
            return new RSACryptoServiceProvider();

        }
    }
}
