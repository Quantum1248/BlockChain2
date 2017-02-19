using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlockChain
{
    static class CValidator
    {

        public static bool ValidateBlock(CBlock b, bool CheckPreviusHash = false)
        {

            //devo tenere conto della difficoltà?
            if (b.Header.Hash == Miner.Hash(b))
            {
                if (!CheckPreviusHash)
                    return true;
                else if (CheckPreviusHash)
                    if (CBlockChain.Instance.RetriveBlock(b.Header.BlockNumber - 1)?.Header.Hash == b.Header.PreviousBlockHash)
                        return true;
                    else
                        return false;
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
                    {
                        if (Msg.DataType == EDataType.NULL && Msg.Data == null)
                            return true;
                        else
                            return false;
                    }
                case ERequestType.NewBlockMined:
                    {
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
                    }
                case ERequestType.ChainLength:
                    {
                        if (Msg.DataType == EDataType.NULL && Msg.Data == null)
                            return true;
                        else
                            return false;
                    }
                case ERequestType.GetHeader:
                    {
                        if (Msg.DataType == EDataType.ULong)
                        {
                            try { Convert.ToInt64(Msg.Data); }
                            catch { return false; }
                            return true;
                        }
                        else
                            return false;
                    }
                case ERequestType.GetLastHeader:
                    {
                        {
                            if (Msg.DataType == EDataType.NULL && Msg.Data == null)
                                return true;
                            else
                                return false;
                        }
                    }
                case ERequestType.GetLastValid:
                    {
                        {
                            if (Msg.DataType == EDataType.NULL && Msg.Data == null)
                                return true;
                            else
                                return false;
                        }
                    }
                case ERequestType.DownloadBlocks:
                    {
                        if (Msg.DataType == EDataType.ULong && Msg.Data != null)
                        {
                            try
                            {
                                string[] stringToConvert = Msg.Data.Split(';');
                                if (stringToConvert.Length != 2)
                                    return false;
                                foreach (string s in stringToConvert)
                                    Convert.ToUInt64(s);
                                return true;
                            }
                            catch { return false; }
                        }
                        return false;
                    }
                case ERequestType.DownloadHeaders:
                    {
                        if (Msg.DataType == EDataType.ULong && Msg.Data != null)
                        {
                            try
                            {
                                string[] stringToConvert = Msg.Data.Split(';');
                                if (stringToConvert.Length != 2)
                                    return false;
                                foreach (string s in stringToConvert)
                                    Convert.ToUInt64(s);
                                return true;
                            }
                            catch { return false; }
                        }
                        return false;
                    }
                default:
                    return false;
            }
        }

        private static bool ValidateData(CMessage Msg)
        {
            if (Msg.RqsType != ERequestType.NULL)
                return false;
            switch (Msg.DataType)
            {
                case EDataType.PeersList:
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
                case EDataType.Block:
                    try
                    {
                        CBlock.Deserialize(Msg.Data);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case EDataType.ULong:
                    try
                    {
                        Convert.ToUInt64(Msg.Data);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case EDataType.ULongList:
                    try
                    {
                        JsonConvert.DeserializeObject<ulong[]>(Msg.Data);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case EDataType.Header:
                    try
                    {
                        JsonConvert.DeserializeObject<CHeader>(Msg.Data);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case EDataType.BlockList:
                    try
                    {
                        JsonConvert.DeserializeObject<CBlock[]>(Msg.Data);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case EDataType.HeaderList:
                    try
                    {
                        JsonConvert.DeserializeObject<CHeader[]>(Msg.Data);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }



    }
}
