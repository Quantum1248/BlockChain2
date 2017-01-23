using System;
using System.IO;

namespace BlockChain
{
    class CBlockChain
    {
        public const string mPATH = "C:\\Users\\Manuel\\AppData\\Roaming\\Blockchain\\ChainData\\";
        private CBlock mLastBlock=null; //ultimo blocco ricevuto
        private CBlock mLastValidBlock = null;  //ultimo blocco sicuramente valido
        #region Singleton
        private static CBlockChain instance;

        private CBlockChain()
        {
            Load();
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
            get { return mLastBlock; }
        }

        public CBlock LastValidBlock
        {
            get { return mLastBlock; }
        }

        public string PATH
        {
            get
            {
                if (Directory.Exists(mPATH))
                {
                    return mPATH;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(mPATH));
                return mPATH;
            }
        }

        /// <summary>
        /// Carica l'ultimo blocco della blockchain.
        /// </summary>
        private void Load()
        {
            if (File.Exists(PATH+"blockchain.txt")) 
            {
                StreamReader file = new StreamReader(PATH + "blockchain.txt");
                mLastValidBlock = CBlock.Deserialize(file.ReadLine());
            }
            else
            {
                File.WriteAllText(PATH + "blockchain.txt", new CGenesisBlock().Serialize());
                StreamReader file = new StreamReader(PATH + "blockchain.txt");
                mLastValidBlock = CBlock.Deserialize(file.ReadLine());
            }
        }


        internal static bool Validate(CBlock b)
        {
            throw new System.NotImplementedException();
        }

        internal static void Add(CBlock b)
        {
            throw new System.NotImplementedException();
        }

        internal static void Add(CBlock[] b)
        {
            throw new System.NotImplementedException();
        }
    }
}