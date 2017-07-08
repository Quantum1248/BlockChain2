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
        public static ServiceHost serviceHost;
        public static NetNamedPipeBinding binding;
        static void Main(string[] args)
        {
            //Apre il canale di comunicazione per GUI https://github.com/Kojee/BlockChainGUI
            OpenWCFServices();
            
            //List<CPeer> lp = GenPeersList();
            List<CPeer> lp = new List<CPeer>();
            CIO.WriteLine("Enter the peer address:");
            string firstPeerIP = Console.ReadLine();
            try
            {
                if (!IPAddress.TryParse(firstPeerIP, out var IP))
                    firstPeerIP = "192.168.1.1";
            }
            catch
            {
                firstPeerIP = "192.168.1.1";
            }
            lp.Add(CPeer.CreatePeer(firstPeerIP, 4000));


            CServer.Instance.InitializePeersList(lp);
            Services cmd = new Services();
            bool b = true;
            while (b)
            {
                string command = Console.ReadLine().ToLower();
                switch (command)
                {
                    case "transaction":
                        {
                            CIO.WriteLine("Enter destination address:");
                            string hashReceiver = Console.ReadLine();
                            CIO.WriteLine("Enter the amout of coins to send:");
                            double amount = Convert.ToDouble(Console.ReadLine());
                            Transaction tx = new Transaction(amount, hashReceiver, CServer.rsaKeyPair);
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
                    case "address":
                        {
                            CIO.WriteLine(cmd.GetKeystore());
                            break;
                        }
                    case "balance":
                        {
                            CIO.WriteLine(cmd.GetBalance().ToString());
                            break;
                        }
                    case "stop":
                        {
                            b = false;
                            Environment.Exit(1);
                            break;
                        }
                    default:
                        {
                            CIO.WriteLine("Invalid command.");
                            break;
                        }
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
