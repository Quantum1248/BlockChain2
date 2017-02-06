using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    //nella classe MemPool vanno inserite le transazioni non confermate. SOLO un client che sta minando inserirà le transazioni nella MemPool.
    //Va inizializzata all' inizio del mining, ogni transazione confermata va inserita qui
    class MemPool
    {
        public Queue<Transaction> TxQueue;

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
            this.TxQueue = new Queue<Transaction>();
        }

        public void AddUTX(Transaction utx)
        {
            this.TxQueue.Enqueue(utx);
        }

        public Transaction GetUTX(string utxHash)
        {
            foreach(Transaction tx in this.TxQueue)
            {
                if(utxHash == tx.Hash)
                {
                    this.TxQueue.Dequeue();
                    return tx;
                }
            }
            return null;
            
        }

        public void RemoveUTX(string utxHash)
        {
            foreach (Transaction tx in this.TxQueue)
            {
                if (utxHash == tx.Hash)
                {
                    this.TxQueue.Dequeue();
                    return;
                }
            }
            
        }

        public void DumpBlock(CBlock block)
        {
            foreach(Transaction tx in block.Transactions)
            {
                if(!(tx.inputs.Count == 0))
                {
                    this.AddUTX(tx);
                }
            }
        }

        public List<Transaction> GetUTX(int utxLimit)
        {
            List<Transaction> utxList = new List<Transaction>();
            for(int i = 0; i < utxLimit; i++)
            {
                utxList.Add(this.TxQueue.Dequeue());
            }
            return utxList;
        }
    }
}
