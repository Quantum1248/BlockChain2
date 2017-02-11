using System.Collections.Generic;

namespace BlockChain
{
    class CParallelChain
    {
        private ulong mLength;
        private List<CPeer> mPeers;
        private CHeader[] mHeaders;
        private CTemporaryBlock[] mBlocks;
        public ulong InitialIndex, FinalIndex;

        public CParallelChain()
        {
            mLength = 0;
            mPeers = new List<CPeer>();
            mHeaders = new CHeader[0];
        }

        public CHeader this[ulong i]
        {
            get { return mHeaders[i]; }
        }

        public CPeer[] Peers
        {
            get { return mPeers.ToArray(); }
        }

        public ulong Length
        {
            get { return mLength; }
        }

        public CTemporaryBlock[] Blocks
        {
            get { return mBlocks; }
        }

        public void AddPeer(CPeer p)
        {
            mPeers.Add(p);
        }

        public void DownloadHeaders()
        {
            mHeaders = CPeers.Instance.DistribuiteDownloadHeaders(InitialIndex, FinalIndex, mPeers.ToArray());
            mLength =(ulong) mHeaders.Length;
        }

        public void DownloadBlocks()
        {
            mBlocks = CPeers.Instance.DistribuiteDownloadBlocks(InitialIndex, FinalIndex, mPeers.ToArray());
            mLength = (ulong)mBlocks.Length;
        }
    }
}