using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptSharp.Utility;
namespace BlockChain
{
    class Miner
    {
        public static void Scrypt(CBlock block) //TODO: implementare evento per l'uscita in caso sia stato trovato un blocco parallelo. Implementare multithreading
        {

            string toHash;
            string hash;
            bool found = false;
            int higher = 0, current = 0;

            while (!found)
            {
                toHash = block.prevBlockHash + block.Nonce + block.Timestamp + block.MerkleRoot; //si concatenano vari parametri del blocco TODO: usare i parmetri giusti, quelli usati qua sono solo per dimostrazione e placeholder
                hash = Utilities.ByteArrayToString(SCrypt.ComputeDerivedKey(Encoding.ASCII.GetBytes(toHash), Encoding.ASCII.GetBytes(toHash), 1024, 1, 1, 1, 32)); //calcola l'hash secondo il template di scrypt usato da litecoin
                for (int i = 0; i <= block.Difficulty; i++)
                {
                    if (i == block.Difficulty) //se il numero di zeri davanti la stringa è pari alla difficoltà del blocco, viene settato l'hash e si esce
                    {
                        block.Hash = hash;
                        //CPeers.Instance.DoRequest(ERequest.SendNewBlock, block); TODO : implementa richiesta di invio blocco
                        return;
                    }
                    if (!(hash[i] == '0'))
                    {
                        current = 0;
                        break;
                    }

                    current++;
                    if (higher < current)
                    {
                        higher = current;
                    }

                }

                block.Nonce++; //incremento della nonce per cambiare hash
            }

        }

        public static bool Verify(CBlock block)
        {
            string toHash = block.prevBlockHash + block.Nonce + block.Timestamp + block.MerkleRoot;
            if (block.Hash == Utilities.ByteArrayToString(SCrypt.ComputeDerivedKey(Encoding.ASCII.GetBytes(toHash), Encoding.ASCII.GetBytes(toHash), 1024, 1, 1, 1, 32)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
