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
        
        public string prevBlockHash;
        public string Hash;
        public ulong BlockNumber;
        public List<Transaction> Transactions;
        public string MerkleRoot;
        public DateTime Timestamp; //TODO!: enorme problema di sicurezza
        public ulong Nonce;
        public ushort Difficulty;
        public static int TargetMiningTime = 60;

        //Ogni blocco viene inizializzato con le transazioni al momento contenute nella MemPool.
        //TODO: E' da implementare il caricamento asincrono di transazioni parallelo al mining
        public CBlock(ulong NumBlock, ushort Difficulty, int txLimit = 5)
        {
            this.BlockNumber = NumBlock;
            this.Nonce = 0;
            this.Difficulty = Difficulty;
            this.Transactions = GetTxFromMemPool(txLimit);
            List<string> strList = new List<string>();
            foreach(Transaction tx in Transactions)
            {
                strList.Add(tx.Serialize());
            }
            this.Timestamp = DateTime.Now;
            GenerateMerkleRoot(strList);
            //TODO: implementare funzione per rpendere last block hash
        }

        private List<Transaction> GetTxFromMemPool(int txLimit)
        {
            //Si assume che le transazioni in MemPool siano già state validate.
            return MemPool.Instance.GetTx(txLimit);            
        }

        public string Serialize()
        {
            return "{" + Hash + ";" + BlockNumber + ";" + ";" + Nonce + ";" + Timestamp + ";" + Difficulty + "}";
        }

        /// <summary>
        /// Crea un nuovo oggetto CBlock usando una stringa che lo rappresenta.
        /// </summary>
        /// <param name="BlockString">Stringa che rappresenta l'oggetto CBlock.</param>
        public static CBlock Deserialize(string SerializedBlock)
        {
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
                //hashSum = Convert.ToBase64String(Encoding.ASCII.GetBytes(hashSum)); //dopo essere stati concatenati, gli hash delle transazioni sono passati attraverso una funzione hash
                
                hashList.Add(Utilities.ByteArrayToString(SHA256Managed.Create().ComputeHash(Utilities.StringToByteArray(hashSum))));
            }
            if(hashList.Count == 1) //quando si arriva all'hash del nodo root ci si ferma
            {
                return hashList.First<string>();
            }
            return GenerateMerkleHashes(hashList);
        }
    }
}