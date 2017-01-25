using System;
using System.IO;

namespace BlockChain
{
    class CBlockChain
    {
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
            get { return mLastValidBlock; }
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
            if (File.Exists(PATH + "\\blockchain.txt"))
            {
                string filepath = PATH + "\\blockchain.txt";
                using (StreamReader sr = new StreamReader(filepath))
                {
                    string line;
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (sr.Peek() == -1)
                        {
                            mLastValidBlock = CBlock.Deserialize(line);

                        }
                    }
                }

                   // mLastValidBlock = CBlock.Deserialize(File.ReadLines(PATH + "blockchain.txt").Last());
            }
            else
            {
                File.WriteAllText(PATH + "\\blockchain.txt", new CGenesisBlock().Serialize());
                StreamReader file = new StreamReader(PATH + "\\blockchain.txt");
                mLastValidBlock = CBlock.Deserialize(file.ReadLine());
            }
        }


        internal static bool Validate(CBlock b)
        {
            throw new System.NotImplementedException();
        }

        public int Add(CBlock[] b)
        {
            //throw new System.NotImplementedException();
            return b.Length;
        }
    }
}