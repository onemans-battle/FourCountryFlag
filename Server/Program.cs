using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server;
using System.Net;
namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                IPAddress ipAddress; int port;
                Console.WriteLine("-------------------------------------------------------------");
                Console.WriteLine("请输入绑定的服务器地址与端口，中间用‘:’隔开,如127.0.0.1:8080。");
                Console.WriteLine("输入'1'使用本机任意地址和固定端口8080。");
                Console.WriteLine("-------------------------------------------------------------");
                string bindIPAndPort = Console.ReadLine();
                if (bindIPAndPort == "1")
                {
                    ipAddress = IPAddress.Any;
                    port = 8080;
                }
                else
                {
                    try
                    {
                        string[] s = bindIPAndPort.Split(':');
                        ipAddress =IPAddress.Parse(s[0]);
                        port = Convert.ToInt32(s[1]);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("格式错误，请重新输入。");
                        continue;
                    }
                }
                try
                {
                    GameServer gameServer = new GameServer(ipAddress, port);
                    //
                    gameServer._connector.SessiontAccept += (sender, e) => {
                        Console.WriteLine("接收到来自 " + e.TcpClient.Client.RemoteEndPoint.ToString() + " 的连接.");
                    };
                    gameServer._connector.MsgReceived += (sender, e) => {
                        Console.WriteLine("接收到来自 " + e.Session.TcpClient.Client.RemoteEndPoint.ToString()+" 的 "+ e.MsgType.Name+" 请求.");
                    };
                    gameServer._connector.SessionClose += (sender, e) => {
                        Console.WriteLine("来自 " + e.TcpClient.Client.RemoteEndPoint.ToString() + " 的连接已关闭.");
                    };
                    //
                    gameServer.StartUp();
                    Console.WriteLine("游戏服务器(" + ipAddress.ToString()+":"+port.ToString()+ ")已启动. 请按C退出服务器.");
                    while (Console.ReadKey(true).Key != ConsoleKey.C) ;
                    Console.Write("游戏服务器(" + ipAddress.ToString() + ":" + port.ToString() + ")正在退出中.....");
                    gameServer.Close();
                    Console.Write("已退出!\n");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }

        }
    }
}
