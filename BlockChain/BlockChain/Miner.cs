﻿using System;
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
                if (Program.DEBUG)
                    CIO.DebugOut("Hash corrente blocco " + Block.Header.BlockNumber + ": " + hash);

                if (hash[0] == '0')
                {
                    found = true;
                    for (int i = 0; i < Block.Difficulty-1 && found; i++)
                        if (hash[i] != hash[i + 1])
                            found = false;
                }
            }
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
            CBlock lastBlock = CBlockChain.Instance.LastBlock;
            CBlock lastValidBlock = CBlockChain.Instance.LastValidBlock;
            short newBlockDifficulty = 0;
            ulong highAverangeTimeLimit = 70, lowAverangeTimeLimit = 50;
            ulong averangeBlockTime = 0;
            /*
            if (lastValidBlock.Header.BlockNumber <60)
                averangeBlockTime = CBlockChain.Instance.AverageBlockTime(0, lastValidBlock.Header.BlockNumber); //in secondi
            else
                averangeBlockTime = CBlockChain.Instance.AverageBlockTime(CBlockChain.Instance.LastValidBlock.Header.BlockNumber-60, lastValidBlock.Header.BlockNumber); //in secondi

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
            }*/

            newBlockDifficulty = 2;

            CBlock res = new CBlock(CBlockChain.Instance.LastBlock.Header.BlockNumber + 1, CBlockChain.Instance.LastBlock.Header.Hash, (ushort)newBlockDifficulty);
            return res;
        }
    }
}
