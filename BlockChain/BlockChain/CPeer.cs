using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;

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
        private bool mConnect;
        private CPeer()
        {
        }

        private CPeer(IPAddress IP_Address, int Port)
        {
            mIp = IP_Address;
            mPort = Port;
            //IPEndPoint peerEndPoint = new IPEndPoint(IP_Address,Port);    Serve per fare il connect
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mConnect = false;
        }

        private CPeer(IPAddress IP_Address, int Port, Socket Sck)
        {
            mIp = IP_Address;
            mPort = Port;
            mSocket = Sck;
            mConnect = true;
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
                    CIO.DebugOut("Connection with " + mIp + ":" + mPort+" enstablished!");
                mConnect = true;
                mThreadListener = new Thread(new ThreadStart(Listen));
                mThreadListener.Start();
                return true;
            }
            else
            {
                if (Program.DEBUG)
                    CIO.DebugOut("Connection with " + mIp + ":" + mPort+" failed!");
                asyncConnection.Dispose();
                return false;
            }
        }

        public void SendCommand(ECommand Cmd)
        {
            switch (Cmd)
            {
                default:
                    throw new ArgumentException("Comando al peer non supportato.");
            }
        }

        public ECommand ReceiveCommand()
        {
            string msg = ReceiveString();
            switch(msg)
            {
                default:
                    throw new ArgumentException("Ricevuta stringa di comando non supportata.");
            }
        }

        public void SendString(string Msg)
        {
            SendData(ASCIIEncoding.ASCII.GetBytes(Msg));//non è asincrono!!
        }

        public string ReceiveString()
        {
            return ASCIIEncoding.ASCII.GetString(ReceiveData());

        }

        //TODO Criptare le comunicazioni
        public void SendData(byte[] Msg)
        {
            CServer.SendData(mSocket, Msg);//non è asincrono!!
        }

        public byte[] ReceiveData()
        {
            return CServer.ReceiveData(mSocket);
        }



        /// <summary>
        /// Rimane in attesa di messaggi dal peer a cui è collegato il socket mSocket.
        /// </summary>
        public void Listen()
        {
            ECommand cmd;
            while (true)    //bisogna bloccarlo in qualche modo all'uscita del programma credo
            {
                lock (mSocket)
                {
                    //il timer viene settato cosicchè in caso non si ricevino comunicazioni venga ritornata un'eccezione, in modo che il programma vada avanti e tolga il lock al socket.
                    mSocket.ReceiveTimeout = 1000;  
                    try
                    {
                        cmd = ReceiveCommand();
                        if (cmd== ECommand.LOOK)  //(!)non serve a niente?
                        {
                            if (Program.DEBUG)
                                Console.WriteLine("LOCK received by" + mIp);
                            SendCommand(ECommand.OK);
                            cmd = ReceiveCommand();
                            switch (cmd)
                            {
                                case ECommand.UPDPEERS:
                                    CPeers.Instance.DoRequest(ERequest.SendPeersList,this);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        Thread.Sleep(1000); //(!)forse è meglio attendere fuori dal lock. E forse non serve comunque perchè il thread si ferma già quando è in attesa di connessioni.
                    }
                    //il timer viene reinpostato a defoult per non causare problemi con altre comunicazioni che potrebbero avvenire in altre parti del codice.
                    mSocket.ReceiveTimeout = 0;
                }
            }
        }
        
    }
}