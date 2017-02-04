using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CoinbaseTransaction : Transaction
    {
        public CoinbaseTransaction()
        {
            this.inputs = null;
            //this.outputs = new Output
        }
    }
}
