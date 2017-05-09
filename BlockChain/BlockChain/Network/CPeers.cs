using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;

namespace BlockChain
{
    class CPeers
    {
        public bool CanReceiveBlock = false;

        private CPeer[] mPeers; //contiene i peer collegati 
        private int mNumReserved;   //numero di slot riservati per le connessioni in ingresso


        #region Singleton

        private static CPeers instance;

        private CPeers() { }

        public static CPeers Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CPeers();
                }
                return instance;
            }
        }

        #endregion Singleton

        public CPeers(int NumConnections, int Reserved)
        {
            mPeers = new CPeer[NumConnections];
            mNumReserved = Reserved;

            instance = this;
        }

        public CPeer[] Peers
        {
            get
            {
                int c = 0;
                CPeer[] res = new CPeer[NumConnection()];
                foreach (CPeer p in mPeers)
                    if(p!=null)
                        res[c++] = p;
                return res;
            }
        }

        public int NumConnection()
        {
            int n = 0;
            for (int i = 0; i < mPeers.Length; i++)
            {
                if (mPeers[i] != null)
                    n++;
            }
            return n;
        }

        public void ValidPeers(CPeer[] Peers)
        {
            bool valid;
            for (int i = 0; i < mPeers.Length; i++)
            {
                if (mPeers[i] != null)
                {
                    valid = false;
                    foreach (CPeer VldP in Peers)
                    {
                        if (mPeers[i]?.IP == VldP.IP)
                        {
                            valid = true;
                        }
                    }
                    if (!valid)
                    {
                        mPeers[i].Disconnect();
                        mPeers[i] = null;
                    }
                }
            }
        }

        public void InvalidPeers(CPeer[] Peers)
        {
            foreach (CPeer InvldP in Peers)
                for (int i = 0; i < mPeers.Length; i++)
                {
                    if (mPeers[i]?.IP == InvldP.IP && mPeers[i].IsConnected)
                    {
                        mPeers[i].Disconnect();
                        mPeers[i] = null;
                    }
                    else if(mPeers[i]?.IP == InvldP.IP && !mPeers[i].IsConnected)
                        mPeers[i] = null;
                }
        }

        /// <summary>
        /// Esegue una richiesta ai peer collegati.
        /// </summary>
        /// <param name="rqs">Richiesta da effettuare.</param>
        /// <param name="arg">Parametro usato per passare un valore e/o ritornare un risultato quando necessario.</param>
        /// <returns></returns>
        public object DoRequest(ERequest rqs, object arg = null)  //(!) rivedere i metodi di input/output del metodo
        {
            switch (rqs)
            {
                case ERequest.UpdatePeers:
                    {
                        UpdatePeers();
                        break;
                    }
                case ERequest.LastValidBlock:
                    {
                        return RequestLastValidBlock();
                    }
                case ERequest.DownloadMissingBlock:
                    {
                        object[] args = arg as object[];
                        ulong startingIndex = Convert.ToUInt64(args[0]);
                        ulong finalIndex = Convert.ToUInt64(args[1]);
                        return DistribuiteDownloadBlocks(startingIndex, finalIndex);
                    }
                case ERequest.BroadcastMinedBlock:
                    {
                        CBlock b = arg as CBlock;
                        foreach (CPeer p in Peers)
                            p.SendRequest(new CMessage(EMessageType.Request, ERequestType.NewBlockMined, EDataType.Block, b.Serialize()));
                        break;
                    }
                case ERequest.LastCommonValidBlock:
                    {
                        return FindLastCommonBlocks();
                    }
                case ERequest.FindParallelChain:
                    {
                        return FindParallelChains(arg as CBlock);
                    }
                case ERequest.BroadcastNewTransaction:
                    {
                        foreach(CPeer p in Peers)
                            p.SendRequest(new CMessage(EMessageType.Request, ERequestType.NewTransaction, EDataType.Transaction, JsonConvert.SerializeObject(arg as Transaction))); //TODO : implementa richiesta di invio transazione
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid request: " + rqs);
            }
            return null;
        }

        private void UpdatePeers()
        {
            int id;
            string[] listsPeer;
            string[] peers;
            string msg = "";
            string ris = "";
            CPeer receivedPeer;
            for (int i = 0; i < mPeers.Length; i++)
                if (mPeers[i] != null)
                {
                    //blocca il peer e manda una richiesta di lock per bloccarlo anche dal nel suo client, così che non avvengano interferenze nella comunicazione
                    try
                    {
                        id = mPeers[i].SendRequest(new CMessage(EMessageType.Request, ERequestType.UpdPeers));
                        msg = mPeers[i].ReceiveData(id, 5000).Data;
                        ris += msg + "/";
                    }
                    catch (SocketException)
                    {
                        if (Program.DEBUG)
                            CIO.DebugOut("Nessuna risposta da " + mPeers[i].IP + " durante la richiesta dei peer.");
                    }
                }

            ris = ris.TrimEnd('/');

            if (ris != "")
            {
                string publicIp = CServer.GetPublicIPAddress();
                string localIp = CServer.GetLocalIPAddress();
                listsPeer = ris.Split('/');
                foreach (string l in listsPeer)
                {
                    peers = l.Split(';');
                    foreach (string rp in peers)
                    {
                        receivedPeer = CPeer.Deserialize(rp);
                        if (!(receivedPeer.IP == publicIp || receivedPeer.IP == localIp))
                            Insert(receivedPeer);
                    }
                }

            }
        }

        private CBlock FindLastCommonBlocks()
        {
            ulong minLength = ulong.MaxValue, tmp = 0;
            int rqsID = 0;
            bool found = false;
            Stack<CHeader> headers = new Stack<CHeader>();
            List<CTemporaryBlock> commonBlocks = new List<CTemporaryBlock>();
            int[] shareRation = new int[0];
            CTemporaryBlock res = null;
            foreach (CPeer p in mPeers)
                if (p != null)
                {
                    try
                    {
                        rqsID = p.SendRequest(new CMessage(EMessageType.Request, ERequestType.ChainLength));
                        tmp = p.ReceiveULong(rqsID, 5000);
                        if (tmp < minLength)
                            minLength = tmp;
                    }
                    catch (SocketException)
                    { }
                    p.Socket.ReceiveTimeout = 0;
                }
            CRange r = new CRange(CBlockChain.Instance.LastValidBlock.Header.BlockNumber, minLength);
            if (r.End != ulong.MaxValue && r.Start < r.End)
            {
                //trova l'ultimo blocco uguale tra i peer e salva l'indice di quel blocco in r.start
                int ID = 0;
                while (r.Start < r.End)
                {
                    found = true;
                    tmp = (r.Start + r.End) / 2;
                    if (r.End - r.Start == 1)
                        tmp++;
                    foreach (CPeer p in mPeers)
                        if (p != null)
                        {
                            ID = p.SendRequest(new CMessage(EMessageType.Request, ERequestType.GetHeader, EDataType.ULong, Convert.ToString(tmp)));
                            headers.Push(p.ReceiveHeader(ID, 5000));
                        }
                    while (headers.Count > 1 && found)
                        if (!(headers?.Pop().Hash == headers?.Peek().Hash))
                            found = false;
                    if (headers.Count == 1)
                        headers.Pop();
                    //se tutti i blocchi sono uguali allora found=true, mentre se ce n'è qualcuno di diverso found=false
                    if (found)
                        r.Start = tmp;
                    else if (!(r.End - r.Start == 1))
                        r.End = tmp;
                    else
                        r.End--;
                }

                lock (mPeers)
                {
                    shareRation = new int[Peers.Count()];
                    foreach (CPeer p in Peers)
                    {
                        ID = p.SendRequest(new CMessage(EMessageType.Request, ERequestType.DownloadBlock, EDataType.ULong, Convert.ToString(r.Start)));
                        res = new CTemporaryBlock(p.ReceiveBlock(ID, 5000), p);
                        for (int i = 0; i < commonBlocks.Count; i++)
                        {
                            if (res.Header.Hash == commonBlocks[i].Header.Hash)
                            {
                                shareRation[i]++;
                                res = null;
                            }
                        }
                        p.Socket.ReceiveTimeout = 0;
                    }
                }
                int resShareRation = -1;
                for (int i = 0; i < commonBlocks.Count-1; i++)
                {
                    if(resShareRation <= shareRation[i])
                    {
                        res = commonBlocks[i];
                    }
                }
                foreach(CTemporaryBlock tb in commonBlocks)
                {
                    if(tb.Header.Hash!=res.Header.Hash)
                    {
                        tb.Sender.Disconnect();
                    }
                }
                
                return res;
            }
            else
            {
                return CBlockChain.Instance.LastValidBlock;
            }
        }
        

        public bool Insert(CPeer Peer, bool IsReserved = false)
        {
            //ritorna true se riesce ad inserire il peer, mentre false se il vettore è pieno o il peer è già presente nella lista
            lock (mPeers) //rimane loccato se ritorno prima della parentesi chiusa??
            {
                if (NumConnection() < mPeers.Length)
                {
                    //controlla che la connessione(e quindi il peer) non sia già presente
                    foreach (CPeer p in mPeers)
                        if (p?.IP == Peer.IP)//e se ci sono più peer nella stessa rete che si collegano su porte diverse?
                        {
                            return false;
                        }

                    if (!Peer.IsConnected)
                        if (!Peer.Connect(2000))
                        {
                            Peer.Disconnect();
                            return false;
                        }
                    //controlla se è il peer che si è collegato a me o sono io che mi sono collegato al peer
                    if (IsReserved)
                        for (int i = mPeers.Length - 1; i > 0; i--)
                        {
                            if (mPeers[i] == null)
                            {
                                mPeers[i] = Peer;
                                mPeers[i].StartListening();
                                return true;
                            }
                        }
                    else
                    {
                        for (int i = 0; i < mNumReserved; i++)
                            if (mPeers[i] == null)
                            {
                                mPeers[i] = Peer;
                                return true;
                            }
                    }
                }
                Peer.Disconnect();
                return false;
            }
        }

        public string PeersList()
        {
            string PeersList = "";
            for (int i = 0; i < mPeers.Length; i++)
            {
                if (mPeers[i] != null)
                    PeersList += mPeers[i].IP + "," + mPeers[i].Port + ";";
            }
            //non deve in viare il peer richiedente
            PeersList = PeersList.TrimEnd(';');
            return PeersList;
        }

        private CTemporaryBlock RequestLastValidBlock()
        {
            List<CTemporaryBlock> blocks = new List<CTemporaryBlock>();
            CTemporaryBlock ris = null;
            CBlock block;
            int ID = 0;
            foreach (CPeer p in mPeers)
                if (p != null)
                {
                    ID = p.SendRequest(new CMessage(EMessageType.Request, ERequestType.GetLastValid));
                    block = p.ReceiveBlock(ID, 5000);
                    blocks.Add(new CTemporaryBlock(block, p));
                }
            if (blocks.Count > 0)
                if (blocks[0] != null)
                {
                    ris = blocks[0];
                    foreach (CTemporaryBlock b in blocks)
                        if (ris.Header.BlockNumber < b.Header.BlockNumber)
                            ris = b;
                }
            return ris;

        }

        private CHeaderChain[] FindParallelChains(CBlock startBlock)
        {
            CHeader tmp1, tmp2;
            Stack<CHeaderChain> res = new Stack<CHeaderChain>();
            int ID=0;

            for (int i = 0; i < Peers.Length; i++)
                if (Peers[i] != null)
                {
                    ID= Peers[i].SendRequest(new CMessage(EMessageType.Request, ERequestType.GetLastHeader));
                    tmp1 = Peers[i].ReceiveHeader(ID, 5000);
                    res.Push(new CHeaderChain());
                    res.Peek().AddPeer(Peers[i]);
                    res.Peek().FinalIndex = tmp1.BlockNumber;
                    Peers[i] = null;
                    for (int j = i + 1; j < Peers.Length; j++)
                    {
                        ID = Peers[j].SendRequest(new CMessage(EMessageType.Request, ERequestType.GetLastHeader));
                        tmp2 = Peers[j].ReceiveHeader(ID, 5000);
                        if (tmp1.Hash == tmp2.Hash)
                        {
                            res.Peek().AddPeer(Peers[j]);
                            Peers[j] = null;
                        }
                    }
                }
            return res.ToArray();
        }

        public CTemporaryBlock[] DistribuiteDownloadBlocks(ulong initialIndex, ulong finalIndex, CPeer[] Peers = null)
        {
            if (Peers == null)
                Peers = mPeers;
            Queue<Thread> threadQueue = new Queue<Thread>();
            Queue<CRange> queueRange = new Queue<CRange>();
            CTemporaryBlock[] ris;
            ulong module = 0, rangeDim = 10, totalBlocks = finalIndex - initialIndex;
            ulong rangeInitialIndex = initialIndex;
            ris = new CTemporaryBlock[totalBlocks];
            foreach (CPeer p in Peers)
                if (p != null)
                    threadQueue.Enqueue(new Thread(new ParameterizedThreadStart(DownloadBlocks)));

            //creazione gruppi di blocchi
            //1-10 scarica i blocchi dall'1 compreso al 10 non compreso(1-2-3-4-5-6-7-8-9)
            module = totalBlocks % rangeDim;
            queueRange.Enqueue(new CRange(finalIndex - module, finalIndex));
            finalIndex -= module;
            while (rangeInitialIndex < finalIndex)
                queueRange.Enqueue(new CRange(rangeInitialIndex, rangeInitialIndex += rangeDim));

            //creazione ed avvio thread
            foreach (CPeer p in Peers)
            {
                if (p != null)
                {
                    threadQueue.Peek().Start(new object[] { p, queueRange, ris, initialIndex });
                    threadQueue.Enqueue(threadQueue.Dequeue());
                }
            }

            while (threadQueue.Count > 0)
            {
                threadQueue.Dequeue().Join();
            }

            return ris;
        }

        private void DownloadBlocks(object obj)
        {
            object[] args = obj as object[];
            CPeer peer = args[0] as CPeer;
            Queue<CRange> rangeAvailable = args[1] as Queue<CRange>;
            CTemporaryBlock[] ris = args[2] as CTemporaryBlock[];
            ulong offset =Convert.ToUInt64(args[3]);
            int c = 0, ID=0;
            CRange rangeInDownload;
            CBlock[] msg;
            while (rangeAvailable.Count > 0)
            {
                c = 0;
                lock (rangeAvailable)
                {
                    if (rangeAvailable.Count <= 0)
                        break;
                    rangeInDownload = rangeAvailable.Dequeue();
                }
                ID = peer.SendRequest(new CMessage(EMessageType.Request, ERequestType.DownloadBlocks, EDataType.ULongList, Convert.ToString(rangeInDownload.Start) + ";" + Convert.ToString(rangeInDownload.End)));
                msg =JsonConvert.DeserializeObject<CBlock[]>(peer.ReceiveData(ID, 5000).Data);
                foreach (CBlock block in msg)
                    ris[rangeInDownload.Start - offset + (ulong)c++] = new CTemporaryBlock(block, peer);
            }
        }

        public CHeader[] DistribuiteDownloadHeaders(ulong initialIndex, ulong finalIndex, CPeer[] Peers = null)
        {
            if (finalIndex < initialIndex)
                return new CHeader[0];
            if (Peers == null)
                Peers = mPeers;
            ulong module = 0, rangeDim = 10, totalHeaders = finalIndex - initialIndex;
            Queue<Thread> threadQueue = new Queue<Thread>();
            Queue<CRange> queueRange = new Queue<CRange>();
            ulong rangeInitialIndex = initialIndex;
            CHeader[] ris = new CHeader[totalHeaders];
            foreach (CPeer p in Peers)
                if (p != null)
                    threadQueue.Enqueue(new Thread(new ParameterizedThreadStart(DownloadHeaders)));

            //creazione gruppi di blocchi
            //(!) genera 1-10/11-20 p 1-10/10-20? è giusto il secondo 
            //1-10 scarica i blocchi dall'1 compreso al 10 non compreso(1-2-3-4-5-6-7-8-9)
            module = totalHeaders % rangeDim;
            queueRange.Enqueue(new CRange(finalIndex - module, finalIndex));
            finalIndex -= module;
            while (rangeInitialIndex < finalIndex)
                queueRange.Enqueue(new CRange(rangeInitialIndex, rangeInitialIndex += rangeDim));

            //creazione ed avvio thread
            //(!) si blocca se qualcuno si disconnette mentre fa il ciclo credo(perchè un thread non farà mai finire il join)
            foreach (CPeer p in Peers)
            {
                if (p != null)
                {
                    threadQueue.Peek().Start(new object[] { p, queueRange, ris, initialIndex });
                    threadQueue.Enqueue(threadQueue.Dequeue());
                }
            }

            while (threadQueue.Count > 0)
            {
                threadQueue.Dequeue().Join();
            }

            return ris;
        }

        private void DownloadHeaders(object obj)
        {
            object[] args = obj as object[];
            CPeer peer = args[0] as CPeer;
            Queue<CRange> rangeAvailable = args[1] as Queue<CRange>;
            CHeader[] ris = args[2] as CHeader[];
            ulong offset = Convert.ToUInt64(args[3]);
            int c = 0, ID = 0;
            CRange rangeInDownload;
            CHeader[] msg;
            while (rangeAvailable.Count > 0)
            {
                c = 0;
                lock (rangeAvailable)
                {
                    if (rangeAvailable.Count <= 0)
                        break;
                    rangeInDownload = rangeAvailable.Dequeue();
                }

                ID = peer.SendRequest(new CMessage(EMessageType.Request, ERequestType.DownloadHeaders, EDataType.ULongList, Convert.ToString(rangeInDownload.Start) + ";" + Convert.ToString(rangeInDownload.End)));
                msg = JsonConvert.DeserializeObject<CHeader[]>(peer.ReceiveData(ID, 5000).Data);

                foreach (CHeader header in msg)
                {
                    ris[rangeInDownload.Start -offset + (ulong)c++] = header;
                }
            }
        }
    }
}

class CRange
{
    public ulong Start;
    public ulong End;

    public CRange(ulong Start, ulong End)
    {
        this.Start = Start;
        this.End = End;
    }

}
