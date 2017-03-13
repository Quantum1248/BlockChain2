using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BlockChain
{
    // NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di interfaccia "IServiceTestNetwork" nel codice e nel file di configurazione contemporaneamente.
    [ServiceContract]
    public interface IServiceTestNetwork
    {
        [OperationContract]
        void DoWork();
    }
}
