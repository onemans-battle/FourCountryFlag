using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class GameServerException:Exception
    {

    }
    public class RoomException: Exception
    {

        public override string Message { get; }
        public RoomException(string message)
        {
            Message = message;
        }
    }
}
