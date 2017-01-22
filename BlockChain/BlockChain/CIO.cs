using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain
{
    class CIO
    {
        //v 2.0 "Vgg"
        //input/output da modificare se non è più su console
        public static void WriteLine(string s)
        {
            Console.WriteLine(s);
        }

        public static void Write(string s)
        {
            Console.WriteLine(s);
        }

        public static void DebugOut(string s)
        {
            WriteLine(s);
            WriteLine("");
        }

        public static string ReadLine()
        {
            return Console.ReadLine();
        }


    }
}

