using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    static class CValidator
    {

        public static bool ValidateBlock(CBlock b)
        {
            if (b.Header.PreviousBlockHash == CBlockChain.Instance.RetriveBlock(b.Header.BlockNumber - 1).Header.Hash)
            {
                string toHash = b.Header.PreviousBlockHash + b.Nonce + b.Timestamp + b.MerkleRoot;
                if (b.Header.Hash == Utilities.ByteArrayToString(SCrypt.ComputeDerivedKey(Encoding.ASCII.GetBytes(toHash), Encoding.ASCII.GetBytes(toHash), 1024, 1, 1, 1, 32)))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ValidateMessage(CMessage Msg)
        {
            if (Msg.Type == EMessageType.Request)
                return ValidateRequest(Msg);
            else if (Msg.Type == EMessageType.Data)
                return ValidateData(Msg);
            return false;
        }

        private static bool ValidateRequest(CMessage Msg)
        {
            switch (Msg.RqsType)
            {
                case ERequestType.UpdPeers:
                    if (Msg.DataType == default(EDataType) && Msg.Data == null)
                        return true;
                    else
                        return false;
                case ERequestType.NewBlockMined:
                    if (Msg.DataType == EDataType.Block && Msg.Data != null)
                    {
                        try
                        {
                            CBlock.Deserialize(Msg.Data);
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

        private static bool ValidateData(CMessage Msg)
        {
            switch (Msg.DataType)
            {
                case EDataType.PeersList:
                    if (Msg.RqsType == default(ERequestType))
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
                    else
                        return false;
                default:
                    return false;
            }
        }
            

        
    }
}
