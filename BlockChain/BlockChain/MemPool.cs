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
        public List<Transaction> TxList;

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
            this.TxList = new List<Transaction>();
        }

        public void AddUTX(Transaction utx)
        {
            this.TxList.Add(utx);
        }

        public Transaction GetUTX(string utxHash)
        {
            foreach(Transaction tx in this.TxList)
            {
                if(utxHash == tx.Hash)
                {
                    this.TxList.Remove(tx);
                    return tx;
                }
            }
            return null;
            
        }

        public void RemoveUTX(string utxHash)
        {
            foreach (Transaction tx in this.TxList)
            {
                if (utxHash == tx.Hash)
                {
                    this.TxList.Remove(tx);
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
    }
}
