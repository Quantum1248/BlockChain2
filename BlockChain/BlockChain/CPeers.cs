using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public void DoRequest(ERequest Rqs, object Arg = null)  //(!) rivedere i metodi di input/output del metodo
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
                    RequestLastValidBlock();
                    break;
                default:
                    throw new ArgumentException("Invalid request.");
            }
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
                        p.SendCommand(ECommand.GET);
                        cmd = p.ReceiveCommand();
                        if (cmd == ECommand.OK)
                        {
                            p.SendCommand(ECommand.LASTVALID);
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
                        if (ris.BlockNumber < b.BlockNumber)
                            ris = b;
                    }
                }
            return ris;

        }

    }
}

