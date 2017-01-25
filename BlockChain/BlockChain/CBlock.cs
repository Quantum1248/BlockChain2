using System;
using CryptSharp.Utility;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace BlockChain
{
    public class CBlock
    {
        //TODO: aggiungere riferimento a blocco precedente
        public string Hash;
        public ulong BlockNumber;
        public string Transiction;
        public string MerkleRoot;
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
            this.Nonce = 0; //TODO inserire nonce come parametro opzionale o toglierlo, il valore deve partire da 0
            this.Timestamp = Timestamp;
            this.Difficutly = Difficutly;
        }

        public CBlock(string Hash, ulong NumBlock, List<string> Transactions, ulong Nonce, ulong Timestamp, ushort Difficutly)
        {
            this.Hash = Hash;
            this.BlockNumber = NumBlock;
            this.Nonce = 0;//TODO inserire nonce come parametro opzionale o toglierlo, il valore deve partire da 0
            this.Timestamp = Timestamp;
            this.Difficutly = Difficutly;
            GenerateMerkleRoot(Transactions);
        }

        public string Serialize()
        {
            return "{" + Hash + ";" + BlockNumber + ";" + Transiction + ";" + Nonce + ";" + Timestamp + ";" + Difficutly + "}";
        }

        /// <summary>
        /// Crea un nuovo oggetto CBlock usando una stringa che lo rappresenta.
        /// </summary>
        /// <param name="BlockString">Stringa che rappresenta l'oggetto CBlock.</param>
        public static CBlock Deserialize(string SerializedBlock)
        {
            //forse è meglio usarlo come costruttore
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

        private void Scrypt(string previousBlockHash) //TODO: verificare singole transazioni
        {
            string toHash;
            string hash;
            bool found = false;
            int higher = 0, current = 0;

            while (!found)
            {
                toHash = previousBlockHash + this.Nonce + this.Timestamp + this.Transiction + this.MerkleRoot; //si concatenano vari parametri del blocco TODO: usare i parmetri giusti, quelli usati qua sono solo per dimostrazione e placeholder
                hash = Convert.ToBase64String(SCrypt.ComputeDerivedKey(Encoding.UTF8.GetBytes(toHash), Encoding.UTF8.GetBytes(toHash), 1024, 1, 1, 1, 32)); //calcola l'hash secondo il template di scrypt usato da litecoin
                for (int i = 0; i <= Difficutly; i++)
                {
                    if (i == Difficutly) //se il numero di zeri davanti la stringa è pari alla difficoltà del blocco, viene settato l'hash e si esce
                    {
                        this.Hash = hash;
                        return;
                    }
                    if (!(hash[i] == '0'))
                    {
                        current = 0;
                        break;
                    }

                    current++;
                    if(higher < current)
                    {
                        higher = current;
                    }
                    
                }
                
                this.Nonce++; //incremento della nonce per cambiare hash
            }
            
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
                hashSum = Convert.ToBase64String(Encoding.UTF8.GetBytes(hashSum)); //dopo essere stati concatenati, gli hash delle transazioni sono passati attraverso una funzione hash
                
                hashList.Add(Convert.ToBase64String(SHA256Managed.Create().ComputeHash(Convert.FromBase64String(hashSum))));
            }
            if(hashList.Count == 1) //quando si arriva all'hash del nodo root ci si ferma
            {
                return hashList.First<string>();
            }
            return GenerateMerkleHashes(hashList);
        }
    }
}