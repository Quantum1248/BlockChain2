using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
namespace BlockChain
{
    /// <summary>
    /// Interfaccia per esporre i metodi utili alla GUI https://github.com/Kojee/BlockChainGUI
    /// </summary>
    [ServiceContract]
    interface IWCF
    {
        /// <summary>
        /// Carica in memoria i parametri RSA del dato keystore
        /// </summary>
        /// <param name="name">Il nome del keystore</param>
        /// <param name="password">La password per sbloccarlo</param>
        [OperationContract]
        void LoadKeyStore(string name, string password);

        /// <summary>
        /// Crea un nuovo keystore con il nome e la password specificata
        /// </summary>
        /// <param name="name">Il nome del keystore</param>
        /// <param name="password">La password per sbloccarlo</param>
        [OperationContract]
        void GenerateKeyStore(string name, string password);
    }
}
