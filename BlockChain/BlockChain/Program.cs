using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.IO;
using System.Configuration;
using System.ServiceModel;

namespace BlockChain
{
    class Program
    {
        public static bool DEBUG = true;
        public static RSACryptoServiceProvider rsaKeyPair;
        public static ServiceHost serviceHost;
        public static NetNamedPipeBinding binding;
        static void Main(string[] args)
        {
            //Apre il canale di comunicazione per GUI https://github.com/Kojee/BlockChainGUI
            OpenWCFServices();
            
            //List<CPeer> lp = GenPeersList();
            List<CPeer> lp = new List<CPeer>();

            lp.Add(CPeer.CreatePeer("172.18.2.20", 4000));


            CServer.Instance.InitializePeersList(lp);

            bool b = true;
            while (b)
            {
                string command = Console.ReadLine();
                switch (command)
                {
                    case "transaction":
                        {
                            string hashReceiver = Console.ReadLine();
                            double amount = Convert.ToDouble(Console.ReadLine());
                            Transaction tx = new Transaction(amount, hashReceiver, rsaKeyPair);
                            break;
                        }
                    case "miner":
                        {
                            CServer.Instance.StartMining();
                            break;
                        }
                    case "sync":
                        {
                            CServer.Instance.SyncBlockchain();
                            break;
                        }
                    default:
                        b = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Apre il canale di comunicazione WCF per la GUI (https://github.com/Kojee/BlockChainGUI), indicando l'interfaccia e l'implementazione da esporre
        /// </summary>
        public static void OpenWCFServices()
        {
            //Specifica l'indirizzo in cui sono hostati i servizi esposti
            string address = "net.pipe://localhost/WCFServices";

            //Indica l'implementazione dell'interfaccia
            serviceHost = new ServiceHost(typeof(Services));
            binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            //Indica l'interfaccia
            serviceHost.AddServiceEndpoint(typeof(IWCF), binding, address);
            serviceHost.Open();

            Console.WriteLine("ServiceHost running. Press Return to Exit");
        }
    }

}
