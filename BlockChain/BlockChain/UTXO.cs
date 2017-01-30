using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class UTXO
    {
        public string TxHash;
        public Output[] Output;

        public UTXO(string txHash, Output[] outputs)
        {
            this.TxHash = txHash;
            this.Output = outputs;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static UTXO Deserialize(string jsonUtxo)
        {
            return JsonConvert.DeserializeObject<UTXO>(jsonUtxo);
        }

    }
}
