using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.IO;

namespace BlockChain
{
    class Transaction
    {
        public string Hash;
        public List<Input> inputs;
        public List<Output> outputs;
        public string Signature;
        public string PubKey;
        
        public Transaction(double Amount, string PubKey, RSACryptoServiceProvider csp) //costruttore per testing
        {
            this.inputs = new List<Input>();
            this.outputs = new List<Output>();
            this.inputs.Add(new Input("123", 0));
            this.outputs.Add(new Output(1, "placeholder"));
            this.PubKey = PubKey;
            this.Hash = Convert.ToBase64String(SHA256Managed.Create().ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this)))); //Calcolo l'hash di questa transazione inizializzata fino a questo punto, esso farà da txId
            this.Signature = RSA.Sign(Encoding.UTF8.GetBytes(this.Serialize()), csp.ExportParameters(true), false); //firmo la transazione fino a questo punto

            //salvo la transazione sul disco
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(appDataFolder, "Blockchain\\UTXODB");
            if (Directory.Exists(specificFolder))
            {
                File.WriteAllText(specificFolder + "\\utxo0", this.Serialize()); 
            }
            else
            {
                Directory.CreateDirectory(specificFolder);
                File.WriteAllText(specificFolder + "\\utxo0", this.Serialize());
            }
        }
        
        public Transaction(List<Input> inputs, List<Output> outputs, string Hash, string PubKey) //costruttore per generare l'hash da confrontare poi alla firma
        {
            this.inputs = inputs;
            this.outputs = outputs;
            this.Hash = Hash;
            this.PubKey = PubKey;
        }

        [JsonConstructor]
        public Transaction(List<Input> inputs, List<Output> outputs, string Hash, string PubKey, string Signature)//costruttore per deserializzare le stringhe json prese dai file
        {
            this.inputs = inputs;
            this.outputs = outputs;
            this.Hash = Hash;
            this.PubKey = PubKey;
            this.Signature = Signature;
        }

        //verifica della transazione
        public bool Verify()
        {
            //si crea una transazione per generare l'hash da confrontare alla firma
            Transaction signedTx = new Transaction(this.inputs, this.outputs, this.Hash, this.PubKey);
            //si verifica la firma con la pubKey allegata alla transazione e confrontando il risultato con l'hash sopra calcolato
            this.VerifyPubKeyHash();
            if(RSA.VerifySignedTransaction(this, SHA256Managed.Create().ComputeHash(Encoding.UTF8.GetBytes(signedTx.Serialize())), this.PubKey))
            {
                double outputRequested = 0;
                double inputRequested = 0;
                //si controlla che l'output riferito da ogni input sia tra gli output non spesi
                if (!CheckUTXO(this.inputs)) { return false; }
                //si verifica che gli output non spendano più di quanto referenziato dagli input
                if (!(GetInputsAmount(this.inputs) >= GetOutputsAmount(this.outputs))) { return false; }
            }
            return false;            
        }

        private bool CheckUTXO(List<Input> inputs)
        {
            Transaction tmp;
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(appDataFolder, "Blockchain\\UTXODB");

            if (Directory.Exists(specificFolder))
            {
                DirectoryInfo d = new DirectoryInfo(specificFolder);
                foreach(Input input in inputs)
                {
                    foreach (var file in d.GetFiles("*.json"))
                    {
                        tmp = Deserialize(File.ReadAllText(specificFolder + "\\" + file.FullName));
                        if (input.TxHash == tmp.Hash)
                        {
                            foreach(Output output in tmp.outputs)
                            {
                                //TODO confrontare index dell'output
                            }
                        }
                    }
                }
                
            }
            return true;
        }

        //calcola l'amount referenziato dagli input
        private double GetInputsAmount(List<Input> inputs)
        {
            double inputRequested = 0;
            Output output;
            foreach (Input input in inputs)
            {
                output = GetOutputFromUTXODB(input);
                inputRequested += output.Amount;
            }
            return 1;
        }

        //calcola l'output richiesto nella transazione
        private double GetOutputsAmount(List<Output> outputs)
        {
            double outputRequested = 0;
            foreach (Output output in outputs)
            {
                outputRequested += output.Amount;
            }
            return outputRequested;
        }

        //confronta l'hash negli output referenziati da ogni input con l'hash della pubkey nella transazione
        private bool VerifyPubKeyHash()
        {
            Output output;
            foreach (Input input in this.inputs)
            {
                output = GetOutputFromUTXODB(input);
                if (output.PubKeyHash != Convert.ToBase64String(SHA256Managed.Create().ComputeHash(Encoding.UTF8.GetBytes(this.PubKey)))) { return false; }
            }
            return true;
        }


        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        //ritorna l'output referenziato dall'input passato come parametro
        private Output GetOutputFromUTXODB(Input input)
        {
            Transaction tmp;
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(appDataFolder, "Blockchain\\UTXODB");
            if (Directory.Exists(specificFolder))
            {
                DirectoryInfo d = new DirectoryInfo(specificFolder);
                foreach(var file in d.GetFiles("*.json"))
                {

                    tmp = Deserialize(File.ReadAllText(specificFolder + "\\" + file.FullName));
                    if(input.TxHash == tmp.Hash)
                    {
                        return tmp.outputs.ElementAt<Output>(input.OutputIndex);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(specificFolder);
            }
            return null;
        }

        private Transaction Deserialize(string jsonString)
        {
            return JsonConvert.DeserializeObject<Transaction>(jsonString);
        }
    }
}
