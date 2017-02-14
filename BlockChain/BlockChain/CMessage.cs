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
        private ERequestType mRqsType;
        public EDataType DataType;
        public string Data;
        public int ID;
        public bool WillReceiveResponse;
        public DateTime TimeOfReceipt;
        public CMessage()
        {
            Type = default(EMessageType);
            RqsType = ERequestType.NULL;
            DataType = EDataType.NULL;
            Data = null;
            ID = 0;
            WillReceiveResponse = true;
        }

        public CMessage(EMessageType Type, ERequestType RqsType) : this()
        {
            this.Type = Type;
            this.RqsType = RqsType;
        }

        public CMessage(EMessageType Type, ERequestType RqsType, EDataType DataType ,string Data) : this()
        {
            this.Type = Type;
            this.RqsType = RqsType;
            this.DataType = DataType;
            this.Data = Data;
        }

        public ERequestType RqsType
        {
            get { return mRqsType; }
            set
            {
                if (value == ERequestType.NewBlockMined)
                    WillReceiveResponse = false;
                mRqsType = value;
            }
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
        UpdPeers,
        GETLASTVALID,
        DOWNLOADBLOCK,
        DOWNLOADBLOCKS,
        DISCONNETC,
        DOWNLOADHEADERS,
        GETHEADER,
        CHAINLENGTH,
        GETLASTHEADER,
        NewBlockMined
    }

    public enum EDataType
    {
        NULL,
        PeersList,
        Block,
    }
}
