using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    enum ECommand
    {
        UPDPEERS,
        GETLASTVALID,
        DOWNLOADBLOCK,
        DOWNLOADBLOCKS,
        RCVMINEDBLOCK,
        DISCONNETC,
        DOWNLOADHEADERS,
        GETHEADER,
        CHAINLENGTH,
        GETLASTHEADER
    }
}
