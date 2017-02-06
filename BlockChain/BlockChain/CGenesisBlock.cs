using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CGenesisBlock : CBlock
    {
        public CGenesisBlock() : base(0,1)
        {
            this.Hash = "GENESISBLOCK";
            this.BlockNumber = 0;
            this.Transactions = new List<Transaction>();
            this.Nonce = 0;
            this.Timestamp = new DateTime(0,0,0);
        }
    }
}
