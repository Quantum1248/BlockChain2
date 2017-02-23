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
        public CBlock(ulong NumBlock,string Hash,string PreviusBlockHash, int TxLimit, ulong Nonce, DateTime Timestamp, ushort Difficulty)
        {
            Header = new CHeader(NumBlock, Hash, PreviusBlockHash);
            this.TxLimit = TxLimit;
            this.Transactions = MemPool.Instance.GetUTX(TxLimit);
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

        public CBlock(ulong NumBlock, string PreviusBlockHash, ushort Difficulty)
        {
            Header = new CHeader(NumBlock, PreviusBlockHash);
            this.Difficulty = Difficulty;
        }

        //Ogni blocco viene inizializzato con le transazioni al momento contenute nella MemPool.
        //TODO: E' da implementare il caricamento asincrono di transazioni parallelo al mining
        public CBlock(ulong NumBlock, ushort Difficulty, int txLimit = 5)
        {
            //this.Header.BlockNumber = NumBlock;
            this.Header = new CHeader(NumBlock, CBlockChain.Instance.LastBlock.Header.Hash);
            this.Nonce = 0;
            this.Difficulty = Difficulty;
            this.Transactions = MemPool.Instance.GetUTX(txLimit);
            List<string> strList = new List<string>();
            foreach(Transaction tx in Transactions)
            {
                strList.Add(tx.Serialize());
            }
            this.Timestamp = DateTime.Now;
            GenerateMerkleRoot(strList);
            //TODO: implementare funzione per rpendere last block hash
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

        private void GenerateMerkleRoot(List<string> transactions) 
        {
            this.MerkleRoot = GenerateMerkleHashes(transactions);
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