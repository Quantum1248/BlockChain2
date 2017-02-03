namespace BlockChain
{
    class CHeaderChain
    {
        private ulong mLength;
        private CPeer[] mPeers;
        private CHeader[] mHeaders;
        private CTemporaryBlock[] mBlocks;
        public ulong InitialIndex, FinalIndex;

        public CHeaderChain()
        {
            mLength = 0;
            mPeers = null;
            mHeaders = new CHeader[0];
        }

        public CHeader this[ulong i]
        {
            get { return mHeaders[i]; }
        }

        public CPeer[] Peers
        {
            get { return mPeers; }
            set { mPeers = value; }
        }

        public ulong Length
        {
            get { return mLength; }
        }

        public CTemporaryBlock[] Blocks
        {
            get { return mBlocks; }
        }

        public void DownloadHeaders()
        {
            mHeaders = CPeers.Instance.DistribuiteDownloadHeaders(InitialIndex, FinalIndex, mPeers);
            mLength =(ulong) mHeaders.Length;
        }

        public void DownloadBlocks()
        {
            mBlocks = CPeers.Instance.DistribuiteDownloadBlocks(InitialIndex, FinalIndex, mPeers);
            mLength = (ulong)mBlocks.Length;
        }
    }
}