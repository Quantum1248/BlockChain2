using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
namespace BlockChain
{
    class Transaction
    {
        private string mPubKeyFrom, mPubKeyTo;
        private double mAmount;
        private string mSignature;
        //TODO: aggiungere riferimento a output precedente per verificare transazione
        public string PubKeyFrom
        {
            get
            {
                return mPubKeyFrom;
            }
            set
            {
                mPubKeyFrom = value;
            }
        }

        public string PubKeyTo
        {
            get
            {
                return mPubKeyTo;
            }
            set
            {
                mPubKeyTo = value;
            }
        }

        public double Amount
        {
            get
            {
                return mAmount;
            }
            set
            {
                mAmount = value;
            }
        }

        public string Signature
        {
            get
            {
                return mSignature;
            }
            set
            {
                mSignature = value;
            }
        }

        public Transaction(RSACryptoServiceProvider csp, string pubKeyTo, double amount)
        {
            this.PubKeyFrom = RSA.ExportPubKey(csp);
            this.PubKeyTo = pubKeyTo;
            this.Amount = amount;
            RSA.HashSignTransaction(this, csp);
        }

        public static string Serialize(Transaction tx)
        {
            return tx.PubKeyFrom + ";" + tx.PubKeyTo + ";" + tx.Amount + ";";
        }

        public static string SerializeVerifiable(Transaction tx)
        {
            //TODO:Implementare metodo per ritornare una stringa da una transazione firmata, ricostruibile e verificabile dopo l'invio tramite socket 
            return "";
        }
    }
}
