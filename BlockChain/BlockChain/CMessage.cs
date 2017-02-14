using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CMessage
    {
        public EMessageType Type;
        public ERequestType RqsType;
        public EDataType DataType;
        public string Data;
        public int ID;
        public DateTime TimeOfReceipt;
        public CMessage()
        {
            Type = default(EMessageType);
            Data = null;
            ID = 0;
        }

        public CMessage(EMessageType Type, string Data)
        {
            this.Type = Type;
            this.Data = Data;
        }
    }

    enum EMessageType
    {
        Request,
        Data
    }

    public enum ERequestType
    {
        NULL,
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

    public enum EDataType
    {
        NULL,
        PeersList,
    }
}
