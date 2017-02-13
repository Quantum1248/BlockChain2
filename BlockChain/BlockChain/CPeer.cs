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

        private Queue<string> RequestQueue = new Queue<string>();
        private Queue<string> DataQueue = new Queue<string>();
        #region Constructors&Properties
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

        //Ritorna l'oggetto solo se i parametri sono corretti
        public static CPeer CreatePeer(string IP_Address, int Port)
        {
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

        #endregion Constructors&Properties

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
                        if (FormatIsCorrect(msg))
                        {
                            if (msg.Type == EMessageType.Request)
                                    RequestQueue.Enqueue(msg);
                            else if (msg.Type == EMessageType.Data)
                                    DataQueue.Enqueue(msg);
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

        public CBlock ReceiveBlock()
        {
            return JsonConvert.DeserializeObject<CBlock>(ReceiveString());
        }

        public void SendHeader(CHeader Header)
        {
            SendString(JsonConvert.SerializeObject(Header));
        }

        public CHeader ReceiveHeader()
        {
            return JsonConvert.DeserializeObject<CHeader>(ReceiveString());
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

        public ulong ReceiveULong()
        {
            return BitConverter.ToUInt64(ReceiveData(),0);
        }

        //TODO Criptare le comunicazioni
        public void SendData(byte[] Msg)
        {
            CServer.SendData(mSocket, Msg);//non è asincrono!!
        }

        public byte[] ReceiveData()
        {
            int c = 0;
            byte[] res = null;
            
                while (c < 10)
                {

                    if (Program.DEBUG)
                        CIO.DebugOut(Convert.ToString(c));
                    res = CServer.ReceiveData(mSocket);
                    if (Encoding.ASCII.GetString(res)[0] == '/')
                        DoCommand(ReceiveCommand(ASCIIEncoding.ASCII.GetString(res)));
                    else
                        c = 10;
                    c++;
                }
            
            return res;
        }
        #endregion NetworkCommunications


        

        public void DoCommand(ECommand cmd)
        {
            ulong index;
            switch (cmd)
            {
                case ECommand.UPDPEERS:
                    if (Program.DEBUG)
                        CIO.DebugOut("UPDPEERS received by " + mIp);
                    CPeers.Instance.DoRequest(ERequest.SendPeersList, this);
                    break;
                case ECommand.GETLASTVALID:
                    if (Program.DEBUG)
                        CIO.DebugOut("GETLASTVALID received by " + mIp);
                    SendString(CBlockChain.Instance.LastValidBlock.Serialize());
                    break;
                case ECommand.DOWNLOADBLOCK:
                    if (Program.DEBUG)
                        CIO.DebugOut("DOWNLOADBLOCK received by " + mIp);
                    index = ReceiveULong();
                    SendBlock(CBlockChain.Instance.RetriveBlock(index));
                    break;
                case ECommand.DOWNLOADBLOCKS:
                    if (Program.DEBUG)
                        CIO.DebugOut("DOWNLOADBLOCKS received by " + mIp);
                    ulong initialIndex = ReceiveULong();
                    ulong finalIndex = ReceiveULong();
                    SendBlocks(RetriveBlocks(initialIndex, finalIndex));
                    break;
                case ECommand.GETHEADER:
                    if (Program.DEBUG)
                        CIO.DebugOut("GETHEADER received by " + mIp);
                    index = ReceiveULong();
                    SendHeader(CBlockChain.Instance.RetriveBlock(index).Header);
                    break;
                case ECommand.CHAINLENGTH:
                    if (Program.DEBUG)
                        CIO.DebugOut("CHAINLENGTH received by " + mIp);
                    SendULong(CBlockChain.Instance.LastBlock.Header.BlockNumber);
                    break;
                case ECommand.RCVMINEDBLOCK:
                    if (Program.DEBUG)
                        CIO.DebugOut("RCVMINEDBLOCK received by " + mIp);
                    if (CPeers.Instance.CanReceiveBlock)
                    {
                        CTemporaryBlock newBlock = new CTemporaryBlock(ReceiveBlock(), this);
                        CBlockChain.Instance.Add(newBlock);
                    }
                    break;
                case ECommand.GETLASTHEADER:
                    SendHeader(CBlockChain.Instance.LastValidBlock.Header);
                    break;
                default:
                    if (Program.DEBUG)
                        CIO.DebugOut("Ricevuto comando sconosciuto: " + cmd + " da " + IP);
                    break;
            }
        }
        

        public void StartListening()
        {
            mThreadListener = new Thread(new ThreadStart(Listen));
            mThreadListener.Start();
        }

        private CBlock[] RetriveBlocks(ulong initialIndex,ulong finalIndex)
        {
            CBlock[] ris = new CBlock[finalIndex - initialIndex];
            int c = 0;
            while(initialIndex<finalIndex)
            {
                ris[c++] = CBlockChain.Instance.RetriveBlock(initialIndex);
                initialIndex++;
            }
            return ris;
        }

    */
    }
}
