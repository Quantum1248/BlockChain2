using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
namespace BlockChain
{
    [ServiceContract]
    interface IWCF
    {
        [OperationContract]
        void LoadKeyStore(string name, string password);

        [OperationContract]
        void GenerateKeyStore(string name, string password);
    }
}
