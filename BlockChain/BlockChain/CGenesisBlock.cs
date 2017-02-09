using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CGenesisBlock : CBlock
    {
        public CGenesisBlock()
        {
            this.Difficulty = 1;
            Header = new CHeader(0, "GENESISBLOCK", "");
            this.Transactions = new List<Transaction>();
            this.Nonce = 0;
            this.Timestamp = new DateTime(0,0,0);
        }
    }
}
