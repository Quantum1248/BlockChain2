using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CTemporaryBlock  : CBlock
    {
        private CPeer mSender;
        public CTemporaryBlock(CBlock Block, CPeer Sender) : base(Block.BlockNumber, Block.Difficulty)
        {
            this.Hash = Block.Hash;
            this.Transactions = Block.Transactions;
            this.Nonce = Block.Nonce;
            this.Timestamp = Block.Timestamp;
            mSender = Sender;
        }
    }
}
