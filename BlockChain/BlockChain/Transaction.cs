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
        public Output[] outputs;
        public string Signature;
        public string PubKey;
        
        public Transaction(Output[] outputs, string PubKey, RSACryptoServiceProvider csp) //costruttore per testing
        {
            
            this.outputs = outputs;
            this.inputs = this.GetEnoughInputs(); //forse vanno anche controllate le firme ma non penso           
            this.PubKey = PubKey;
            this.Hash = Utilities.ByteArrayToString(SHA256Managed.Create().ComputeHash(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(this)))); //Calcolo l'hash di questa transazione inizializzata fino a questo punto, esso farà da txId
            this.Signature = RSA.Sign(Encoding.ASCII.GetBytes(this.Serialize()), csp.ExportParameters(true), false); //firmo la transazione fino a questo punto

            //salvo la transazione sul disco
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(appDataFolder, "Blockchain\\UTXODB");
            UTXO utxo = new UTXO(this.Hash, this.outputs);
            if (Directory.Exists(specificFolder))
            {
                
                File.WriteAllText(specificFolder + "\\" + this.Hash + ".json", utxo.Serialize()); 
            }
            else
            {
                Directory.CreateDirectory(specificFolder);
                File.WriteAllText(specificFolder + "\\" + this.Hash + ".json", utxo.Serialize());
            }
        }
        
        public Transaction(List<Input> inputs, Output[] outputs, string Hash, string PubKey) //costruttore per generare l'hash da confrontare poi alla firma
        {
            this.inputs = inputs;
            this.outputs = outputs;
            this.Hash = Hash;
            this.PubKey = PubKey;
        }

        [JsonConstructor]
        public Transaction(List<Input> inputs, Output[] outputs, string Hash, string PubKey, string Signature)//costruttore per deserializzare le stringhe json prese dai file
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
            //si verifica la firma digitale
            if(RSA.VerifySignedTransaction(this, SHA256Managed.Create().ComputeHash(Encoding.ASCII.GetBytes(signedTx.Serialize())), this.PubKey))
            {
                double outputRequested = 0;
                double inputRequested = 0;
                //si controlla che l'output riferito da ogni input sia tra gli output non spesi e si confrontano gli hash
                if (!this.CheckUTXO()) { return false; }
                //si verifica che gli output non spendano più di quanto referenziato dagli input
                if (!(this.GetInputsAmount() >= this.GetOutputsAmount())) { return false; }
            }
            return false;            
        }

        private bool CheckUTXO()
        {
            Output tmp;
            string pubKeyHash = Utilities.ByteArrayToString(SHA256Managed.Create().ComputeHash(Encoding.ASCII.GetBytes(this.PubKey)));
            //si controlla che esistano output non spesi corrispondenti a quelli referenziati dagli input
            foreach(Input input in this.inputs) 
            {
                tmp = UTXOManager.Instance.GetUTXO(pubKeyHash, input.TxHash, input.OutputIndex); 
                //se non viene ritornato nulla, gli output sono, probabilmente, stati già spesi o non ci sono mai stati 
                if(tmp == null)
                {
                    return false;
                }
            }
            return true;
        }

        //calcola l'amount referenziato dagli input
        private double GetInputsAmount()
        {
            double inputRequested = 0;
            Output output;
            foreach (Input input in this.inputs)
            {
                output = UTXOManager.Instance.GetUTXO(Utilities.ByteArrayToString(SHA256Managed.Create().ComputeHash(Encoding.ASCII.GetBytes(this.PubKey))), input.TxHash, input.OutputIndex);
                inputRequested += output.Amount;
            }
            return 1;
        }

        //calcola l'output richiesto nella transazione
        private double GetOutputsAmount()
        {
            double outputRequested = 0;
            foreach (Output output in this.outputs)
            {
                outputRequested += output.Amount;
            }
            return outputRequested;
        }

        //ritorna gli input necessari a soddisfare le richieste degli output. Se si avanza qualcosa dagli input, 
        //esso viene rispedito al mittente (colui che crea la transazione) tramite un nuovo output
        private List<Input> GetEnoughInputs()
        {
            List<Input> inputs = new List<Input>();
            double outputRequested = this.GetOutputsAmount();
            string pubKeyHash = Utilities.ByteArrayToString(SHA256Managed.Create().ComputeHash(Encoding.ASCII.GetBytes(this.PubKey)));
            List<UTXO> utxos = UTXOManager.Instance.GetUTXObyHash(pubKeyHash);
            foreach(UTXO utxo in utxos)
            {
                for(int outputIndex = 0; outputIndex < utxo.Output.Length; outputIndex++)
                {
                    //si effettua il confronto degli hash
                    if(utxo.Output[outputIndex].PubKeyHash == pubKeyHash)
                    {
                        //si scala l'amount richiesto
                        outputRequested -= utxo.Output[outputIndex].Amount;
                        inputs.Add(new Input(utxo.TxHash, outputIndex));
                        //L'UTXO viene speso e quindi rimosso dal database, mentre un nuovo input viene aggiunto alla lista
                        UTXOManager.Instance.SpendUTXO(pubKeyHash, utxo.TxHash, outputIndex);
                        //se l'amount richiesto scende sotto lo 0, si aggiunge una nuova transazione per rispedire a noi stessi il resto
                        if (outputRequested <= 0)
                        {
                            Array.Resize(ref this.outputs, this.outputs.Length + 1);
                            this.outputs[this.outputs.Length - 1] = new Output(Math.Abs(outputRequested), pubKeyHash);
                        }
                    }
                    if(outputRequested <= 0)
                    {
                        return inputs;
                    }
                }
            }
            return null;
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        private Transaction Deserialize(string jsonString)
        {
            return JsonConvert.DeserializeObject<Transaction>(jsonString);
        }
    }
}
