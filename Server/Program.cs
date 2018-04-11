using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server;
namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {

            GameServer gameServer = new GameServer();
            gameServer.StartUp();
            Console.WriteLine("游戏服务器已启动。");
            Console.WriteLine("请按Ctrl+C退出服务器");
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("正在关闭游戏服务器");
                gameServer = null;
                Console.WriteLine("游戏服务器已关闭");
                System.Threading.Thread.Sleep(1000);
            };
            while (true)
            {
                //read string and do something
                Console.ReadLine();
            }

            
        }
    }
}
