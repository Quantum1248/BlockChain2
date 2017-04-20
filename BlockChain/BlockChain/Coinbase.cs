using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class Coinbase : Input
    {
        public int Rand;

        public Coinbase()
        {
            Random rnd = new Random();
            this.TxHash = "0";
            this.OutputIndex = -1;
            this.Rand = rnd.Next();
        }
    }
}
