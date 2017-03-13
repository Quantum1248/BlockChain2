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

namespace BlockChain
{
    class Program
    {
        public static bool DEBUG = true;
        public static RSACryptoServiceProvider rsaKeyPair;

        static void Main(string[] args)
        {
            rsaKeyPair = RSA.GenRSAKey();
            while (true)
            {
                //comandi per creazione o loading dei keystore cifrati con AES, chiave a 128 bit creata dall'utente e paddata se non lunga abbastanza
                Console.WriteLine("Insert 'open [name] [password]' or 'generate [name] [password]' command");
                string input = Console.ReadLine();
                string[] exInput = input.Split(' ');
                if(exInput[0] == "generate" && exInput.Length == 3)
                {
                    //ottiene il contenuto del csp, lo cifra e lo salva
                    string keystore = rsaKeyPair.ToXmlString(true);
                    keystore = AESFiles.Encrypt(keystore, exInput[2]);
                    File.WriteAllText(RSA.PATH + "\\" + exInput[1], keystore);
                }
                else if (exInput[0] == "open" && exInput.Length == 3)
                {
                    if(File.Exists(RSA.PATH + "\\" + exInput[1]))
                    {
                        //ottiene il contenuto del file cifrato e lo decifra con la chiave fornita, per poi caricarlo nel csp
                        string keystore = AESFiles.Decrypt(File.ReadAllText(RSA.PATH + "\\" + exInput[1]), exInput[2]);
                        rsaKeyPair.FromXmlString(keystore);
                    }
                }
            }
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
                    Transaction tx = new Transaction(amount, hashReceiver, rsaKeyPair);
                }
            }
        }
    }

}
