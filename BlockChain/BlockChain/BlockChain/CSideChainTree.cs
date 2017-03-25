using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CTree<T>
    {
        protected T mRoot;
        protected List<CTree<T>> mChildren;

        public CTree()
        {
            Root = default(T);
            mChildren = new List<CTree<T>>();
        }
        public CTree(T TreeRoot)
        {
            Root = TreeRoot;
            mChildren = new List<CTree<T>>();
        }

        public T Root
        {
            get { return mRoot; }
            set { mRoot = value; }
        }

        public List<CTree<T>> Children
        {
            get { return mChildren; }
            set { mChildren = value; }
        }

        public List<T> GetNodeByLevel(int Level)
        {
            List<T> res = new List<T>();
            if (Level > 0)
                foreach (CTree<T> t in mChildren)
                    res.AddRange(t.GetNodeByLevel(Level - 1));
            else if (Level == 0)
                res.Add(mRoot);
            return res;
        }

        public List<T> AllNode(bool firstBlock=false)
        {
            List<T> blocks = new List<T>();
            if (firstBlock)
            {
                blocks.Add(mRoot);
            }
            foreach (CTree<T> t in mChildren)
            {
                blocks.AddRange(t.AllNode(true));
            }
            return blocks;
        }
    }

    class CSideChainTree : CTree<CTemporaryBlock>
    {
        public int RelativeDepth = 0, MaxDepth = 0; //MaxDepth è la profondità massima dell'albero(una volta raggiunta si effettua lo switch), RelativeDepth è la profondità relativa a questo nodo

        public CSideChainTree()
        {
            this.Root = null;
            this.MaxDepth = 0;
        }

        public CSideChainTree(CTemporaryBlock Root, int MaxDepth)
        {
            this.Root = Root;
            this.MaxDepth = MaxDepth;
        }

        public CTemporaryBlock GetLastBlock()
        {
            //(!) Gestire i casi in cui le sidechain hanno la stessa lunghezza
            if (mChildren.Count <= 0)
                return mRoot;
            else
            {
                CSideChainTree deepest = new CSideChainTree();
                foreach (CSideChainTree sc in mChildren)
                    if (sc.RelativeDepth >= deepest.RelativeDepth)
                        deepest = sc;
                return deepest.GetLastBlock();
            }
        }

        /// <summary>
        /// Aggiunge il blocco alla sidechain corretta ed effettua lo switch alla sidechain più lunga se necessario.
        /// </summary>
        /// <param name="newBlock">Blocco da aggiungere.</param>
        /// <returns></returns>
        public bool Add(CTemporaryBlock newBlock)
        {
            /*
            Se non c'è nulla nell'albero mette il blocco nella root, altrimenti esegue mAdd e se la nuova profondità dell'albero è
            maggiore alla massima consentita esegue lo switch tra le chain, aggiungendo la root ai blocchi sicuri.
            */

            if (mRoot == null)
            {
                mRoot = new CTemporaryBlock(CBlockChain.Instance.LastValidBlock,null);
            }
            int newDepth = mAdd(newBlock, 1);
            if (RelativeDepth < newDepth)
                RelativeDepth = newDepth;
            if (newDepth >= MaxDepth)
            {
                foreach (CSideChainTree t in mChildren)
                    if (t.RelativeDepth >= this.MaxDepth - 1)
                    {
                        CBlockChain.Instance.Add(new CTemporaryBlock[] { t.Root });
                        MemPool.Instance.RemoveBlock(t.Root);
                        UTXOManager.Instance.ApplyBlock(t.Root);
                        this.Root = t.Root;
                        this.Children = t.Children;
                    }

            }
            if (newDepth > -1)
                return true;
            else
                return false;
        }

        public CTemporaryBlock RetriveBlock(ulong blockNumber)
        {
            CTemporaryBlock last = GetLastBlock();
            List<CTemporaryBlock> validChain = GetChainFor(last);
            foreach (CTemporaryBlock b in validChain)
                if (b.Header.BlockNumber == blockNumber)
                    return b;
            return null;
        }

        private int mAdd(CTemporaryBlock newBlock, int depth)
        {
            /*
            Se l'hash del blocco precedente di newBlock è uguale all'hash della root, aggiunge newBlock in una nuova sidechain
            figlia di questo nodo e se è il primo figlio del nodo setta la profondità relativa a questo nodo a 1.
            Altrimenti prova ad aggiungere newBlock ad ogni sidechain figlia di questo nodo e se trova quella giusta setta la 
            nuova profondità relativa al valore corretto.
            */
            int newDepth;
            if (newBlock.Header.PreviousBlockHash == Root.Header.Hash)
            {
                foreach (CSideChainTree cs in mChildren)
                    if (cs.Root.Header.Hash == newBlock.Header.Hash)
                        return depth;
                mChildren.Add(new CSideChainTree(newBlock, MaxDepth));
                if (RelativeDepth < 1)
                    RelativeDepth = 1;
                return depth;
            }
            else
                foreach (CSideChainTree t in mChildren)
                {
                    newDepth = t.mAdd(newBlock, depth + 1);
                    if (newDepth > 0)
                    {
                        if ((newDepth - depth) + 1 > RelativeDepth)
                            RelativeDepth = newDepth - depth + 1;
                        return newDepth;
                    }
                }
            return -1;
        }

        public List<Transaction> AllDistinctTransaction()
        {
            List<Transaction> transactions = new List<Transaction>();
            List<CTemporaryBlock> tmpBlocks = AllNode();
            foreach (CTemporaryBlock b in tmpBlocks)
            {
                foreach (Transaction tx in b.Transactions)
                {
                    if (!transactions.Contains(tx))
                    {
                        transactions.Add(tx);
                    }
                } 
            } 
            return transactions;
        }

        private List<CTemporaryBlock> GetChainFor(CTemporaryBlock lastBlock)
        {
            List<CTemporaryBlock> res = new List<CTemporaryBlock>(), tmp;
            res.Add(mRoot);
            if (mRoot.Header.Hash == lastBlock.Header.Hash)
                return res;
            else
                foreach(CSideChainTree sc in mChildren)
                {
                    tmp = sc.GetChainFor(lastBlock);
                    if (tmp != null)
                    {
                        res.AddRange(tmp);
                        return res;
                    }
                }
            return null;
        }


    }
}
