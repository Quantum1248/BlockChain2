using System.Collections.Generic;

namespace BlockChain
{
    class CHeaderChain
    {
        private ulong mLength;
        private List<CPeer> mPeers;
        private CHeader[] mHeaders;
        public ulong InitialIndex, FinalIndex;

        public CHeaderChain()
        {
            mLength = 0;
            mPeers = new List<CPeer>();
            mHeaders = new CHeader[0];
            InitialIndex = CBlockChain.Instance.LastValidBlock.Header.BlockNumber;
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

        public void AddPeer(CPeer p)
        {
            mPeers.Add(p);
        }

        public void DownloadHeaders()
        {
            mHeaders = CPeers.Instance.DistribuiteDownloadHeaders(InitialIndex+1, FinalIndex, mPeers.ToArray());
            mLength =(ulong) mHeaders.Length;
        }
    }
}