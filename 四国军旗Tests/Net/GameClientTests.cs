using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetConnector;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetConnector.MsgType;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using NetConnector.MsgType;
namespace NetConnector.Tests
{
    [TestClass()]
    public class GameClientTests
    {
        [TestMethod()]
        public void GameClientTest()
        {
            GameLogic.ChessInfo[] c = GameLogic.CheckBroad.ConvertFromLayoutToCheInfo(GameLogic.CheckBroad.GetDefaultLayout(), GameLogic.OfSide.First);
            RGInfo gi = new RGInfo() { CChessesInfo = c, RoomID = 1111111 };
            
            GameTCPClient[] gcList = new GameTCPClient[100];
            string s;
            for (int i = 0; i < 100; i++)//模拟100个客户端连接
            {
               
                gcList[i] = new GameTCPClient();
                gcList[i].NetMsgRev += (sender,netmsg) =>{
                    s = ((GameTCPClient)sender).TcpClient.Client.RemoteEndPoint.ToString() +
                    "回应的数据是:" + netmsg.Data.ToString();
                };
                gcList[i].TcpClient.ConnectAsync(IPAddress.Loopback, 8080).Wait();
                gcList[i].SendAsync(gi).Wait();
                gcList[i].TcpClient.Close();

            }

            //gc.MsgReceived += (sender, msg) =>
            //{

            //    StreamWriter sw = new StreamWriter(@"E:\学习\编程\四国军旗的开发\四国军旗\四国军旗Tests\1111.txt", 
            //        true,
            //        Encoding.UTF8);
            //    sw.WriteLine(JsonConvert.SerializeObject(msg));
            //    sw.WriteLine('\n');

            //    };
            Thread.Sleep(1000*6);

            Thread.Sleep(6000);





        }
    }
}