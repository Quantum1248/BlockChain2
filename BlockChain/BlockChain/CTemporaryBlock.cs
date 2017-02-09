using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CTemporaryBlock : CBlock
    {
        private CPeer mSender;
        public CTemporaryBlock(CBlock Block, CPeer Sender, int txLimit) : base(Block.Header.BlockNumber, Block.Header.Hash, Block.Header.PreviusBlockHash, txLimit, Block.Nonce, Block.Timestamp, Block.Difficulty)
        {
            this.Transactions = Block.Transactions;
            this.Nonce = Block.Nonce;
            this.Timestamp = Block.Timestamp;
            mSender = Sender;
            { }
        }
    }
}
