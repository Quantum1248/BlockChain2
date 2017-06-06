using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlockChain
{
    class CBlockChain
    {
        private CBlock mLastBlock=null; //ultimo blocco ricevuto
        private CBlock mLastValidBlock = null;  //ultimo blocco sicuramente valido
        private CSideChainTree mSideChain = null;

        const string FILENAME = "blockchain.txt";

        #region Singleton
        private static CBlockChain instance;

        private CBlockChain()
        {
            Load();
            mSideChain = new CSideChainTree(null, 5);
        }

        public static CBlockChain Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CBlockChain();
                }
                return instance;
            }
        }
        #endregion Singleton

        public CBlock LastBlock
        {
            get
            {
                lock(mSideChain)
                    mLastBlock = mSideChain.GetLastBlock();
                if (mLastBlock!=null)
                    return mLastBlock;
                return mLastValidBlock;
            }
        }

        public CBlock LastValidBlock
        {
            get { return mLastValidBlock; }
        }

        public CSideChainTree SideChain
        {
            get
            {
                return mSideChain;
            }
        }

        public string PATH
        {
            get
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string specificFolder = Path.Combine(appDataFolder, "Blockchain\\ChainData");
                if (Directory.Exists(specificFolder))
                {
                    return specificFolder;
                }
                Directory.CreateDirectory(specificFolder);
                return specificFolder;
            }
        }

        /// <summary>
        /// Carica l'ultimo blocco della blockchain.
        /// </summary>
        private void Load()
        {
            string filepath = PATH + "\\" + FILENAME;
            string block="";
            mLastValidBlock = new CGenesisBlock();
            if (File.Exists(filepath))
            {
                StreamReader streamReader = new StreamReader(filepath);
                while ((block = streamReader.ReadLine()) != null)
                {
                    CBlock b = JsonConvert.DeserializeObject<CBlock>(block);
                    if (b.Header.BlockNumber > mLastValidBlock.Header.BlockNumber)
                        mLastValidBlock = b;
                }
                streamReader.Close();
            }
            else
            {
                File.WriteAllText(filepath, new CGenesisBlock().Serialize() + '\n');
                mLastValidBlock = new CGenesisBlock();
            }
            
        }

        public CBlock RetriveBlock(ulong index, bool searchInSidechain=false)
        {
            string filepath = PATH + "\\" + FILENAME;
            string blockJson = "";
            lock (Instance)
            {
                StreamReader streamReader = new StreamReader(filepath);
                while ((blockJson = streamReader.ReadLine()) != null)
                {
                    CBlock block = JsonConvert.DeserializeObject<CBlock>(blockJson);
                    if (block.Header.BlockNumber == index)
                    {
                        streamReader.Close();
                        return block;
                    }
                }
                streamReader.Close();
            }
            if(searchInSidechain)
                lock(mSideChain)
                    if (LastBlock.Header.BlockNumber >= index)
                        return mSideChain.RetriveBlock(index);

            return null;
        }

        public CBlock[] RetriveBlocks(ulong initialIndex, ulong finalIndex)
        {
            CBlock[] ris = new CBlock[finalIndex - initialIndex];
            int c = 0;
            while (initialIndex < finalIndex)
            {
                ris[c++] = RetriveBlock(initialIndex);
                initialIndex++;
            }
            return ris;
        }

        public CHeader[] RetriveHeaders(ulong initialIndex, ulong finalIndex)
        {
            CHeader[] ris = new CHeader[finalIndex - initialIndex];
            int c = 0;
            while (initialIndex < finalIndex)
            {
                ris[c++] = RetriveBlock(initialIndex).Header;
                initialIndex++;
            }
            return ris;
        }

        /// <summary>
        /// Aggiunge i blocchi presenti nel vettore e ritorna l'indice dell'ultimo blocco aggiunto.
        /// </summary>
        /// <param name="Blocks"></param>
        /// <returns></returns>
        public ulong Add(CTemporaryBlock[] Blocks)
        {
            ulong counter = 0;
            string filepath = PATH + "\\" + FILENAME;
            //(!) e se scarico tutta la blockchain e da un certo punto in poi sbagliata?
            foreach (CTemporaryBlock b in Blocks)
            {
               
                if (b == null)
                    break;
              
                if (CValidator.ValidateBlock(b,true))
                {
                    if (b.Header.BlockNumber == LastValidBlock.Header.BlockNumber)
                        break;
                    mLastValidBlock = b as CBlock;
                    counter++;
                    lock (Instance)
                    {
                        File.AppendAllText(filepath, new CBlock(b).Serialize() + '\n');
                    }
                    //int togliilcommentoeilfalsesopra;
                }
                else
                    break;
            }
            return counter;
        }

        public bool AddNewMinedBlock(CTemporaryBlock newBlock)
        {
            lock (mSideChain)
                return mSideChain.Add(newBlock);
        }

        public CHeaderChain BestChain(CHeaderChain[] HeaderChains)
        {
            //TODO sceglie in base alla difficoltà
            CHeaderChain res=new CHeaderChain();
            foreach (CHeaderChain hc in HeaderChains)
                if (hc.Length >= res.Length)
                    res = hc;
            return res;
        }

        public ulong AverageBlockTime(CBlock start, CBlock final)
        { 
            TimeSpan timespan = final.Timestamp.Subtract(start.Timestamp);
            ulong seconds =(ulong) timespan.TotalSeconds;
            ulong nblock = final.Header.BlockNumber - start.Header.BlockNumber;
            try
            {
                return seconds / nblock;
            }
            catch
            {
                return 60;
            }
        }
    }
}