using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    //Wrapper usato per ritornare valori dalla funzione CPeers.DoRequest()
    class ArgumentWrapper<T>
    {
        public T Value;

        public ArgumentWrapper()
        { }

        public ArgumentWrapper(T Value)
        {
            this.Value = Value;
        }
    }
}
