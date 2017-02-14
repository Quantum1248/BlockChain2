using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    static class CValidator
    {
        public static bool ValidateMessage(CMessage Msg)
        {
            if (Msg.Type == EMessageType.Request)
            {
                switch(Msg.RqsType)
                {
                    case ERequestType.UPDPEERS:
                        if (Msg.DataType == default(EDataType) && Msg.Data == null)
                            return true;
                        else
                            return false;
                    default:
                        return false;
                }
            }
            else if (Msg.Type == EMessageType.Data)
            {
                switch (Msg.DataType)
                {
                    case EDataType.PeersList:
                        if (Msg.RqsType == default(ERequestType))
                        {
                            try
                            {
                                string[] peers = Msg.Data.Split(';');
                                foreach (string rp in peers)
                                    CPeer.Deserialize(rp);
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                        else
                            return false;
                    default:
                        return false;
                }
            }
            return false;
        }
    }
}
