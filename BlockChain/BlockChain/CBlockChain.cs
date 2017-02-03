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
                            if (b.Header.BlockNumber > mLastValidBlock.Header.BlockNumber)
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
                        if (b.Header.BlockNumber == Index)
                            return b;
                    }
                }

            }
            return null;
        }
            

        public static bool ValidateHeaders(CHeaderChain HeaderChain)
        {
            for(ulong i=0;i<HeaderChain.Length;i++)
                if (HeaderChain[i].Hash != HeaderChain[i + 1].PreviusBlockHash && HeaderChain[i].BlockNumber != HeaderChain[i + 1].BlockNumber+1)//(!) il controllu sul numero serve?
                    return false;
            return true;
        }

        public static bool Validate(CBlock b)
        {
            return CMiner.Instance.Validate(b);
        }

        /// <summary>
        /// Aggiunge i blocchi presenti nel vettore e ritorna l'indice dell'ultimo blocco aggiunto.
        /// </summary>
        /// <param name="Blocks"></param>
        /// <returns></returns>
        public ulong Add(CTemporaryBlock[] Blocks)
        {
            ulong lastValidIndex = 0;
            string filepath = PATH + "\\" + FILENAME;
            //(!) e se scarico tutta la blockchain e da un certo punto in poi sbagliata?
            foreach (CTemporaryBlock b in Blocks)
            {
                if (b == null)
                    break;
                if (CMiner.Instance.Validate(b))
                {
                    File.AppendAllText(filepath, (b as CBlock).Serialize());
                }
                else
                {
                    lastValidIndex =Convert.ToUInt64(CPeers.Instance.DoRequest(ERequest.FindLastCommonIndex));
                    return LastValidBlock.Header.BlockNumber;
                }
            }
            return LastValidBlock.Header.BlockNumber;
        }

        public CHeaderChain BestChain(CHeaderChain[] HeaderChains)
        {
            //TODO sceglie in base alla difficoltà
            CHeaderChain res=new CHeaderChain();
            foreach (CHeaderChain hc in HeaderChains)
                if (hc.Length > res.Length)
                    res = hc;
            return res;
        }

        internal void Add(CTemporaryBlock newBlock)
        {
            throw new NotImplementedException();
        }

        
    }
}