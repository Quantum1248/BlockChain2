using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
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
            this.Signature = RSA.Sign(Encoding.ASCII.GetBytes(this.Serialize()), csp.ExportParameters(true), false);
        }
    }
}
