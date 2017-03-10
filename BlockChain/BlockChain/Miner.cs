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

        public static void Start(int txLimit)
        {
            CBlock block;
            while (true)
            {
                block = GenerateNextBlock(); 
                AddProof(block);
                CBlockChain.Instance.AddNewMinedBlock(new CTemporaryBlock(block, null));
                CPeers.Instance.DoRequest(ERequest.BroadcastMinedBlock, block);
            }


            //TODO: deve ritornare qualcosa questa funzione? Il blocco minato va spedito da qua o dall'esterno?
        }
        /// <summary>
        /// Aggiunge il proof of work(l'hash) al blocco.
        /// </summary>
        /// <param name="Block">Blocco su cui calcolare il proof of work.</param>
        public static void AddProof(CBlock Block) //TODO: implementare evento per l'uscita in caso sia stato trovato un blocco parallelo. Implementare multithreading
        {
            string hash="";
            bool found = false;

            while (!found)
            {
                Block.Timestamp = DateTime.Now;
                Block.Nonce++; //incremento della nonce per cambiare hash
                hash = HashBlock(Block); //calcola l'hash secondo il template di scrypt usato da litecoin
                found = true;
                for (int i = 0; i < Block.Difficulty && found; i++)
                    if (hash[i] != '0')
                        found = false;
            }
            if (Program.DEBUG)
                CIO.DebugOut("Found hash for block " + Block.Header.BlockNumber + ": " + hash);
            Block.Header.Hash = hash;
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

        public static string HashBlock(CBlock block)
        {
            string toHash = block.Header.PreviousBlockHash + block.Nonce + block.Timestamp + block.MerkleRoot; //si concatenano vari parametri del blocco TODO: usare i parmetri giusti, quelli usati qua sono solo per dimostrazione e placeholder
            return Utilities.ByteArrayToHexString(
                SCrypt.ComputeDerivedKey(
                    Encoding.ASCII.GetBytes(toHash), Encoding.ASCII.GetBytes(toHash), 1024, 1, 1, 1, 32)
                    ); //calcola l'hash secondo il template di scrypt usato da litecoin
        }

        private static CBlock GenerateNextBlock()
        {
            int numberOfBlocks = 60;

            CBlock lastBlock = CBlockChain.Instance.LastBlock;
            CBlock previousBlock;
            short newBlockDifficulty = 0;
            ulong highAverangeTimeLimit = 70, lowAverangeTimeLimit = 30;
            ulong averangeBlockTime = 0;

            if (lastBlock.Header.BlockNumber > (ulong)numberOfBlocks)
                previousBlock = CBlockChain.Instance.RetriveBlock(lastBlock.Header.BlockNumber - (ulong)numberOfBlocks, true);
            else
                previousBlock = CBlockChain.Instance.RetriveBlock(1, true);

            if (previousBlock != null)
                averangeBlockTime = CBlockChain.Instance.AverageBlockTime(previousBlock, lastBlock); //in secondi
            else
                averangeBlockTime = 60;


            if (averangeBlockTime > highAverangeTimeLimit)
            {
                newBlockDifficulty = (short)(lastBlock.Difficulty -1);
                if (newBlockDifficulty <= 0)
                    newBlockDifficulty = 1;
                if (Program.DEBUG)
                {
                    CIO.DebugOut("La nuova difficoltà è: " + newBlockDifficulty);
                    Thread.Sleep(1000);
                }
            }
            else if (averangeBlockTime < lowAverangeTimeLimit)
            {
                newBlockDifficulty = (short)(lastBlock.Difficulty + 1);
                if (Program.DEBUG)
                {
                    CIO.DebugOut("La nuova difficoltà è: " + newBlockDifficulty);
                    Thread.Sleep(1000);
                }
            }
            else
            {
                newBlockDifficulty = (short)lastBlock.Difficulty;
                if (Program.DEBUG)
                {
                    CIO.DebugOut("La nuova difficoltà è: " + newBlockDifficulty);
                    Thread.Sleep(1000);
                }
            }

            CBlock res = new CBlock(CBlockChain.Instance.LastBlock.Header.BlockNumber + 1, CBlockChain.Instance.LastBlock.Header.Hash, (ushort)newBlockDifficulty);
            return res;
        }
    }
}
