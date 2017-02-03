using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace BlockChain
{
    class UTXOManager
    {
        public Hashtable HashTable;

        private static UTXOManager instance;

        public static UTXOManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UTXOManager();
                }
                return instance;
            }
        }
        //all'avvio del programma, si caricano in memoria le transazioni nell'UTXODB in modo da poterle cercare in modo veloce per essere confermate come non double spend
        private UTXOManager()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string path = Path.Combine(appDataFolder, "Blockchain\\UTXODB");
            this.HashTable = new Hashtable();
            if (Directory.Exists(path))
            {
                DirectoryInfo d = new DirectoryInfo(path);
                UTXO tmp;
                foreach (var file in d.GetFiles("*.json"))
                {
                    tmp = JsonConvert.DeserializeObject<UTXO>(File.ReadAllText(file.FullName));
                    foreach(Output output in tmp.Output)
                    {
                        if (output != null)
                        {
                            SetTransactionPath(output.PubKeyHash, file.FullName);
                        }
                    }
                }
            }
        }

        public void ApplyBlock(CBlock block)
        {
            foreach (Transaction tx in block.Transactions)
            {
                foreach(Input input in tx.inputs)
                {
                    this.RemoveUTXO(tx.PubKey, input.TxHash, input.OutputIndex);

                }
                this.AddUTXO(new UTXO(tx.Hash, tx.outputs));

            }
        }

        public void AddUTXO(UTXO utxo)
        {
            this.SetTransactionPath(utxo);
        }


        public void SetTransactionPath(string hash, string filename)
        {
            List<string> pathList = (List<string>)HashTable[hash];
            if(pathList == null)
            {
                pathList = new List<string>();
            }
            pathList.Add(filename);
            HashTable[hash] = pathList;
        }

        public void SetTransactionPath(UTXO utxo)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(appDataFolder, "Blockchain\\UTXODB");
            string filename = specificFolder + "\\" + utxo.TxHash + ".json";
            if (Directory.Exists(specificFolder))
            {

                File.WriteAllText(filename, utxo.Serialize());
            }
            else
            {
                Directory.CreateDirectory(specificFolder);
                File.WriteAllText(filename, utxo.Serialize());
            }
            List<string> pathList = (List<string>)HashTable[utxo.TxHash];
            if (pathList == null)
            {
                pathList = new List<string>();
            }
            pathList.Add(filename);
            HashTable[utxo.TxHash] = pathList;
        }

        public Output GetUTXO(string hash, string txHash, int outputIndex)
        {
            List<string> pathList = (List<string>)HashTable[hash];
            UTXO utxo;
            foreach(string path in pathList)
            {
                utxo = JsonConvert.DeserializeObject<UTXO>(File.ReadAllText(path));
                if(utxo.TxHash == txHash)
                {
                    return utxo.Output[outputIndex];
                }
            }
            return null;
        }

        public Output RetrieveRemoveUTXO(string hash, string txHash, int outputIndex)
        {
            Output output;
            List<string> pathList = (List<string>)HashTable[hash];
            if(pathList.Count == 0)
            {
                return null;
            }
            UTXO utxo;
            foreach (string path in pathList)
            {
                utxo = JsonConvert.DeserializeObject<UTXO>(File.ReadAllText(path));
                if (utxo.TxHash == txHash)
                {
                    output = utxo.Output[outputIndex];
                    utxo.Output[outputIndex] = null;
                    if(utxo.Output.Length == 0)
                    {
                        File.Delete(path);
                    }
                    else
                    {
                        File.WriteAllText(path, utxo.Serialize());
                    }
                    return output;
                }
            }
            return null;
        }

        public void RemoveUTXO(string hash, string txHash, int outputIndex)
        {
            Output output;
            List<string> pathList = (List<string>)HashTable[hash];
            if (pathList.Count == 0)
            {
                return;
            }
            UTXO utxo;
            foreach (string path in pathList)
            {
                utxo = JsonConvert.DeserializeObject<UTXO>(File.ReadAllText(path));
                if (utxo.TxHash == txHash)
                {
                    output = utxo.Output[outputIndex];
                    utxo.Output[outputIndex] = null;
                    if (utxo.Output.Length == 0)
                    {
                        File.Delete(path);
                    }
                    else
                    {
                        File.WriteAllText(path, utxo.Serialize());
                    }
                    return;
                }
            }
        }
        public List<UTXO> GetUTXObyHash(string hash)
        {
            List<string> paths = (List<string>)this.HashTable[hash];
            List<UTXO> utxos = new List<UTXO>();
            foreach(string path in paths)
            {
                utxos.Add(UTXO.Deserialize(File.ReadAllText(path)));
            }
            return utxos;
        }
    }
}
