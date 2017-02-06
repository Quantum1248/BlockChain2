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

        private CPeer[] mPeers;
        private int mNumReserved;


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
            bool valid = false;
            for (int i = 0; i < mPeers.Length; i++)
            {
                valid = false;
                foreach (CPeer VldP in Peers)
                {
                    if (mPeers[i]?.IP == VldP.IP)
                    {
                        valid = true;
                    }
                }
                if(!valid)
                {
                    mPeers[i].Disconnect();
                    mPeers[i] = null;
                }
            }
        }

        public void InvalidPeers(CPeer[] Peers)
        {
            foreach (CPeer InvldP in Peers)
                for (int i = 0; i < mPeers.Length; i++)
                {
                    if (mPeers[i]?.IP == InvldP.IP)
                    {
                        mPeers[i].Disconnect();
                        mPeers[i] = null;
                    }
                }
        }

        /// <summary>
        /// Esegue una richiesta ai peer collegati.
        /// </summary>
        /// <param name="Rqs">Richiesta da effettuare.</param>
        /// <param name="Arg">Parametro usato per passare un valore e/o ritornare un risultato quando necessario.</param>
        /// <returns></returns>
        public object DoRequest(ERequest Rqs, object Arg = null)  //(!) rivedere i metodi di input/output del metodo
        {
            switch (Rqs)
            {
                case ERequest.UpdatePeers:
                    UpdatePeers();
                    break;
                case ERequest.SendPeersList:
                    SendPeersList(Arg as CPeer);
                    break;
                case ERequest.LastValidBlock:
                    return RequestLastValidBlock();
                case ERequest.DownloadMissingBlock:
                    object[] args = Arg as object[];
                    ulong startingIndex = Convert.ToUInt64(args[0]);
                    ulong finalIndex = Convert.ToUInt64(args[1]);
                    return DistribuiteDownloadBlocks(startingIndex, finalIndex);
                case ERequest.BroadcastMinedBlock:
                    CBlock b = Arg as CBlock;
                    foreach (CPeer p in mPeers)
                    {
                        if (p != null)
                        {
                            lock (p.Socket)
                            {
                                p.SendCommand(ECommand.LOOK);
                                if (p.ReceiveCommand() == ECommand.OK)
                                {
                                    p.SendCommand(ECommand.RCVMINEDBLOCK);
                                    p.SendBlock(b);
                                }
                            }
                        }
                    }
                    break;
                case ERequest.LastCommonValidBlock:
                    return FindLastCommonBlocks();
                default:
                    throw new ArgumentException("Invalid request: " + Rqs);
            }
            return null;
        }

        private CBlock FindLastCommonBlocks()
        {
            ulong minLength = ulong.MaxValue, tmp = 0;
            bool found = false;
            Stack<CHeader> headers = new Stack<CHeader>();
            CBlock res=null;
            foreach (CPeer p in mPeers)
            {
                if (p != null)
                {
                    lock (p.Socket)
                    {
                        p.SendCommand(ECommand.LOOK);
                        if (p.ReceiveCommand() == ECommand.OK)
                        {
                            p.SendCommand(ECommand.CHAINLENGTH);
                            tmp = p.ReceiveULong();
                        }
                        if (tmp < minLength)
                            minLength = tmp;
                    }
                }
            }

            CRange r = new CRange(CBlockChain.Instance.LastValidBlock.Header.BlockNumber, minLength);
            if (r.Start > r.End)
            {
                while (!found)
                {
                    found = true;
                    tmp = r.Start + r.End / 2;
                    foreach (CPeer p in mPeers)
                    {
                        if (p != null)
                        {
                            lock (p.Socket)
                            {
                                p.SendCommand(ECommand.LOOK);
                                if (p.ReceiveCommand() == ECommand.OK)
                                {
                                    p.SendCommand(ECommand.GETHEADER);
                                    p.SendULong(tmp);
                                    headers.Push(p.ReceiveHeader());
                                }
                            }
                        }
                    }
                    while (headers.Count > 1 && found)
                    {
                        if (!(headers?.Pop().Hash == headers?.Peek().Hash))
                            found = false;
                    }
                    //se tutti i blocchi sono uguali allora found=true, mentre se ce n'è qualcuno di diverso found=false
                    if (found)
                        r.Start = tmp;
                    else
                        r.End = tmp;
                    if (r.Start == r.End)
                        found = true;
                    else
                        found = false;
                }
                foreach (CPeer p in mPeers)
                {
                    if (p != null)
                    {
                        lock (p.Socket)
                        {
                            p.SendCommand(ECommand.LOOK);
                            if (p.ReceiveCommand() == ECommand.OK)
                            {
                                p.SendCommand(ECommand.DOWNLOADBLOCK);
                                p.SendULong(r.Start);
                                res = p.ReceiveBlock();
                            }
                            if (res != null && CBlockChain.Validate(res))
                                break;
                            else
                                p.Disconnect();
                        }
                    }
                }
                return res;
            }
            else
            {
                return CBlockChain.Instance.LastValidBlock;
            }
        }

        private void UpdatePeers()
        {
            string ris = "";
            string msg="";
            ECommand cmd;
            string[] listsPeer;
            string[] peers;
            CPeer receivedPeer;
            for (int i = 0; i < mPeers.Length; i++)
                if (mPeers[i] != null)
                {
                    //blocca il peer e manda una richiesta di lock per bloccarlo anche dal nel suo client, così che non avvengano interferenze nella comunicazione
                    lock (mPeers[i].Socket)
                    {
                        try
                        {
                            mPeers[i].Socket.ReceiveTimeout = 2000;
                            mPeers[i].SendCommand(ECommand.LOOK);
                            if (mPeers[i].ReceiveCommand() == ECommand.OK)
                            {
                                mPeers[i].SendCommand(ECommand.UPDPEERS);
                                msg = mPeers[i].ReceiveString();
                            }
                            ris += msg + "/";
                            // mPeers[i].SendData("ENDLOCK");
                        }
                        catch (SocketException e)
                        {
                            if (Program.DEBUG)
                                CIO.DebugOut("Nessuna risposta da " + mPeers[i].IP + " durante la richiesta dei peer.");
                        }
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
                        receivedPeer = DeserializePeer(rp);
                        if (receivedPeer.IP != publicIp && receivedPeer.IP != localIp)
                            Insert(receivedPeer);
                    }
                }

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
                            return false;

                    if (!Peer.IsConnected)
                        if (!Peer.Connect())
                            return false;
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
                    Peer.Disconnect();
                }
                return false;
            }
        }

        private static CPeer DeserializePeer(string Peer)
        {
            string[] peerField = Peer.Split(',');
            return CPeer.CreatePeer(peerField[0], Convert.ToInt32(peerField[1]));
        }

        private void SendPeersList(CPeer Peer)
        {
            string PeersList = "";
            for (int i = 0; i < mPeers.Length; i++)
            {
                if (mPeers[i] != null)
                    PeersList += mPeers[i].IP + "," + mPeers[i].Port + ";";
            }
            //non deve in viare il peer richiedente
            PeersList = PeersList.TrimEnd(';');
            Peer.SendString(PeersList);
        }

        private CTemporaryBlock RequestLastValidBlock()
        {
            List<CTemporaryBlock> blocks = new List<CTemporaryBlock>();
            CTemporaryBlock ris = null;
            ECommand cmd;
            string msg;

            foreach (CPeer p in mPeers)
            {
                if (p != null)
                {
                    lock (p.Socket)
                    {
                        p.SendCommand(ECommand.LOOK);
                        if (p.ReceiveCommand() == ECommand.OK)
                        {
                            p.SendCommand(ECommand.GETLASTVALID);
                            msg = p.ReceiveString();
                            blocks.Add(new CTemporaryBlock(CBlock.Deserialize(msg), p));
                        }
                    }
                }
            }
            if (blocks.Count > 0)
                if (blocks[0] != null)
                {
                    ris = blocks[0];
                    foreach (CTemporaryBlock b in blocks)
                    {
                        if (ris.Header.BlockNumber < b.Header.BlockNumber)
                            ris = b;
                    }
                }
            return ris;

        }

        public CTemporaryBlock[] DistribuiteDownloadBlocks(ulong initialIndex, ulong finalIndex, CPeer[] Peers=null)
        {
            if (Peers == null)
                Peers = mPeers;
            Queue<Thread> threadQueue=new Queue<Thread>();
            Queue<CRange> queueRange = new Queue<CRange>();
            CTemporaryBlock[] ris;
            ulong module=0, rangeDim=10, totalBlocks= finalIndex - initialIndex;
            ris = new CTemporaryBlock[totalBlocks];
            foreach(CPeer p in Peers)
                if(p!=null)
                    threadQueue.Enqueue(new Thread(new ParameterizedThreadStart(DownloadBlocks)));

            //creazione gruppi di blocchi
            //(!) genera 1-10/11-20 p 1-10/10-20? è giusto il secondo 
            //1-10 scarica i blocchi dall'1 compreso al 10 non compreso(1-2-3-4-5-6-7-8-9)
            module = totalBlocks % rangeDim;
            while(initialIndex<=finalIndex-module)
            {
                queueRange.Enqueue(new CRange(initialIndex, initialIndex + rangeDim));
                initialIndex += rangeDim;
            }
            initialIndex -= rangeDim;
            queueRange.Enqueue(new CRange(initialIndex, initialIndex + module));
            
            //creazione ed avvio thread
            foreach (CPeer p in Peers)
            {
                if(p!=null)
                {
                    threadQueue.Peek().Start(new object[] { p, queueRange ,ris});
                    threadQueue.Enqueue(threadQueue.Dequeue());
                }
            }

            while(threadQueue.Count>0)
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

            int c = 0;
            CRange rangeInDownload;
            ECommand cmd;
            string msg="";
            while (rangeAvailable.Count > 0)
            {
                c = 0;
                lock (rangeAvailable)
                {
                    if (rangeAvailable.Count <= 0)
                        break;
                    rangeInDownload = rangeAvailable.Dequeue();
                }
                lock (peer.Socket)
                {
                    peer.SendCommand(ECommand.LOOK);
                    if (peer.ReceiveCommand() == ECommand.OK)
                    {
                        peer.SendCommand(ECommand.DOWNLOADBLOCKS);
                        peer.SendULong(rangeInDownload.Start);
                        peer.SendULong(rangeInDownload.End);
                        msg = peer.ReceiveString();
                    }
                    foreach(string block in msg.Split('/'))
                    {
                        ris[rangeInDownload.Start + (ulong)c++] = new CTemporaryBlock(JsonConvert.DeserializeObject<CBlock>(block), peer);
                    }
                }
            }
        }



        public CHeader[] DistribuiteDownloadHeaders(ulong initialIndex, ulong finalIndex, CPeer[] Peers = null)
        {
            if (Peers == null)
                Peers = mPeers;
            ulong module = 0, rangeDim = 10, totalHeaders = finalIndex - initialIndex;
            Queue<Thread> threadQueue = new Queue<Thread>();
            Queue<CRange> queueRange = new Queue<CRange>();
            CHeader[] ris = new CHeader[totalHeaders];

            foreach (CPeer p in Peers)
                if (p != null)
                    threadQueue.Enqueue(new Thread(new ParameterizedThreadStart(DownloadHeaders)));

            //creazione gruppi di blocchi
            //(!) genera 1-10/11-20 p 1-10/10-20? è giusto il secondo 
            //1-10 scarica i blocchi dall'1 compreso al 10 non compreso(1-2-3-4-5-6-7-8-9)
            module = totalHeaders % rangeDim;
            while (initialIndex <= finalIndex - module)
            {
                queueRange.Enqueue(new CRange(initialIndex, initialIndex + rangeDim));
                initialIndex += rangeDim;
            }
            initialIndex -= rangeDim;
            queueRange.Enqueue(new CRange(initialIndex, initialIndex + module));

            //creazione ed avvio thread
            //(!) si blocca se qualcuno si disconnette mentre fa il ciclo credo(perchè un thread non farà mai finire il join)
            foreach (CPeer p in Peers)
            {
                if (p != null)
                {
                    threadQueue.Peek().Start(new object[] { p, queueRange, ris });
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

            int c = 0;
            CRange rangeInDownload;
            ECommand cmd;
            string msg;
            while (rangeAvailable.Count > 0)
            {
                c = 0;
                lock (rangeAvailable)
                {
                    if (rangeAvailable.Count <= 0)
                        break;
                    rangeInDownload = rangeAvailable.Dequeue();
                }
                lock (peer.Socket)
                {
                    peer.SendCommand(ECommand.DOWNLOADHEADERS);
                    peer.SendULong(rangeInDownload.Start);
                    peer.SendULong(rangeInDownload.End);
                    msg = peer.ReceiveString();
                    foreach (string header in msg.Split('/'))
                    {
                        ris[rangeInDownload.Start + (ulong)c++] = JsonConvert.DeserializeObject<CHeader>(header);
                    }
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
