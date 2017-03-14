using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class Services : IWCF
    {
        public void LoadKeyStore(string name, string password)
        {
            if (File.Exists(RSA.PATH + "\\" + name))
            {
                //ottiene il contenuto del file cifrato e lo decifra con la chiave fornita, per poi caricarlo nel csp
                string keystore = AESFiles.Decrypt(File.ReadAllText(RSA.PATH + "\\" + name), password);
                CServer.rsaKeyPair.FromXmlString(keystore);
            }
        }

        public void GenerateKeyStore(string name, string password)
        {
            if (!File.Exists(RSA.PATH + "\\" + name))
            {
                //ottiene il contenuto del csp, lo cifra e lo salva
                string keystore = CServer.rsaKeyPair.ToXmlString(true);
                keystore = AESFiles.Encrypt(keystore, password);
                File.WriteAllText(RSA.PATH + "\\" + name, keystore);
            }
        }
    }
}
