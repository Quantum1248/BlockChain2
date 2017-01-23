using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BlockChain
{
    static class SHA
    {

        static public string SHA1(string str) //inserire commenti (sarà divertente)
        {
            string binarystr = "", strlen; 
            int nzero;
            string[] dag = new string[80];
            string      h0 = "01100111010001010010001100000001",
                        h1 = "11101111110011011010101110001001",
                        h2 = "10011000101110101101110011111110",
                        h3 = "00010000001100100101010001110110",
                        h4 = "11000011110100101110000111110000",
                        A = h0,
                        B = h1,
                        C = h2,
                        D = h3,
                        E = h4;

            if(str.Length*8+1 > 512) //fixare il fatto che se il messaggio supera i 440 bit si fotte tutto
            {
                nzero = 448 - ((str.Length * 8 + 1) % 521);
                
            }
            else
            {
                nzero = 448 - (str.Length * 8 + 1);

            }

            for(int i = 0; i < str.Length;  i++)
            {
                binarystr += Convert.ToString(str[i], 2).PadLeft(8, '0');
            }

            strlen = Convert.ToString(binarystr.Length, 2).PadLeft(64, '0');
            binarystr += "1" + new String('0', nzero) + strlen;
            int k = 0;
            for (int i = 0; i < 80 ; i++)
            {
                if (i < 16)
                {
                    dag[i] = binarystr.Substring(32 * i, 32);
                }
                else
                {
                    dag[i] = LeftRotate(XOR(XOR(XOR(dag[i - 3], dag[i - 8]), dag[i - 14]), dag[i - 16]), 1);
                    
                }
            }

            string f = "", j = "", temp;
            for (int i = 0; i < 80; i++)
            {
                if (i <= 19)
                {
                    f = OR(AND(B, C), AND(NOT(B), D));
                    j = "01011010100000100111100110011001";
                }
                else if (i <= 39)
                {
                    f = XOR(XOR(B, C), D);
                    j = "01101110110110011110101110100001";
                }
                else if (i <= 59)
                {
                    f = OR(OR(AND(B, C), AND(B, D)), AND(C, D));
                    j = "10001111000110111011110011011100";
                }
                else if (i <= 79)
                {
                    f = XOR(XOR(B, C), D);
                    j = "11001010011000101100000111010110";
                }
                temp = ADD(ADD(ADD(ADD(LeftRotate(A, 5), f), E), j), dag[i].ToString());
                temp = temp.Substring(temp.Length - 32);
                E = D;
                D = C;
                C = LeftRotate(B, 30);
                B = A;
                A = temp;
            }
            h0 = ADD(h0, A);
            h0 = h0.Substring(h0.Length - 32);
            h1 = ADD(h1, B);
            h1 = h1.Substring(h1.Length - 32);
            h2 = ADD(h2, C);
            h2 = h2.Substring(h2.Length - 32);
            h3 = ADD(h3, D);
            h3 = h3.Substring(h3.Length - 32);
            h4 = ADD(h4, E);
            h4 = h4.Substring(h4.Length - 32);

            return BinaryToHex(h0 + h1 + h2 + h3 + h4);
        }

        static private string BinaryToHex(string str)
        {
            string res = "";
            int value = 0, j = 3;
            for(int i = 0; i < str.Length; i += 4)
            {
                for(int k = 0; k < 4; k++)
                {

                    value += Convert.ToInt32(str[k + i].ToString()) * (int)Math.Pow(2, j);
                    j--;
                }
                res += value.ToString("X");
                j = 3;
                value = 0;
            }
            return res;
        }
        static private string ADD(string str1, string str2)
        {
            if (str1.Length > str2.Length)
            {
                str2 = new String('0', Math.Abs(str1.Length - str2.Length)) + str2;
            }
            else
            {
                str1 = new String('0', Math.Abs(str1.Length - str2.Length)) + str1;
            }
            string res = "";
            int over = 0;
            for (int i = str1.Length - 1; i >= 0; i--)
            {
                char un = str1[i], du = str2[i];
                res = (((Convert.ToInt32(str1[i].ToString()) + Convert.ToInt32(str2[i].ToString())) + over) % 2 ).ToString() + res;
                
                if ((Convert.ToInt32(str1[i].ToString()) + Convert.ToInt32(str2[i].ToString() )+ over > 1))
                {
                    over = 1;
                }
                else
                {
                    over = 0;
                }
            }
            if (over == 1)
                res = '1' + res;
            return res;
        }

        static private string XOR(string str1, string str2)
        {

            if (str1.Length > str2.Length)
            {
                str2 = new String('0', Math.Abs(str1.Length - str2.Length)) + str2;
            }
            else
            {
                str1 = new String('0', Math.Abs(str1.Length - str2.Length)) + str1;
            }
            string res = "";
            for (int i = str1.Length - 1; i >= 0; i--)
            {
                if (str1[i] != str2[i])
                {
                    res = '1' + res;
                }
                else
                {
                    res = '0' + res;
                }
            }
            return res;
        }

        static private string AND(string str1, string str2)
        {

            if (str1.Length > str2.Length)
            {
                str2 = new String('0', Math.Abs(str1.Length - str2.Length)) + str2;
            }
            else
            {
                str1 = new String('0', Math.Abs(str1.Length - str2.Length)) + str1;
            }
            string res = "";
            for (int i = str1.Length - 1; i >= 0; i--)
            {
                char culodio = str1[i], culocane = str2[i];
                if (str1[i] == str2[i] && str1[i] != '0')
                {
                    res = '1' + res;
                }
                else
                {
                    res = '0' + res;
                }
            }
            return res;
        }

        static private string OR(string str1, string str2)
        {

            if (str1.Length > str2.Length)
            {
                str2 = new String('0', Math.Abs(str1.Length - str2.Length)) + str2;
            }
            else
            {
                str1 = new String('0', Math.Abs(str1.Length - str2.Length)) + str1;
            }
            string res = "";
            for (int i = str1.Length - 1; i >= 0; i--)
            {
                if (str1[i] == '1' || str2[i] == '1')
                {
                    res = '1' + res;
                }
                else
                {
                    res = '0' + res;
                }
            }
            return res;
        }


        static private string NOT(string str)
        {
            string res = "";
            for (int i = str.Length - 1; i >= 0; i--)
            {
                if (str[i] == '1')
                {
                    res = '0' + res;
                }
                else
                {
                    res = '1' + res;
                }
            }
            return res;
        }

        static private string LeftRotate(string str, int factor)
        {
            string substring = "";
            substring = str.Substring(0, factor);
            return str.Remove(0, factor) + substring;
        }
    }
       

}
