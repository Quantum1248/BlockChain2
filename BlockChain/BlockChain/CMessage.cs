using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CMessage
    {
        public EMessageType Type;
        public string Data;
        public int ID;

        public CMessage()
        {
            Type = default(EMessageType);
            Data = "";
            ID = 0;
        }

        public CMessage(EMessageType Type, string Data)
        {
            this.Type = Type;
            this.Data = Data;
        }
    }
}
