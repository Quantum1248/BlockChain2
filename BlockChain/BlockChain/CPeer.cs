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

        public bool Connect()
        {
            if (Program.DEBUG)
                CIO.DebugOut("Connecting to " + mIp + ":" + mPort);
            SocketAsyncEventArgs asyncConnection = new SocketAsyncEventArgs();
            bool SuccessfulConnected = false;
            asyncConnection.Completed += (object sender, SocketAsyncEventArgs e) => { SuccessfulConnected = true; };
            asyncConnection.RemoteEndPoint = new IPEndPoint(mIp, mPort);
            mSocket.ConnectAsync(asyncConnection);
            Thread.Sleep(3000);
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
                asyncConnection.Dispose();
                return false;
            }
        }

        public void Disconnect()
        {
            mSocket.Close();
            mSocket.Dispose();//(!) in teoria è inutile perchè fa già tutto Close()
            CPeers.Instance.InvalidPeers(new CPeer[] { this });
            mIsConnected = false;
        }

        public void StartListening()
        {
            mThreadListener = new Thread(new ThreadStart(Listen));
            mThreadListener.Start();
        }

        #endregion Constructors&Properties&Inizialization

        /// <summary>
        /// Rimane in attesa di messaggi dal peer a cui è collegato il socket mSocket.
        /// </summary>
        private void Listen()
        {
            CMessage msg;
            while (mIsConnected)    //bisogna bloccarlo in qualche modo all'uscita del programma credo
            {
                lock (mSocket)
                {
                    //il timer viene settato cosicchè in caso non si ricevino comunicazioni venga ritornata un'eccezione, in modo che il programma vada avanti e tolga il lock al socket.
                    mSocket.ReceiveTimeout = 1000;
                    try
                    {
                        msg =JsonConvert.DeserializeObject<CMessage>(ReceiveString());
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
                    catch(JsonSerializationException)
                    { Disconnect(); }
                    //il timer viene reinpostato a defoult per non causare problemi con altre comunicazioni che potrebbero avvenire in altre parti del codice.
                    mSocket.ReceiveTimeout = 0;
                }
                Thread.Sleep(1000);
            }
        }

        private void ExecuteRequest()
        {
            CMessage rqs;
            while (mIsConnected)
            {
                Thread.Sleep(500);
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
                                    CPeers.Instance.DoRequest(ERequest.SendPeersList, this);
                                    break;
                                }
                            case ERequestType.NewBlockMined:
                                {
                                    if (Program.DEBUG)
                                        CIO.DebugOut("NewBlockMined received by " + mIp);
                                    if (CPeers.Instance.CanReceiveBlock)
                                    {
                                        CTemporaryBlock newBlock = new CTemporaryBlock(CBlock.Deserialize(rqs.Data), this);
                                        if (!CValidator.ValidateBlock(newBlock))
                                        {
                                            Disconnect();
                                            break;
                                        }
                                        //TODO scaricare i blocchi mancanti se ne mancano(sono al blocco 10 e mi arriva il blocco 50)
                                        CBlockChain.Instance.Add(newBlock);
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
                            /*
                                case ECommand.GETLASTVALID:
                                
                                case ECommand.DOWNLOADBLOCK:
                                if (Program.DEBUG)
                                CIO.DebugOut("DOWNLOADBLOCK received by " + mIp);
                                index = ReceiveULong();
                                SendBlock(CBlockChain.Instance.RetriveBlock(index));
                                break;
                                case ECommand.DOWNLOADBLOCKS:

                                case ECommand.GETHEADER:
                                if (Program.DEBUG)
                                CIO.DebugOut("GETHEADER received by " + mIp);
                                index = ReceiveULong();
                                SendHeader(CBlockChain.Instance.RetriveBlock(index).Header);
                                break;
                                case ECommand.CHAINLENGTH:
                                
                                */
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











/*

        #region NetworkCommunications
        public void SendCommand(ECommand Cmd)
        {
            if (Program.DEBUG)
                CIO.DebugOut("Send " + Cmd + ".");
            switch (Cmd)
            {
                case ECommand.UPDPEERS:
                    SendString("/UPDPEERS");
                    break;
                case ECommand.GETLASTVALID:
                    SendString("/GETLASTVALID");
                    break;
                case ECommand.DOWNLOADBLOCK:
                    SendString("/DOWNLOADBLOCK");
                    break;
                case ECommand.DOWNLOADBLOCKS:
                    SendString("/DOWNLOADBLOCKS");
                    break;
                case ECommand.RCVMINEDBLOCK:
                    SendString("/RCVMINEDBLOCK");
                    break;
                case ECommand.DISCONNETC:
                    SendString("/DISCONNETC");
                    break;
                case ECommand.DOWNLOADHEADERS:
                    SendString("/DOWNLOADHEADERS");
                    break;
                case ECommand.GETHEADER:
                    SendString("/GETHEADER");
                    break;
                case ECommand.CHAINLENGTH:
                    SendString("/CHAINLENGTH");
                    break;
                case ECommand.GETLASTHEADER:
                    SendString("/GETLASTHEADER");
                    break;
                default:
                    throw new ArgumentException("Comando al peer non supportato.");
            }
        }

        public ECommand ReceiveCommand(string msg=null)
        {
            if(msg==null)
                msg = ReceiveString();
            else
            {
                switch (msg)
                {
                    case "/UPDPEERS":
                        return ECommand.UPDPEERS;
                    case "/GETLASTVALID":
                        return ECommand.GETLASTVALID;
                    case "/DOWNLOADBLOCK":
                        return ECommand.DOWNLOADBLOCK;
                    case "/DOWNLOADBLOCKS":
                        return ECommand.DOWNLOADBLOCKS;
                    case "/RCVMINEDBLOCK":
                        return ECommand.RCVMINEDBLOCK;
                    case "/DISCONNETC":
                        return ECommand.DISCONNETC;
                    case "/DOWNLOADHEADERS":
                        return ECommand.DOWNLOADHEADERS;
                    case "/GETHEADER":
                        return ECommand.GETHEADER;
                    case "/CHAINLENGTH":
                        return ECommand.CHAINLENGTH;
                    case "/GETLASTHEADER":
                        return ECommand.GETLASTHEADER;
                    default:
                        throw new ArgumentException("Ricevuta stringa di comando non supportata.");
                }
            }
            if (Program.DEBUG)
                CIO.DebugOut("Received " + msg + ".");
            return default(ECommand);
        }

        public void SendBlocks(CBlock[] Blocks)
        {
            string msg="";
            foreach(CBlock b in Blocks)
                if(b!=null)
                    msg += JsonConvert.SerializeObject(b) + "/";
                else
                    msg += "NULL/";
            msg = msg.TrimEnd('/'); 
            SendString(msg);
        }

        public void SendBlock(CBlock b)
        {
            SendString(JsonConvert.SerializeObject(b));
        }



        public void SendHeader(CHeader Header)
        {
            SendString(JsonConvert.SerializeObject(Header));
        }



        public void SendString(string Msg)
        {
            if (Program.DEBUG)
                CIO.DebugOut("Send string " + Msg + ".");
            SendData(ASCIIEncoding.ASCII.GetBytes(Msg));
        }

        public string ReceiveString()
        {
            string msg= ASCIIEncoding.ASCII.GetString(ReceiveData());
            if (Program.DEBUG)
                CIO.DebugOut("Received string " + msg + ".");
            return msg;
        }

        public void SendULong(ulong Nmb)
        {
            SendData(BitConverter.GetBytes(Nmb));
        }

        #endregion NetworkCommunications


        

        
        



        

    */
