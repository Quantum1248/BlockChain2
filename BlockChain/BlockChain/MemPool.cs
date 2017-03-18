using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{

    /// <summary>
    /// nella classe MemPool vanno inserite le transazioni non confermate. SOLO un client che sta minando inserirà le transazioni nella MemPool.
    /// Va inizializzata all' inizio del mining, ogni transazione confermata va inserita qui
    /// </summary>
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

        /// <summary>
        /// Aggiunge una transazione alla mempool
        /// </summary>
        /// <param name="utx">La transazione da inserire</param>
        public void AddUTX(Transaction utx)
        {
            this.TxQueue.Enqueue(utx);
        }

        /// <summary>
        /// Ritorna una transazione dato il suo hash
        /// </summary>
        /// <param name="utxHash">L'hash della transazione da ritornare</param>
        /// <returns></returns>
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
        /// <summary>
        /// Rimuove una transazione dalla mempool
        /// </summary>
        /// <param name="utxHash">L'hash della transazione da rimuovere</param>
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
        /// <summary>
        /// Inserisce le transazioni di un dato blocco nella mempool
        /// </summary>
        /// <param name="block">Il blocco da inserire</param>
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

        /// <summary>
        /// Ritorna una lista di transazioni
        /// </summary>
        /// <param name="utxLimit">Il limite di transazioni da ritornare</param>
        /// <returns></returns>
        public List<Transaction> GetUTX(int utxLimit)
        {
            List<Transaction> utxList = new List<Transaction>();
            for (int i = 0; i < utxLimit && this.TxQueue.Count > 0; i++)
            {
                utxList.Add(this.TxQueue.Dequeue());
            }
            return utxList;
        }
        /// <summary>
        /// Controlla l'esistenza nella mempool di una transazione data
        /// </summary>
        /// <param name="transaction">La transazione da cercare nella mempool</param>
        /// <returns>Ritorna true se la transazione è presente nella mempool</returns>
        public bool CheckDouble(Transaction transaction)
        {
            if (this.TxQueue.Contains(transaction))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Rimuove dalla mempool tutte le transazioni contenute in un blocco dato
        /// </summary>
        /// <param name="block"></param>
        public void RemoveBlock(CBlock block)
        {
            foreach(Transaction tx in block.Transactions)
            {
                if (this.CheckDouble(tx))
                {
                    this.RemoveUTX(tx.Hash);
                }
            }
        }
        /// <summary>
        /// Fa un controllo all'interno della mempool per verificare se la transazione data spende input già spesi ma non ancora applicati all'utxodb
        /// </summary>
        /// <param name="transaction">La transazione da confrontare con la mempool</param>
        /// <returns>False se non ci sono double spending</returns>
        public bool CheckDoubleSpending(Transaction transaction)
        {
            List<Input> spentInputs = new List<Input>();
            foreach(Transaction tx in this.TxQueue)
            {
                foreach(Input input in tx.inputs)
                {
                    spentInputs.Add(input);
                }
            }
            foreach(Input input in transaction.inputs)
            {
                if (spentInputs.Contains(input))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
