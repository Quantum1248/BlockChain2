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

        const string FILENAME = "blockchain.txt";
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
            string filepath = PATH + "\\" + FILENAME;
            mLastValidBlock = new CGenesisBlock();
            if (File.Exists(filepath))
            {
                StreamReader streamReader = new StreamReader(filepath);
                using (JsonTextReader reader = new JsonTextReader(streamReader))
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            // Load each object from the stream and do something with it

                            JObject obj = JObject.Load(reader);

                            JsonSerializer serializer = new JsonSerializer();
                            CBlock b = (CBlock)serializer.Deserialize(new JTokenReader(obj), typeof(CBlock));
                            if (b.BlockNumber > mLastValidBlock.BlockNumber)
                                mLastValidBlock = b;
                        }
                    }

                }
            }
            else
            {
                File.WriteAllText(filepath, new CGenesisBlock().Serialize());
                mLastValidBlock = new CGenesisBlock();
            }
        }

        public CBlock RetriveBlock(ulong Index)
        {
            string filepath = PATH + "\\" + FILENAME;
            StreamReader streamReader = new StreamReader(filepath);
            using (JsonTextReader reader = new JsonTextReader(streamReader))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        // Load each object from the stream and do something with it

                        JObject obj = JObject.Load(reader);

                        JsonSerializer serializer = new JsonSerializer();
                        CBlock b = (CBlock)serializer.Deserialize(new JTokenReader(obj), typeof(CBlock));
                        if (b.BlockNumber == Index)
                            return b;
                    }
                }

            }
            return null;
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