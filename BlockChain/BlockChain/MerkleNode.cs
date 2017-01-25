using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
namespace BlockChain
{
    //TODO
    class MerkleNode
    {
        public string Hash { get; set; }
        public MerkleNode LeftNode { get; set; }
        public MerkleNode RightNode { get; set; }

        public MerkleNode(string hash)
        {
            this.Hash = hash;
        }

        public MerkleNode(MerkleNode leftNode, MerkleNode rightNode)
        {
            this.LeftNode = leftNode;
            this.RightNode = rightNode;
            SHA256Managed sha = new SHA256Managed();
            this.Hash = Convert.ToBase64String(SHA256Managed.Create().ComputeHash(Convert.FromBase64String(leftNode.Hash + rightNode.Hash)));
        }
    }
}
