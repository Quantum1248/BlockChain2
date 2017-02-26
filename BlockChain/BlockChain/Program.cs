using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlockChain
{
    class Program
    {
        public static bool DEBUG = true;
        
        static void Main(string[] args)
        {

            //List<CPeer> lp = GenPeersList();
            List<CPeer> lp = new List<CPeer>();

            lp.Add(CPeer.CreatePeer("192.168.1.103",2000));

            CServer s = CServer.StartNewServer(lp);

            while (true)
            {
                string command = Console.ReadLine();
                if(command == "transaction")
                {
                    string hashReceiver = Console.ReadLine();
                    double amount = Convert.ToDouble(Console.ReadLine());
                    Transaction tx = new Transaction(amount, hashReceiver, CServer.rsaKeyPair);
                }
            }
        }
    }

}
