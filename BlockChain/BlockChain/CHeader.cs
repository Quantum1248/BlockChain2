namespace BlockChain
{
    public class CHeader
    {
        public ulong BlockNumber;
        public string Hash;
        public string PreviousBlockHash;

        public CHeader()
        { }

        public CHeader(ulong BlockNumber, string Hash, string PreviusBlockHash)
        {
            this.BlockNumber = BlockNumber;
            this.Hash = Hash;
            this.PreviousBlockHash = PreviusBlockHash;
        }

        public CHeader(ulong BlockNumber, string PreviusBlockHash)
        {
            this.BlockNumber = BlockNumber;
            this.PreviousBlockHash = PreviusBlockHash;
        }
    }
}