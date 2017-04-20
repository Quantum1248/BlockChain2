using System;
using CryptSharp.Utility;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BlockChain
{
    class CBlock
    {

        public CHeader Header;
        public List<Transaction> Transactions;
        public string MerkleRoot;
        public DateTime Timestamp; //TODO!: enorme problema di sicurezza
        public ulong Nonce;
        public ushort Difficulty;
        public static int TargetMiningTime = 60;
        public int TxLimit;

        public CBlock()
        { }

        //public CBlock(ulong NumBlock, string Hash, string PreviusBlockHash, string Transiction, ulong Nonce, DateTime Timestamp, ushort Difficulty)
        public CBlock(ulong NumBlock,string Hash,string PreviusBlockHash, int TxLimit, ulong Nonce, DateTime Timestamp, ushort Difficulty, List<Transaction> transactions)
        {
            Header = new CHeader(NumBlock, Hash, PreviusBlockHash);
            this.TxLimit = TxLimit;
            this.Transactions = transactions;
            this.GenerateMerkleRoot();
            this.Nonce = Nonce;
            this.Timestamp = Timestamp;
            this.Difficulty = Difficulty;
        }

        public CBlock(CHeader Header, Transaction Transaction, ulong Nonce, DateTime Timestamp, ushort Difficulty)
        {
            this.Header = Header;
            this.Transactions.Add(Transaction);
            this.Nonce = Nonce;
            this.Timestamp = Timestamp;
            this.Difficulty = Difficulty;
        }

        public CBlock(ulong numBlock, string previusBlockHash, ushort difficulty, int txLimit)
        {
            this.Transactions = new List<Transaction>();
            this.Transactions.Add(new CoinbaseTransaction(CServer.rsaKeyPair));
            this.Transactions.AddRange(MemPool.Instance.GetUTX(txLimit-1));
            this.GenerateMerkleRoot();
            Header = new CHeader(numBlock, previusBlockHash);
            this.Difficulty = difficulty;
            this.Nonce = 0;
        }

        public string Serialize()
        {
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

        public string GenerateMerkleRoot() 
        {
            List<string> transactionsHashes = new List<string>();
            foreach(Transaction t in Transactions)
                transactionsHashes.Add(t.Hash);
            string merkleRoot = GenerateMerkleHashes(transactionsHashes);
            this.MerkleRoot = merkleRoot;
            return merkleRoot;
        }

        private string GenerateMerkleHashes(List<string> transactions)//funzione ricorsiva per calcolare hash da coppie di hash: da un numero n di foglie di un albero si ricava un nodo root con un hash calcolato sugli hash delle foglie
        {
            string hashSum;
            List<string> hashList = new List<string>();
            
            while (transactions.Count >= 1)
            {
                hashSum = transactions.First<string>(); //si rimuove il primo e il secondo (se esiste) elemento dalla lista
                transactions.RemoveAt(0);
                if(transactions.Count != 0)
                {
                    hashSum += transactions.First<string>();
                    transactions.RemoveAt(0);
                }
                else
                {
                    hashSum += hashSum;
                }
                
                hashList.Add(Utilities.SHA2Hash(hashSum));
            }
            if(hashList.Count == 1) //quando si arriva all'hash del nodo root ci si ferma
            {
                return hashList.First<string>();
            }
            else if (hashList.Count == 0)
            {
                return "noTxs";
            }
            return GenerateMerkleHashes(hashList);
        }
    }
}