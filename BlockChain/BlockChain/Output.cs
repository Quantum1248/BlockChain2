using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class Output
    {
        public double Amount;
        public string PubKeyHash;

        public Output(double Amount, string PubKeyHash)
        {
            this.Amount = Amount;
            this.PubKeyHash = PubKeyHash;
        }
    }
}
