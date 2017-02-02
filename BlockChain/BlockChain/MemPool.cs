using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    //nella classe MemPool vanno inserite le transazioni non confermate. SOLO un client che sta minando inserirà le transazioni nella MemPool
    class MemPool
    {
        public Hashtable HashTable;

        private static MemPool instance;

        public static MemPool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MemPool();
                }
                return instance;
            }
        }

        private MemPool()
        {
            this.HashTable = new Hashtable();
        }

        public void AddUTX(Transaction utx)
        {
            this.HashTable.Add(utx.Hash, utx);
        }

        public Transaction GetUTX(string utxHash)
        {
            return (Transaction)this.HashTable[utxHash];
        }

        public void RemoveUTX(string utxHash)
        {
            this.HashTable.Remove(utxHash);
        }
    }
}
