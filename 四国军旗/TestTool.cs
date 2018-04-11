using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTool
{
    public static class ExchangeToString
    {
        
        //可视化二维数组
        public static string Array<T>(T[,] array,Func<T,string> VisibleString)
        {
            string str = string.Empty;
            for (int i = 0; i < array.GetLength(1); i++)
            {
                for (int j = 0; j < array.GetLength(0); j++)
                {
                    str += VisibleString(array[i,j]);
                }
                str += '\n';
            }
            return str;
        }
    }
}
