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

        //all'avvio del programma, si caricano in memoria le transazioni nell'UTXODB in modo da poterle cercare in modo veloce per essere confermate come non double spend
        public UTXOManager(string path)
        {
            this.HashTable = new Hashtable();

            if (Directory.Exists(path))
            {
                DirectoryInfo d = new DirectoryInfo(path);
                Transaction tmp;
                foreach (var file in d.GetFiles("*.json"))
                {
                    tmp = JsonConvert.DeserializeObject<Transaction>(File.ReadAllText(file.FullName));
                    try
                    {
                        this.HashTable.Add(tmp.Hash, file.FullName);
                    }
                    catch
                    {
                        Console.WriteLine("An element with Key = " + tmp.Hash + " already exists.");
                    }
                }
            }
        }

        public string GetTransactionPath(string hash)
        {
            return (string)this.HashTable[hash];
        }
    }
}
