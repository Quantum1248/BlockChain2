using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptSharp.Utility;
using System.Threading;

namespace BlockChain
{
    class Miner
    {

        private Miner instance;

        public Miner Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Miner();
                }
                return instance;
            }
        }

        public void Start(int txLimit)
        {
            CBlock block;
            while (true)
            {
                block = new CBlock(CBlockChain.Instance.LastBlock.Header.BlockNumber + 1, CBlockChain.Instance.LastBlock.Difficulty, txLimit);
                Scrypt(block);
            }
            //TODO: deve ritornare qualcosa questa funzione? Il blocco minato va spedito da qua o dall'esterno?
        }
        /// <summary>
        /// Calcola il proof of work
        /// </summary>
        /// <param name="block">Blocco su cui calcolare il proof of work</param>
        public static void Scrypt(CBlock block) //TODO: implementare evento per l'uscita in caso sia stato trovato un blocco parallelo. Implementare multithreading
        {
            string toHash;
            string hash;
            bool found = false;
            int higher = 0, current = 0;

            while (!found)
            {
                block.Timestamp = DateTime.Now;
                toHash = block.Header.PreviousBlockHash + block.Nonce + block.Timestamp + block.MerkleRoot; //si concatenano vari parametri del blocco TODO: usare i parmetri giusti, quelli usati qua sono solo per dimostrazione e placeholder
                hash = Hash(block); //calcola l'hash secondo il template di scrypt usato da litecoin
                if (Program.DEBUG)
                    CIO.DebugOut("Hash corrente blocco " + block.Header.BlockNumber + ": " + hash);
                for (int i = 0; i <= block.Difficulty; i++)
                {
                    if (i == block.Difficulty) //se il numero di zeri davanti la stringa è pari alla difficoltà del blocco, viene settato l'hash e si esce
                    {
                        block.Header.Hash = hash;
                        CBlockChain.Instance.Add(new CTemporaryBlock(block,null));
                        CPeers.Instance.DoRequest(ERequest.BroadcastMinedBlock, block);
                        return;
                    }
                    if (!(hash[i] == '0'))
                    {
                        current = 0;
                        break;
                    }

                    current++;
                    if (higher < current)
                    {
                        higher = current;
                    }

                }

                block.Nonce++; //incremento della nonce per cambiare hash
            }

        }

        /// <summary>
        /// Calcola l'hash di un blocco e lo confronta al proof of work fornito per verificarne la validità
        /// </summary>
        /// <param name="block">Il blocco da confermare</param>
        /// <returns></returns>
        public static bool Verify(CBlock block)
        {
            if (block.Header.PreviousBlockHash == CBlockChain.Instance.RetriveBlock(block.Header.BlockNumber - 1).Header.Hash)
            {
                string toHash = block.Header.PreviousBlockHash + block.Nonce + block.Timestamp + block.MerkleRoot;
                if (block.Header.Hash == Utilities.ByteArrayToHexString(SCrypt.ComputeDerivedKey(Encoding.ASCII.GetBytes(toHash), Encoding.ASCII.GetBytes(toHash), 1024, 1, 1, 1, 32)))
                {
                    return true;
                }
            }
            return false;
        }

        public static string Hash(CBlock b)
        {
            string toHash = b.Header.PreviousBlockHash + b.Nonce + b.Timestamp + b.MerkleRoot; //si concatenano vari parametri del blocco TODO: usare i parmetri giusti, quelli usati qua sono solo per dimostrazione e placeholder
            return Utilities.ByteArrayToHexString(
                SCrypt.ComputeDerivedKey(
                    Encoding.ASCII.GetBytes(toHash), Encoding.ASCII.GetBytes(toHash), 1024, 1, 1, 1, 32)
                    ); //calcola l'hash secondo il template di scrypt usato da litecoin
        }
    }
}
