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
        public static RSACryptoServiceProvider rsaKeyPair;


        private Thread mUpdateBlockChainThread;
        private CPeers mPeers;
        private static int MAX_PEERS = 30;//deve essere pari
        private static int RESERVED_CONNECTION = MAX_PEERS / 2;//connessioni usate per chi vuole collegarsi con me
        private static int NOT_RESERVED_CONNECTION = MAX_PEERS - RESERVED_CONNECTION;//connessioni che utilizzo io per collegarmi agli altri
        private static string mPublicIp = "";
        private Thread mThreadListener, mThreadPeers;
        private Socket mListener;
        private static int DEFOULT_PORT = 2000;

        private bool IsStopped = false; //set true per spegnere il server

        private CServer(List<CPeer> Peers)
        {
            rsaKeyPair = RSA.GenRSAKey();// crea oggetto CSP per generare o caricare il keypair

            if (File.Exists(RSA.PATH + "\\keystore.xml"))// Se il file di keystore esiste viene caricato in memoria
            {
                string xmlString = File.ReadAllText(RSA.PATH + "\\keystore.xml");
                rsaKeyPair.FromXmlString(xmlString);
            }
            else//se il file non esiste ne viene generato uno
            {
                string xmlString = rsaKeyPair.ToXmlString(true);
                File.WriteAllText(RSA.PATH + "\\keystore.xml", xmlString);
            }

            //TODO: testare la verifica e la creazione delle transazioni con le nuove funzioni e modifiche implementate in Transaction.cs e UTXOManager.cs
            //TODO: testare nuovi metodi di encoding in RSA.cs, non vogliamo che si fottano tutte le firme digitali e annesse verifiche, o no?
            {
                /*
                Output[] outputs;

                Transaction tx; ;
                UTXOManager.Instance.SpendUTXO("314f04b30f62e0056bd059354a5536fb2e302107eed143b5fa2aa0bbba07f608", @"8yeeMidRStH4QvdNAr6fzwaaJ92hlSpcplki/KRSjy8=", 0);

                for (int i = 0; i < 1000000; i += 3)
                {
                    outputs[k] = new Output(1.4242, Utilities.ByteArrayToHexString(SHA256Managed.Create().ComputeHash(Encoding.ASCII.GetBytes(((i + k).ToString())))));
                }
                tx = new Transaction(outputs, RSA.ExportPubKey(rsaKeyPair), rsaKeyPair);
            }*/

                if (Program.DEBUG)
                    CIO.DebugOut("Last block number: " + CBlockChain.Instance.LastValidBlock.Header.BlockNumber + ".");

                if (Program.DEBUG)
                    CIO.DebugOut("Initialize mPeers...");
                mPeers = new CPeers(MAX_PEERS, RESERVED_CONNECTION);

                if (Program.DEBUG)
                    CIO.DebugOut("Finish initializing!");
                Start(Peers);
            }
        }

        public static CServer StartNewServer(List<CPeer> Peers)
        {
            if (Peers?.Count > 0)
                return new CServer(Peers);
            return null;
        }

        private int ConnectedPeers
        {
            get { return mPeers.NumConnection(); }
        }

        private void Start(List<CPeer> Peers)
        {
            if (Program.DEBUG)
                CIO.DebugOut("Begin to enstablish connections to initial peers...");
            //si collega ai peer inseriti nella lista iniziale.
            foreach (CPeer p in Peers)
                if (p.Connect(500))
                    if (!mPeers.Insert(p))
                        break;
            
            if (Program.DEBUG)
                CIO.DebugOut("Begin to enstablish connections to other peers...");
            mThreadPeers = new Thread(new ThreadStart(UpdatePeersList));
            mThreadPeers.Start();
            
            if (Program.DEBUG)
                CIO.DebugOut("Start listening...");
            mThreadListener = new Thread(new ThreadStart(StartAcceptUsersConnection));
            mThreadListener.Start();
            
            if (Program.DEBUG)
                CIO.DebugOut("Start update blockchain...");
            mUpdateBlockChainThread = new Thread(new ThreadStart(UpdateBlockchain));
            mUpdateBlockChainThread.Start();
            
        }

        private void UpdatePeersList()
        {
            while (!IsStopped)
            {
                int numPeers = mPeers.NumConnection();
                if (numPeers < NOT_RESERVED_CONNECTION && numPeers>0)
                    mPeers.DoRequest(ERequest.UpdatePeers);
                //inserire qui il controllo per verificare che i peer presenti siano ancora online?
                Thread.Sleep(300);
            }
        }

        //attende il collegamento di nuovi peer
        private void StartAcceptUsersConnection()
        {
            if (Program.DEBUG)
                CIO.DebugOut("Initialize the Listener...");
            //crea un socket che attende connessioni in ingresso di peer che vogliono collegarsi, in ascolto sulla porta DEFOULT_PORT
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, DEFOULT_PORT);
            mListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mListener.Bind(localEndPoint);
            mListener.Listen(DEFOULT_PORT);

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
            CTemporaryBlock[] newBlocks;
            CParallelChain[] forkChains;
            CParallelChain bestChain;
            CBlock lastCommonBlock= mPeers.DoRequest(ERequest.LastCommonValidBlock) as CBlock;
            CTemporaryBlock otherLastValidBlock = mPeers.DoRequest(ERequest.LastValidBlock) as CTemporaryBlock;
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
                newBlocks = mPeers.DoRequest(ERequest.DownloadMissingBlock, new object[] { CBlockChain.Instance.LastValidBlock.Header.BlockNumber+1, lastCommonBlock.Header.BlockNumber +1 }) as CTemporaryBlock[];
                CBlockChain.Instance.Add(newBlocks);
                forkChains = mPeers.DoRequest(ERequest.FindParallelChain, lastCommonBlock) as CParallelChain[];
                if (forkChains.Length > 0)
                {
                    foreach (CParallelChain hc in forkChains)
                        hc.DownloadHeaders();
                    bestChain = CBlockChain.Instance.BestChain(forkChains);
                    if (CBlockChain.ValidateHeaders(bestChain))
                    {
                        bestChain.DownloadBlocks();
                        mPeers.ValidPeers(bestChain.Peers);
                        addedBlocks = CBlockChain.Instance.Add(bestChain.Blocks);
                        if (addedBlocks >= bestChain.Length)    //solo se scarica tutti i blocchi
                        {
                            isSynced = true;
                            mPeers.CanReceiveBlock = true;
                        }
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
            //(!) da cambiare

            while (true)
                Miner.AddProof(new CBlock(CBlockChain.Instance.LastBlock.Header.BlockNumber + 1, CBlockChain.Instance.LastBlock.Header.Hash,2));        //(!) da cambiare a seconda di come verrà fattp il miner


        }

        private void InsertNewPeer(Socket NewConnection)
        {
            //crea un nuovo peer con un socket già collegato e una nuova connessione con questo peer, e la inserisce nel contenitore mConnections
            CPeer newPeer = CPeer.CreatePeer(Convert.ToString((NewConnection.RemoteEndPoint as IPEndPoint).Address), (NewConnection.RemoteEndPoint as IPEndPoint).Port, NewConnection);
            mPeers.Insert(newPeer, true);
        }

        static public byte[] ReceiveData(Socket Receiving)
        {
            byte[] data = new byte[4];
            Receiving.Receive(data);
            data = new byte[BitConverter.ToInt32(data, 0)];
            Receiving.Receive(data);
            return data;
        }

        static public void SendData(Socket Dispatcher, byte[] data)
        {
            if (data.Length < 1)
                throw new Exception();
            Dispatcher.Send(BitConverter.GetBytes(data.Length));
            Dispatcher.Send(data);
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
            if(mPublicIp=="")
                mPublicIp= new WebClient().DownloadString("http://icanhazip.com");
            return mPublicIp;
        }
    }
}
