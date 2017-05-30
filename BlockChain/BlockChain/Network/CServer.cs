using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
namespace BlockChain
{
    class CServer
    {
        //RSA
        //TODO: cambiare l'inizializzazione una volta definite le classi


        private Thread mUpdateBlockChainThread, mThreadListener, mThreadUpdatePeers, mMinerThread;

        public static RSACryptoServiceProvider rsaKeyPair;

        private static CServer mInstance;

        private CPeers mPeers;
        private static int MAX_PEERS = 30;//deve essere pari
        private static int RESERVED_CONNECTION = MAX_PEERS / 2;//connessioni usate per chi vuole collegarsi con me
        private static int NOT_RESERVED_CONNECTION = MAX_PEERS - RESERVED_CONNECTION;//connessioni che utilizzo io per collegarmi agli altri
        private static string mPublicIp = "";
        private Socket mListener;
        public static int DEFAULT_PORT = 4000;

        private bool IsStopped = false; //set true per fermare il server

        private CServer()
        {
            rsaKeyPair = RSA.GenRSAKey();// crea oggetto CSP per generare o caricare il keypair

            if (File.Exists(RSA.PATH + "\\keystore.xml"))// Se il file di keystore esiste viene caricato in memoria dal disco, altrimenti viene creato e salvato su disco
            {
                string xmlString = File.ReadAllText(RSA.PATH + "\\keystore.xml");
                rsaKeyPair.FromXmlString(xmlString);
            }
            else//se il file non esiste ne viene generato uno
            {
                string xmlString = rsaKeyPair.ToXmlString(true);
                File.WriteAllText(RSA.PATH + "\\keystore.xml", xmlString);
            }

            if (Program.DEBUG)
                CIO.DebugOut("Last block number: " + CBlockChain.Instance.LastValidBlock.Header.BlockNumber + ".");

        }

        public static CServer Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new CServer();
                }
                return mInstance;
            }
        }

        public void InitializePeersList(List<CPeer> peers)
        {
            if (Program.DEBUG)
            {
                CIO.DebugOut("Initialize mPeers...");
            }
            mPeers = new CPeers(MAX_PEERS, RESERVED_CONNECTION);
            if (Program.DEBUG)
            {
                CIO.DebugOut("Begin to enstablish connections to initial peers...");
            }
            //si collega ai peer inseriti nella lista iniziale.
            foreach (CPeer p in peers)
            {
                if (p.Connect(500))
                {
                    if (!mPeers.Insert(p))
                    {
                        break;
                    }
                }
            }

            if (Program.DEBUG)
            {
                CIO.DebugOut("Begin to enstablish connections to other peers...");
            }
            mThreadUpdatePeers = new Thread(new ThreadStart(UpdatePeersList));
            mThreadUpdatePeers.Start();

            if (Program.DEBUG)
            {
                CIO.DebugOut("Start listening...");
            }
            mThreadListener = new Thread(new ThreadStart(StartAcceptUsersConnection));
            mThreadListener.Start();
        }


        //sincronizza la blockchain
        public void SyncBlockchain()
        {
            if (Program.DEBUG)
                CIO.DebugOut("Start update blockchain...");
            mUpdateBlockChainThread = new Thread(new ThreadStart(UpdateBlockchain));
            mUpdateBlockChainThread.Start();
        }

        //avvia il thread del miner
        public void StartMining()
        {
            if (mMinerThread == null)
            {
                if (Program.DEBUG)
                    CIO.DebugOut("Start Miner...");
                mMinerThread = new Thread(new ThreadStart(StartMiner));
                mMinerThread.Start();
            }
        }

        //avvia il miner
        private void StartMiner()
        {
            if(mUpdateBlockChainThread!=null)
                mUpdateBlockChainThread.Join();
            Miner.Start(10);
        }

        private int ConnectedPeers
        {
            get { return mPeers.NumConnection(); }
        }



        private void UpdatePeersList()
        {
            while (!IsStopped)
            {
                int numPeers = mPeers.NumConnection();
                if (numPeers < NOT_RESERVED_CONNECTION && numPeers>0)
                    mPeers.DoRequest(ERequest.UpdatePeers);
                //inserire qui il controllo per verificare che i peer presenti siano ancora online?
                Thread.Sleep(60000);
            }
        }

        //attende il collegamento di nuovi peer
        private void StartAcceptUsersConnection()
        {
            if (Program.DEBUG)
                CIO.DebugOut("Initialize the Listener...");
            //crea un socket che attende connessioni in ingresso di peer che vogliono collegarsi, in ascolto sulla porta DEFOULT_PORT
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, DEFAULT_PORT);
            mListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mListener.Bind(localEndPoint);
            mListener.Listen(DEFAULT_PORT);

            //crea un eventargs per una richiesta di connessione asincrona, se la lista dei peers non è ancora piena inizia ad attendere fino a quando non riceve
            //una richiesta di connessione o il segnale d'arresto. Se viene ricevuta una richiesta di connessione viene chiamata la funzione InsertNewPeer che
            //inserisce il nuovo peer nella lista dei peer mPeers

            //è asincrono perchè altrimenti al segnale di spegnimento non si fermerebbe  
            SocketAsyncEventArgs asyncConnection;
            bool IncomingConnection = false;
            if (Program.DEBUG)
                CIO.DebugOut("Attending connection...");
            while (!IsStopped)
            {
                if (ConnectedPeers < MAX_PEERS)
                {
                    IncomingConnection = false;
                    asyncConnection = new SocketAsyncEventArgs();
                    asyncConnection.Completed += (object sender, SocketAsyncEventArgs e) => { IncomingConnection = true; };
                    mListener.AcceptAsync(asyncConnection);
                    while (!IncomingConnection && !IsStopped)
                    {
                        Thread.Sleep(1000);
                    }
                    if (IncomingConnection)
                    {
                        if (Program.DEBUG)
                            CIO.DebugOut("Established connection with "+ ((IPEndPoint)asyncConnection.AcceptSocket.RemoteEndPoint).Address+" !");
                        InsertNewPeer(asyncConnection.AcceptSocket);
                    }
                    asyncConnection.Dispose();
                }
                else
                {
                    Thread.Sleep(10000);
                }
            }
        }

        private void UpdateBlockchain()
        {
            bool isSynced = false;
            ulong addedBlocks = 0;
            CTemporaryBlock[] DownloadedBlock;
            CTemporaryBlock[] newBlocks;
            CHeaderChain[] forkChains;
            CHeaderChain bestChain;
            CBlock lastCommonBlock;
            CTemporaryBlock otherLastValidBlock;

            lastCommonBlock= mPeers.DoRequest(ERequest.LastCommonValidBlock) as CBlock;
            otherLastValidBlock = mPeers.DoRequest(ERequest.LastValidBlock) as CTemporaryBlock;

            if (Program.DEBUG)
                if (otherLastValidBlock != null)
                    CIO.DebugOut("Il numero di blocco di otherLastValidBlock è " + otherLastValidBlock.Header.BlockNumber + ".");
                else
                    CIO.DebugOut("Nessun otherLastValidBlock ricevuto.");

            if (CBlockChain.Instance.LastValidBlock.Header.BlockNumber < otherLastValidBlock?.Header.BlockNumber)
                isSynced = false;
            else
                isSynced = true;

            //TODO potrebbero dover essere scaricati un numero maggiore di MAXINT blocchi
            while (!isSynced)
            {
                newBlocks = mPeers.DoRequest(ERequest.DownloadMissingBlock, new object[] { CBlockChain.Instance.LastValidBlock.Header.BlockNumber + 1, lastCommonBlock.Header.BlockNumber + 1 }) as CTemporaryBlock[];
                CBlockChain.Instance.Add(newBlocks);
                forkChains = mPeers.DoRequest(ERequest.FindParallelChain, lastCommonBlock) as CHeaderChain[];
                if (forkChains.Length > 0)
                {
                    foreach (CHeaderChain hc in forkChains)
                        hc.DownloadHeaders();
                    bestChain = CBlockChain.Instance.BestChain(forkChains);
                    if (CValidator.ValidateHeaderChain(bestChain))
                    {
                        DownloadedBlock=CPeers.Instance.DistribuiteDownloadBlocks(bestChain.InitialIndex+1,bestChain.FinalIndex);
                        foreach(CTemporaryBlock block in DownloadedBlock)
                        {
                            UTXOManager.Instance.ApplyBlock(block);
                        }
                        mPeers.ValidPeers(bestChain.Peers);
                        addedBlocks = CBlockChain.Instance.Add(DownloadedBlock);
                        if (addedBlocks >= bestChain.Length)    //solo se scarica tutti i blocchi
                            isSynced = true;
                    }
                    else
                    {
                        mPeers.InvalidPeers(bestChain.Peers);
                        otherLastValidBlock = mPeers.DoRequest(ERequest.LastValidBlock) as CTemporaryBlock;
                        lastCommonBlock = mPeers.DoRequest(ERequest.LastCommonValidBlock) as CTemporaryBlock;
                    }
                }
            }
            if (Program.DEBUG)
                CIO.DebugOut("Sincronizzazione Blockchain terminata!");
            mPeers.CanReceiveBlock = true;
        }



        private void InsertNewPeer(Socket newConnection)
        {
            //crea un nuovo peer con un socket già collegato e una nuova connessione con questo peer, e la inserisce nel contenitore mConnections
            CPeer newPeer = CPeer.CreatePeer(Convert.ToString((newConnection.RemoteEndPoint as IPEndPoint).Address), (newConnection.RemoteEndPoint as IPEndPoint).Port, newConnection);
            mPeers.Insert(newPeer, true);
        }

        static public byte[] ReceiveData(Socket Receiving)
        {
            byte[] data = new byte[4];
            Receiving.Receive(data);
            Thread.Sleep(100);
            data = new byte[BitConverter.ToInt32(data, 0)];
            Receiving.Receive(data);
            return data;
        }

        static public void SendData(Socket Dispatcher, byte[] data)
        {
            Dispatcher.Send(BitConverter.GetBytes(data.Length).Concat(data).ToArray());
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        public static string GetPublicIPAddress()
        {
            try
            {
                if (mPublicIp == "")
                {
                    mPublicIp = new WebClient().DownloadString("http://icanhazip.com");
                }
                return mPublicIp.Trim('\n');
            }
            catch
            {
                if (Program.DEBUG)
                {
                    CIO.DebugOut("Stupido proxy schifoso autistico");
                }
                return "";
            }
        }
    }
}
