using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetConnector
{
    public class NetCoderException:Exception
    {
        public override string Message { get; }
        public NetCoderException(string s) {
            Message = s;
        }
    }
}
