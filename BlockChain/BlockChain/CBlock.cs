using System;

namespace BlockChain
{
    public class CBlock
    {
        public string Hash;
        public ulong BlockNumber;
        public string Transiction;
        public ulong Nonce;
        public ulong Timestamp;
        public ushort Difficutly;
        public static int TargetMiningTime = 60;

        public CBlock()
        { }

        public CBlock(string Hash, ulong NumBlock, string Transiction, ulong Nonce, ulong Timestamp, ushort Difficutly)
        {
            this.Hash = Hash;
            this.BlockNumber = NumBlock;
            this.Transiction = Transiction;
            this.Nonce = Nonce;
            this.Timestamp = Timestamp;
            this.Difficutly = Difficutly;
        }

        /// <summary>
        /// Crea un nuovo oggetto CBlock usando una stringa che lo rappresenta.
        /// </summary>
        /// <param name="BlockString">Stringa che rappresenta l'oggetto CBlock.</param>
        public static CBlock Deserialize(string SerializedBlock)
        {
            string[] blockField;
            SerializedBlock = SerializedBlock.Trim('{', '}');
            blockField = SerializedBlock.Split(';');
            if (Program.DEBUG)
                CIO.DebugOut("Deserializing block number: "+ blockField[1]+".");
            return new CBlock(blockField[0], Convert.ToUInt64(blockField[1]), blockField[2], Convert.ToUInt64(blockField[3]), Convert.ToUInt64(blockField[4]), Convert.ToUInt16(blockField[5]));
        }

        /*
        public ulong BlockNumber
        {
            get { return mBlockNumber; }
        }
        */
    }
}