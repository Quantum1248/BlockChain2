using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace BlockChain
{
    class CPeers
    {
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

        public bool Insert(CPeer Peer, bool IsReserved = false)
        {
            //ritorna true se riesce ad inserire il peer, mentre false se il vettore è pieno o il peer è già presente nella lista
            lock (mPeers) //rimane loccato se ritorno prima della parentesi chiusa??
            {
                //controlla che la connessione(e quindi il peer) non sia già presente
                foreach (CPeer p in mPeers)
                    if (p?.IP == Peer.IP)//e se ci sono più peer nella stessa rete che si collegano su porte diverse?
                        return false;

                //controlla se è il peer che si è collegato a me o sono io che mi sono collegato al peer
                if (IsReserved)
                    for (int i = mPeers.Length - 1; i > 0; i--)
                    {
                        if (mPeers[i] == null)
                        {
                            mPeers[i] = Peer;
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
                return false;
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
                        p.SendCommand(ECommand.LOOK);
                        if (p.ReceiveCommand() == ECommand.OK)
                        {
                            p.SendCommand(ECommand.RCVMINEDBLOCK);
                            p.SendBlock(b);
                        }

                    }
                    break;
                default:
                    throw new ArgumentException("Invalid request.");
            }
            return null;
        }

        private void UpdatePeers()
        {
            string ris = "";
            string msg;
            ECommand cmd;
            string[] lists;
            string[] peers;
            List<CPeer> receivedPeers = new List<CPeer>(), newPeers = new List<CPeer>();
            for (int i = 0; i < mPeers.Length; i++)
            {
                if (mPeers[i] != null)
                {
                    //blocca il peer e manda una richiesta di lock per bloccarlo anche dal nel suo client, così che non avvengano interferenze nella comunicazione
                    lock (mPeers[i].Socket)
                    {
                        mPeers[i].SendCommand(ECommand.LOOK); //(!)in realtà non serve a niente?
                        cmd = mPeers[i].ReceiveCommand();
                        if (cmd == ECommand.OK)
                        {
                            mPeers[i].SendCommand(ECommand.UPDPEERS);
                            msg = mPeers[i].ReceiveString();
                            ris += msg + "/";
                        }
                        // mPeers[i].SendData("ENDLOCK");
                    }
                }
            }
            ris = ris.TrimEnd('/');

            if (ris != "")
            {
                lists = ris.Split('/');
                foreach (string l in lists)
                {
                    peers = l.Split(';');
                    foreach (string p in peers)
                    {
                        receivedPeers.Add(DeserializePeer(p));
                    }
                }


                bool AlreadyPresent = false;
                //controlla tutti i peer ricevuti presenti in receivedPeers e li mette ogni peer in newPeers solo se non è un doppione e se ci si è riusciti a collegarcisi
                foreach (CPeer rp in receivedPeers)
                {
                    foreach (CPeer np in newPeers)
                        if (rp.IP == np.IP)
                        {
                            AlreadyPresent = true;
                            break;
                        }
                    if (!AlreadyPresent)
                        if (rp.Connect())
                            newPeers.Add(rp);
                    AlreadyPresent = false;
                }
                //inserisce tutti i nuovi peer GIà COLLEGATI
                foreach (CPeer p in newPeers)
                    if (!this.Insert(p))
                        break;
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
                    p.SendCommand(ECommand.LOOK);
                    cmd = p.ReceiveCommand();
                    if (cmd == ECommand.OK)
                    {
                        p.SendCommand(ECommand.GETLASTVALID);
                        msg = p.ReceiveString();
                        blocks.Add(new CTemporaryBlock(CBlock.Deserialize(msg), p));
                    }
                }
            }
            if (blocks.Count > 0)
                if (blocks[0] != null)
                {
                    ris = blocks[0];
                    foreach (CTemporaryBlock b in blocks)
                    {
                        if (ris.BlockNumber < b.BlockNumber)
                            ris = b;
                    }
                }
            return ris;

        }

        private CTemporaryBlock[] DistribuiteDownloadBlocks(ulong initialIndex, ulong finalIndex)
        {
            Queue<Thread> threadQueue=new Queue<Thread>();
            Queue<CRange> queueRange = new Queue<CRange>();
            CTemporaryBlock[] ris;
            ulong module=0, rangeDim=10, totalBlocks= finalIndex - initialIndex;
            ris = new CTemporaryBlock[totalBlocks];
            foreach(CPeer p in mPeers)
            {
                if(p!=null)
                {
                    threadQueue.Enqueue(new Thread(new ParameterizedThreadStart(DownloadBlocks)));

                }
            }

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
            foreach (CPeer p in mPeers)
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
                peer.SendCommand(ECommand.LOOK);
                cmd = peer.ReceiveCommand();
                if(cmd==ECommand.OK)
                {
                    peer.SendCommand(ECommand.DOWNLOADBLOCKS);
                    peer.SendULong(rangeInDownload.Start);
                    peer.SendULong(rangeInDownload.End);
                    msg = peer.ReceiveString();
                    foreach(string block in msg.Split('/'))
                    {
                        ris[rangeInDownload.Start + (ulong)c++] = new CTemporaryBlock(JsonConvert.DeserializeObject<CBlock>(block), peer);
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
