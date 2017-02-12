using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    enum ERequest
    {
        UpdatePeers,
        SendPeersList,
        LastValidBlock,
        DownloadMissingBlock,
        BroadcastMinedBlock,
        LastCommonValidBlock,
        FindParallelChain
    }
}
