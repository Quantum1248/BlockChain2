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
        }
        public CTree(T TreeRoot)
        {
            Root = TreeRoot;
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
            List<T> res=new List<T>();
            if(Level>0)
                foreach(CTree<T> t in mChildren)
                    res.AddRange(t.GetNodeByLevel(Level - 1));
            else if(Level==0)
                res.Add(mRoot);
            return res;
        }
    }

    class CSideChainTree:CTree<CBlock>
    {
        public int ChildDepth=0,MaxDepth = 0;

        public CSideChainTree(CBlock Root, int MaxDepth)
        {
            this.Root = Root;
            this.MaxDepth = MaxDepth;
        }

        /// <summary>
        /// Aggiunge il blocco alla sidechain corretta ed effettua lo switch alla sidechain più lunga se necessario.
        /// </summary>
        /// <param name="b">Blocco da aggiungere.</param>
        /// <returns></returns>
        public bool Add(CBlock b)
        {


            if (mAdd(b,1)>=MaxDepth)
            {
                foreach (CSideChainTree t in mChildren)
                    if (t.ChildDepth >= this.MaxDepth - 1)
                    {
                        CBlockChain.Instance.Add(new CBlock[] { mRoot });
                        this.Root = t.Root;
                        this.Children = t.Children;
                    }
                return true;
            }
            else
                return false;
        }

        private int mAdd(CBlock b,int Level)
        {
            int tmp;
            if (b.PreviusBlock.Hash == Root.Hash)
            {
                mChildren.Add(new CSideChainTree(b, MaxDepth));
                if (ChildDepth < 1)
                    ChildDepth = 1;
                return Level;
            }
            else
                foreach (CSideChainTree t in mChildren)
                {
                    tmp = t.mAdd(b, Level + 1);
                    if (tmp > 0)
                    {
                        if ((tmp-Level)+1 > ChildDepth)
                            ChildDepth = tmp - Level;
                        return tmp;
                    }
                }
            return -1;
        }
    }
}
