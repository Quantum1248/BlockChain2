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
    class Transaction:IEquatable<Transaction>
    {
        public string Hash;
        public List<Input> inputs;
        public Output[] outputs;
        public string Signature;
        public string PubKey;

        //Costruttore per classi ereditanti
        public Transaction()
        {

        }

        public Transaction(double amount, string hashReceiver, RSACryptoServiceProvider csp) //costruttore legittimo
        {

            this.outputs = new Output[] { new Output(amount, hashReceiver) };
            this.PubKey = RSA.ExportPubKey(csp);
            this.inputs = this.GetEnoughInputs(); //forse vanno anche controllate le firme ma non penso           
            this.Hash = Utilities.SHA2Hash(JsonConvert.SerializeObject(this)); //Calcolo l'hash di questa transazione inizializzata fino a questo punto, esso farà da txId
            RSA.HashSignTransaction(this, csp); //firmo la transazione fino a questo punto

            CPeers.Instance.DoRequest(ERequest.BroadcastNewTransaction, this);
           
        }

        public Transaction(double amount, string hashReceiver, RSACryptoServiceProvider csp, bool testing) //costruttore per testing
        {

            this.outputs = new Output[] { new Output(amount, hashReceiver) };
            this.PubKey = RSA.ExportPubKey(csp);
            this.inputs = this.GetEnoughInputs(); //forse vanno anche controllate le firme ma non penso           
            this.Hash = Utilities.SHA2Hash(JsonConvert.SerializeObject(this)); //Calcolo l'hash di questa transazione inizializzata fino a questo punto, esso farà da txId
            RSA.HashSignTransaction(this, csp); //firmo la transazione fino a questo punto
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
            //CPeers.Instance.DoRequest(ERequest.SendTransaction, this); TODO : implementa richiesta di invio transazione
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

        public void Broadcast()
        {
            if (this.Verify())
            {
                //TODO: sostituire true con funzione per controllare se il client sta minando
                if (true)
                {
                    MemPool.Instance.AddUTX(this);
                }
                //TODO: implementare funzione per inoltrare transazione, o creare una funzione nella classe CPeer che in base al valore ritornato da Verify() inoltri o meno
            }
        }
        //verifica della transazione
        public bool Verify()
        {
            //si crea una transazione per generare l'hash da confrontare alla firma
            Transaction signedTx = new Transaction(this.inputs, this.outputs, this.Hash, this.PubKey);
            //si verifica la firma digitale
            if(RSA.VerifySignedTransaction(this, Utilities.SHA2HashBytes(signedTx.Serialize()), this.PubKey))
            {
                double outputRequested = 0;
                double inputRequested = 0;
                //si controlla che l'output riferito da ogni input sia tra gli output non spesi e si confrontano gli hash
                if (!this.CheckUTXO()) { return false; }
                //si verifica che gli output non spendano più di quanto referenziato dagli input
                if (!(this.GetInputsAmount() >= this.GetOutputsAmount())) { return false; }
                return true;
            }
            return false;
        }

        private bool CheckUTXO()
        {
            Output tmp;
            string pubKeyHash = Utilities.Base64SHA2Hash(this.PubKey);
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
                output = UTXOManager.Instance.GetUTXO(Utilities.Base64SHA2Hash(this.PubKey), input.TxHash, input.OutputIndex);
                inputRequested += output.Amount;
            }
            return inputRequested;
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
            string pubKeyHash = Utilities.Base64SHA2Hash(this.PubKey);
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
                        UTXOManager.Instance.RemoveUTXO(pubKeyHash, utxo.TxHash, outputIndex);
                        //se l'amount richiesto scende sotto lo 0, si aggiunge una nuova transazione per rispedire a noi stessi il resto
                        if (outputRequested <= 0)
                        {
                            Array.Resize(ref this.outputs, this.outputs.Length + 1);
                            this.outputs[this.outputs.Length - 1] = new Output(Math.Abs(outputRequested), pubKeyHash);
                            outputRequested += this.outputs[this.outputs.Length - 1].Amount;
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

        
        public string Serialize()//TODO: tutte le operazioni di serializzazione e deserializzazione andrebbero spostate in utilities per un codice meno ridondante e più pulito
        {
            return JsonConvert.SerializeObject(this);
        }

        private Transaction Deserialize(string jsonString)
        {
            return JsonConvert.DeserializeObject<Transaction>(jsonString);
        }

        public bool Equals(Transaction other)
        {
            return this.Hash == other.Hash;
        }
    }
}
