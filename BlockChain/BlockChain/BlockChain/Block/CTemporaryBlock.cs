using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace BlockChain
{
    class CTemporaryBlock : CBlock
    {
        private CPeer mSender;

        //public CTemporaryBlock(CBlock Block, CPeer Sender) : base(Block.Header.BlockNumber, Block.Header.Hash, Block.Header.PreviousBlockHash, Block.Transiction, Block.Nonce, Block.Timestamp, Block.Difficulty)
        public CTemporaryBlock(CBlock Block, CPeer Sender) : base(Block.Header.BlockNumber, Block.Header.Hash, Block.Header.PreviousBlockHash,Block.TxLimit, Block.Nonce, Block.Timestamp, Block.Difficulty,Block.Transactions)
        {
            this.Nonce = Block.Nonce;
            this.Timestamp = Block.Timestamp;
            mSender = Sender;
            { }
        }

        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public CPeer Sender
        {
            get { return mSender; }
        }
    }
}
