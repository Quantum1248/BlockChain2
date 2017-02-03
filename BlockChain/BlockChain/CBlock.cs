using System;
using Newtonsoft.Json;

namespace BlockChain
{
    public class CBlock
    {
        public CHeader Header;
        public string Transiction;
        public ulong Nonce;
        public ulong Timestamp;
        public ushort Difficutly;
        public static int TargetMiningTime = 60;

        public CBlock()
        { }

        public CBlock(ulong NumBlock,string Hash,string PreviusBlockHash, string Transiction, ulong Nonce, ulong Timestamp, ushort Difficutly)
        {
            Header = new CHeader(NumBlock, Hash, PreviusBlockHash);
            this.Transiction = Transiction;
            this.Nonce = Nonce;
            this.Timestamp = Timestamp;
            this.Difficutly = Difficutly;
        }

        public string Serialize()
        {
            //return "{" + Hash + ";" + BlockNumber + ";" + Transiction + ";" + Nonce + ";" + Timestamp + ";" + Difficutly + "}";
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Crea un nuovo oggetto CBlock usando una stringa che lo rappresenta.
        /// </summary>
        /// <param name="BlockString">Stringa che rappresenta l'oggetto CBlock.</param>
        public static CBlock Deserialize(string SerializedBlock)
        {
            /*
            string[] blockField;
            SerializedBlock = SerializedBlock.Trim('{', '}');
            blockField = SerializedBlock.Split(';');
            if (Program.DEBUG)
                CIO.DebugOut("Deserializing block number: " + blockField[1] + ".");
            return new CBlock(blockField[0], Convert.ToUInt64(blockField[1]), blockField[2], Convert.ToUInt64(blockField[3]), Convert.ToUInt64(blockField[4]), Convert.ToUInt16(blockField[5]));
            */
            return JsonConvert.DeserializeObject<CBlock>(SerializedBlock);
        }
    }
}