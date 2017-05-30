using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockChain
{
    /// <summary>
    /// Implementazione dei servizi da offrire all'esterno
    /// </summary>
    class Services : IWCF
    {
        /// <summary>
        /// Carica in memoria i parametri RSA del dato keystore
        /// </summary>
        /// <param name="name">Il nome del keystore</param>
        /// <param name="password">La password per sbloccarlo</param>
        public void LoadKeyStore(string name, string password)
        {
            if (File.Exists(RSA.PATH + "\\" + name))
            {
                //ottiene il contenuto del file cifrato e lo decifra con la chiave fornita, per poi caricarlo nel csp
                string keystore = AESFiles.Decrypt(File.ReadAllText(RSA.PATH + "\\" + name), password);
                CServer.rsaKeyPair.FromXmlString(keystore);
            }
        }

        /// <summary>
        /// Crea un nuovo keystore con il nome e la password specificata
        /// </summary>
        /// <param name="name">Il nome del keystore</param>
        /// <param name="password">La password per sbloccarlo</param>
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
        /// <summary>
        /// Ritorna l'address del keystore attualmente caricato
        /// </summary>
        /// <returns></returns>
        public string GetKeystore()
        {
            return Utilities.Base64SHA2Hash(RSA.ExportPubKey(CServer.rsaKeyPair));
        }

        public double GetBalance()
        {
            double balance = 0;
            List<UTXO> utxos = UTXOManager.Instance.GetUTXObyHash(Utilities.Base64SHA2Hash(RSA.ExportPubKey(CServer.rsaKeyPair)));
            foreach(UTXO utxo in utxos)
            {
                foreach(Output output in utxo.Output)
                {
                    if (output.PubKeyHash.Equals(Utilities.Base64SHA2Hash(RSA.ExportPubKey(CServer.rsaKeyPair))))
                    {
                        balance += output.Amount;
                    }
                }
            }
            return balance;
        }


        public void SendTransaction(string address, double amount)
        {
            new Transaction(amount, address, CServer.rsaKeyPair);
        }

        public void StartMining()
        {
            CServer.Instance.StartMining();
        }
    }
}
