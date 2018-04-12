using Microsoft.VisualStudio.TestTools.UnitTesting;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetConnector.MsgType;
using NetConnector;
namespace Server.Tests
{
    [TestClass()]
    public class GameServerTests
    {
        [TestMethod()]
        public void GameServerTest()
        {
            
        }

        [TestMethod()]
        public void StartUpTest()
        {
            GameServer gameServer = new GameServer(System.Net.IPAddress.Loopback);
            gameServer.StartUp();

            LoginIn loginIn = new LoginIn() { UserName = "123456", Password = "123456" };
            GameTCPClient gameTCPClient = new GameTCPClient();
            gameTCPClient.StartUp(System.Net.IPAddress.Loopback);
            gameTCPClient.SendAsync(loginIn);
            gameTCPClient.SendAsync(new Match() { GameMode=GameLogic.GameMode.SiAn});
            gameTCPClient.SendAsync(new Match() { GameMode = GameLogic.GameMode.SiAn });
            gameTCPClient.SendAsync(new Match() { GameMode = GameLogic.GameMode.SiAn });
            gameTCPClient.SendAsync(new Match() { GameMode = GameLogic.GameMode.SiAn });
            System.Threading.Thread.Sleep(1000 * 5);

            gameServer.ToString();
            System.Threading.Thread.Sleep(1000*300);
            
        }

        [TestMethod()]
        public void GenerateTokenTest()
        {
            Assert.Fail();
        }
    }
}