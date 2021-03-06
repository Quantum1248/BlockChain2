﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class Input:IEquatable<Input>
    {
        public string TxHash;
        public int OutputIndex;
        public Input()
        {

        }
        public Input(string hash, int index)
        {
            this.TxHash = hash;
            this.OutputIndex = index;
        }

        public bool Equals(Input other)
        {
            return (this.TxHash == other.TxHash && this.OutputIndex == other.OutputIndex);
        }
    }
}
