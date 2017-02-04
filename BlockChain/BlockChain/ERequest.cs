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
        LastBlockNumber,
        UpdateBlockchain,
        DownloadMissingBlock,
        LastValidBlock,
        DownloadMissingValidBlock,
        DownloadSixtyBlock,
        BroadcastMinedBlock,
        FindLastCommonIndex,
        LastCommonValidBlock,
        FindForkChain,
        SendNewBlock
    }
}
