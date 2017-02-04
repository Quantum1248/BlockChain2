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
            Header = new CHeader(0, "GENESISBLOCK", "");
            this.Transiction = "";
            this.Nonce = 0;
            this.Timestamp = new DateTime(0);
            this.Difficulty = 1;
        }
    }
}
