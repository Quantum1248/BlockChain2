using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace BlockChain
{
    class CPeer
    {
        private IPAddress mIp;
        private int mPort;
        private Socket mSocket;
        //TODO: cambiare l'inizializzazione una volta definite le classi
        private RSACryptoServiceProvider csp = null;
        private RSACryptoServiceProvider cspMine;
        private Thread mThreadListener;
        private Thread mThreadRequest;
        private bool mIsConnected;

        private Queue<CMessage> RequestQueue = new Queue<CMessage>();
        private Queue<CMessage> DataQueue = new Queue<CMessage>();
        private List<int> ValidID = new List<int>();//forse non serve
        private static Random Rnd=new Random();
        public DateTime LastCommunication = DateTime.Now;

        #region Constructors&Properties&Inizialization

        private CPeer()
        {
        }

        private CPeer(IPAddress IP_Address, int Port)
        {
            mIp = IP_Address;
            mPort = Port;
            //IPEndPoint peerEndPoint = new IPEndPoint(IP_Address,Port);    Serve per fare il connect
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mIsConnected = false;
        }

        private CPeer(IPAddress IP_Address, int Port, Socket Sck)
        {
            mIp = IP_Address;
            mPort = Port;
            mSocket = Sck;
            mIsConnected = true;
        }

        public static CPeer CreatePeer(string IP_Address, int Port)
        {
            //Ritorna l'oggetto solo se i parametri sono corretti
            IPAddress ip;
            if (IPAddress.TryParse(IP_Address, out ip))
                return new CPeer(ip, Port);
            else
                return null;

        }

        public static CPeer CreatePeer(string IP_Address, int Port, Socket Sck)
        {
            IPAddress ip;
            if (IPAddress.TryParse(IP_Address, out ip))
            {
                return new CPeer(ip, Port, Sck);
            }
            else
                return null;
        }

        public string IP
        {
            get { return Convert.ToString(mIp); }
        }

        public int Port
        {
            get { return mPort; }
        }

        public Socket Socket
        {
            get { return mSocket; }
        }
        

        public bool IsConnected
        {
            get { return mIsConnected; }
        }

        public bool Connect(int Timeout)
        {
            if (Program.DEBUG)
                CIO.DebugOut("Connecting to " + mIp + ":" + mPort);
            SocketAsyncEventArgs asyncConnection = new SocketAsyncEventArgs();
            bool SuccessfulConnected = false;
            asyncConnection.Completed += (object sender, SocketAsyncEventArgs e) => { SuccessfulConnected = true; };
            asyncConnection.RemoteEndPoint = new IPEndPoint(mIp, mPort);
            mSocket.ConnectAsync(asyncConnection);
            Thread.Sleep(Timeout);
            if (SuccessfulConnected)
            {
                if (Program.DEBUG)
                    CIO.DebugOut("Connection with " + mIp + ":" + mPort + " enstablished!");
                mIsConnected = true;
                StartListening();
                return true;
            }
            else
            {
                if (Program.DEBUG)
                    CIO.DebugOut("Connection with " + mIp + ":" + mPort + " failed!");
                mSocket.Close();
                mSocket.Dispose();
                asyncConnection.Dispose();
                return false;
            }
        }

        public void Disconnect()
        {
            mSocket.Close();
            mSocket.Dispose();//(!) in teoria è inutile perchè fa già tutto Close()
            mIsConnected = false;
            CPeers.Instance.InvalidPeers(new CPeer[] { this });        
        }

        public void StartListening()
        {
            mThreadRequest = new Thread(new ThreadStart(ExecuteRequest));
            mThreadRequest.Start();
            mThreadListener = new Thread(new ThreadStart(Listen));
            mThreadListener.Start();

        }

        #endregion Constructors&Properties&Inizialization

        /// <summary>
        /// Rimane in attesa di messaggi dal peer a cui è collegato il socket mSocket.
        /// </summary>
        private void Listen()
        {
            string tmp;
            CMessage msg;
            while (mIsConnected)    //bisogna bloccarlo in qualche modo all'uscita del programma credo
            {
                //il timer viene settato cosicchè in caso non si ricevino comunicazioni venga ritornata un'eccezione, in modo che il programma vada avanti e tolga il lock al socket.
                mSocket.ReceiveTimeout = 1000;
                try
                {
                    tmp = ReceiveString();
                    msg = JsonConvert.DeserializeObject<CMessage>(tmp);
                    if (CValidator.ValidateMessage(msg))
                    {
                        msg.TimeOfReceipt = DateTime.Now;
                        if (msg.Type == EMessageType.Request)
                            lock (RequestQueue)
                                RequestQueue.Enqueue(msg);
                        else if (msg.Type == EMessageType.Data && ValidID.Contains(msg.ID))
                            lock (DataQueue)
                            {
                                DataQueue.Enqueue(msg);
                                ValidID.Remove(msg.ID);
                            }
                        else if (Program.DEBUG)
                            throw new ArgumentException("MessageType " + msg.Type + " non supportato.");
                    }
                    else
                        Disconnect();
                }
                catch (SocketException)
                {
                }
                catch (JsonSerializationException)
                {
                    throw new Exception();
                    Disconnect();
                }
                //il timer viene reinpostato a defoult per non causare problemi con altre comunicazioni che potrebbero avvenire in altre parti del codice.
                mSocket.ReceiveTimeout = 0;
            }
        }

        private void ExecuteRequest()
        {
            CMessage rqs;
            while (mIsConnected)
            {
                Thread.Sleep(100);
                lock (RequestQueue)
                {
                    if (RequestQueue.Count > 0)
                    {
                        rqs = RequestQueue.Dequeue();
                        switch (rqs.RqsType)
                        {
                            case ERequestType.UpdPeers:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("UpdPeers received by " + mIp);
                                    //(!) è meglio farsi ritornare la lista e poi usare json?
                                    SendRequest(new CMessage(EMessageType.Data, ERequestType.NULL, EDataType.PeersList,
                                        CPeers.Instance.PeersList(),rqs.ID));
                                    break;
                                }
                            case ERequestType.NewBlockMined:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("NewBlockMined received by " + mIp);
                                    if (CPeers.Instance.CanReceiveBlock)
                                    {
                                        CTemporaryBlock newBlock = new CTemporaryBlock(CBlock.Deserialize(rqs.Data), this);
                                        if (!CValidator.ValidateBlock(newBlock) && newBlock.Header.BlockNumber<CBlockChain.Instance.LastValidBlock.Header.BlockNumber)
                                        {
                                            Disconnect();
                                            break;
                                        }
                                        //TODO scaricare i blocchi mancanti se ne mancano(sono al blocco 10 e mi arriva il blocco 50)
                                        if(!CBlockChain.Instance.AddNewMinedBlock(newBlock))
                                        {
                                            Stack<CTemporaryBlock> blocks = new Stack<CTemporaryBlock>();
                                            int ID=0;
                                            blocks.Push(newBlock);
                                            for (ulong i = newBlock.Header.BlockNumber-1; i > CBlockChain.Instance.LastValidBlock.Header.BlockNumber; i--)
                                            {
                                                ID=SendRequest(new CMessage(EMessageType.Request, ERequestType.DownloadBlock, EDataType.ULong, Convert.ToString(i)));
                                                blocks.Push(new CTemporaryBlock(JsonConvert.DeserializeObject<CBlock>(ReceiveData(ID, 5000).Data),this));
                                                if (!CValidator.ValidateBlock(blocks.Peek()) && blocks.Peek().Header.BlockNumber < CBlockChain.Instance.LastValidBlock.Header.BlockNumber)
                                                {
                                                    Disconnect();
                                                    break;
                                                }
                                                if (CBlockChain.Instance.AddNewMinedBlock(blocks.Peek()))
                                                {
                                                    blocks.Pop();
                                                    for (int j = blocks.Count; j > 0; j--)
                                                        CBlockChain.Instance.AddNewMinedBlock(blocks.Pop());
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            case ERequestType.GetLastHeader:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("GetLastHeader received by " + mIp);
                                    SendRequest(new CMessage(EMessageType.Data, ERequestType.NULL, EDataType.Header,
                                        JsonConvert.SerializeObject(CBlockChain.Instance.LastValidBlock.Header), rqs.ID));
                                    break;
                                }
                            case ERequestType.ChainLength:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("ChainLength received by " + mIp);
                                    SendRequest(new CMessage(EMessageType.Data,ERequestType.NULL,EDataType.ULong,
                                        Convert.ToString(CBlockChain.Instance.LastBlock.Header.BlockNumber),rqs.ID));
                                    break;
                                }
                            case ERequestType.GetLastValid:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("GetLastValid received by " + mIp);
                                    SendRequest(new CMessage(EMessageType.Data,ERequestType.NULL,EDataType.Block,
                                        CBlockChain.Instance.LastValidBlock.Serialize(), rqs.ID));
                                    break;
                                }
                            case ERequestType.DownloadBlock:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("DownloadBlocks received by " + mIp);

                                    SendRequest(new CMessage(EMessageType.Data, ERequestType.NULL, EDataType.Block,
                                        JsonConvert.SerializeObject(CBlockChain.Instance.RetriveBlock(Convert.ToUInt64(rqs.Data),true)), rqs.ID));
                                    break;
                                }
                            case ERequestType.DownloadBlocks:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("DownloadBlocks received by " + mIp);

                                    SendRequest(new CMessage(EMessageType.Data, ERequestType.NULL, EDataType.BlockList,
                                        JsonConvert.SerializeObject(CBlockChain.Instance.RetriveBlocks(Convert.ToUInt64(rqs.Data.Split(';')[0]), Convert.ToUInt64(rqs.Data.Split(';')[1]))),rqs.ID));
                                    break;
                                }
                            case ERequestType.DownloadHeaders:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("DownloadBlocks received by " + mIp);

                                    SendRequest(new CMessage(EMessageType.Data, ERequestType.NULL, EDataType.HeaderList,
                                        JsonConvert.SerializeObject(CBlockChain.Instance.RetriveHeaders(Convert.ToUInt64(rqs.Data.Split(';')[0]), Convert.ToUInt64(rqs.Data.Split(';')[1]))), rqs.ID));
                                    break;
                                }
                            case ERequestType.GetHeader:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("GetLastHeader received by " + mIp);
                                    SendRequest(new CMessage(EMessageType.Data, ERequestType.NULL, EDataType.Header,
                                        JsonConvert.SerializeObject(CBlockChain.Instance.RetriveBlock(Convert.ToUInt64(rqs.Data)).Header), rqs.ID));
                                    break;
                                }
                            case ERequestType.NewTransaction:
                                {
                                    Transaction t=JsonConvert.DeserializeObject<Transaction>(rqs.Data);
                                    if(t.Verify())
                                    {
                                        MemPool.Instance.AddUTX(t);
                                    }
                                    break;   
                                }
                            default:
                                if (Program.DEBUG)
                                    CIO.DebugOut("Ricevuto comando sconosciuto: " + rqs.RqsType + " da " + IP);
                                break;
                        }
                    }
                }
            }
        }

        #region TypedReceive

        public CBlock ReceiveBlock(int ID, int Timeout)
        {
            return JsonConvert.DeserializeObject<CBlock>(ReceiveData(ID, Timeout).Data);
        }

        public CHeader ReceiveHeader(int ID, int Timeout)
        {
            return JsonConvert.DeserializeObject<CHeader>(ReceiveData(ID, Timeout).Data);
        }

        public ulong ReceiveULong(int ID, int Timeout)
        {
            return Convert.ToUInt64(ReceiveData(ID, Timeout).Data);
        }

        #endregion TypedReceive

        public CMessage ReceiveData(int ID, int Timeout, int checkFrequency=100)
        {
            int timeoutSlice = (Timeout / checkFrequency);
            CMessage res;
            for (int i = 0; i < checkFrequency; i++)
            {
                lock (DataQueue)
                {
                    int count = DataQueue.Count;
                    for (int j = 0; j < count; j++)
                    {
                        res = DataQueue.Dequeue();
                        if (res.ID == ID)
                            return res;
                        else if ((DateTime.Now - res.TimeOfReceipt).TotalSeconds < 300)
                            DataQueue.Enqueue(res);
                    }
                }
            Thread.Sleep(timeoutSlice);
            }  
            return null;
        }

        public int SendRequest(CMessage Msg)
        {
            //genera un nuovo id per la richiesta
            if (Msg.WillReceiveResponse)
            {
                Msg.ID = Rnd.Next();
                while (ValidID.Contains(Msg.ID))
                {
                    Msg.ID = Rnd.Next();
                }
                ValidID.Add(Msg.ID);
            }
            SendMessage(Msg);
            return Msg.ID;
        }

        public static CPeer Deserialize(string Peer)
        {
            string[] peerField = Peer.Split(',');
            return CPeer.CreatePeer(peerField[0], Convert.ToInt32(peerField[1]));
        }

        private void SendMessage(CMessage Msg)
        {
            SendString(JsonConvert.SerializeObject(Msg));
        }

        private void SendString(string Msg)
        {
            SendData(ASCIIEncoding.ASCII.GetBytes(Msg));
            if (Program.DEBUG)
                CIO.DebugOut("Sent string " + Msg + ".");
        }

        private string ReceiveString()
        {
            string msg = ASCIIEncoding.ASCII.GetString(Receive());
            if (Program.DEBUG)
                CIO.DebugOut("Received string " + msg + ".");
            return msg;
        }

        //TODO Criptare le comunicazioni
        private void SendData(byte[] Msg)
        {
            CServer.SendData(mSocket, Msg);
        }

        private byte[] Receive()
        {
            byte[] res= CServer.ReceiveData(mSocket);
            LastCommunication = DateTime.Now;
            return res;
        }
    }
}
