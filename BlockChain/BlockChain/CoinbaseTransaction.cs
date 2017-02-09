using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace BlockChain
{
    class CoinbaseTransaction : Transaction
    {
        public CoinbaseTransaction(RSACryptoServiceProvider csp)
        {
            this.inputs = null;
            this.outputs = new Output[]{ new Output(50, Utilities.SHA2Hash(RSA.ExportPubKey(csp)))};
            this.PubKey = RSA.ExportPubKey(csp);
            this.Hash = Utilities.SHA2Hash(JsonConvert.SerializeObject(this));
            RSA.HashSignTransaction(this, csp);
        }

        public CoinbaseTransaction(RSACryptoServiceProvider csp, bool testing)//Costruttore da usare SOLO per testing
        {
            this.inputs = null;
            this.outputs = new Output[] { new Output(50, Utilities.SHA2Hash(RSA.ExportPubKey(csp))) };
            this.PubKey = RSA.ExportPubKey(csp);
            this.Hash = Utilities.SHA2Hash(JsonConvert.SerializeObject(this));
            RSA.HashSignTransaction(this, csp);

            //Salva transazione su disco
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(appDataFolder, "Blockchain\\UTXODB");
            UTXO utxo = new UTXO(this.Hash, this.outputs);
            if (Directory.Exists(specificFolder))
            {

                File.WriteAllText(specificFolder + "\\" + this.Hash + ".json", utxo.Serialize());
            }
            else
            {
                Directory.CreateDirectory(specificFolder);
                File.WriteAllText(specificFolder + "\\" + this.Hash + ".json", utxo.Serialize());
            }
        }
    }
}
