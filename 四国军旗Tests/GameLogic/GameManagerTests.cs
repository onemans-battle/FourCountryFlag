using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic.Tests
{
    [TestClass()]
    public class GameManagerTests
    {
        private GameManager initGame( )
        {
            GameManager gameManager = new GameManager();
            int[,] defaultlayout = CheckBroad.GetDefaultLayout();
            
            //进入四个玩家并准备
            gameManager.Enter(OfSide.First);
            gameManager.Ready(OfSide.First, defaultlayout);
            gameManager.Enter(OfSide.Second);
            gameManager.Ready(OfSide.Second, defaultlayout);
            gameManager.Enter(OfSide.Third);
            gameManager.Ready(OfSide.Third,defaultlayout);
            gameManager.Enter(OfSide.Fourth);
            gameManager.Ready(OfSide.Fourth,defaultlayout);



            //4个军旗、4个可行棋的棋子
            gameManager._checkbroad.ClearAllChesses();
            ChessInfo[] chessInfo = new ChessInfo[8]
            {
                new ChessInfo(new Coord(0,7),new Chess(OfSide.Third,ChessType.JunQi)),//0
                new ChessInfo(new Coord(7,0),new Chess(OfSide.Fourth,ChessType.JunQi)),//1
                new ChessInfo(new Coord(7,16),new Chess(OfSide.Second,ChessType.JunQi)),//2
                new ChessInfo(new Coord(16,7),new Chess(OfSide.First,ChessType.JunQi)),//3

                new ChessInfo(new Coord(1,7),new Chess(OfSide.Second,ChessType.PaiZhang)),//4
                new ChessInfo(new Coord(7,1),new Chess(OfSide.Third,ChessType.PaiZhang)),//5
                new ChessInfo(new Coord(7,15),new Chess(OfSide.First,ChessType.PaiZhang)),//6
                new ChessInfo(new Coord(15,7),new Chess(OfSide.Fourth,ChessType.PaiZhang)),//7
            };
            gameManager._checkbroad.Recover(chessInfo);
            return gameManager;
        }
        //在正确输入的情况下，测试对局管理的基本功能：进入游戏、退出游戏、准备、取消准备、行棋。
        [TestMethod()]
        public void SiAnGameNormal()
        {
            int[,] defaultlayout = CheckBroad.GetDefaultLayout();
            GameManager gameManager = new GameManager(GameMode.SiAn);
            //进入两个玩家
            gameManager.Enter(OfSide.First);
            gameManager.Ready(OfSide.First, defaultlayout);
            gameManager.Enter(OfSide.Second);
            gameManager.Ready(OfSide.Second,defaultlayout);
            Assert.AreEqual(gameManager.Status, GameStatus.Layouting);//断言游戏未开始
            //退出一个玩家，进入两个玩家
            gameManager.Exit(OfSide.First);
            gameManager.Enter(OfSide.Third);
            gameManager.Ready(OfSide.Third, defaultlayout);
            gameManager.Enter(OfSide.Fourth);
            gameManager.Ready(OfSide.Fourth, defaultlayout);
            Assert.AreEqual(gameManager.Status, GameStatus.Layouting);//断言游戏未开始
            gameManager.Enter(OfSide.First);
            gameManager.Ready(OfSide.First, defaultlayout);
            Assert.AreEqual(gameManager.Status, GameStatus.Doing);//断言游戏开始


            //下棋
            gameManager._checkbroad.ClearAllChesses();
            //4个军旗、4个可行棋的棋子
            ChessInfo[] chessInfo = new ChessInfo[8]
            {
                new ChessInfo(new Coord(0,7),new Chess(OfSide.Third,ChessType.JunQi)),//0
                new ChessInfo(new Coord(7,0),new Chess(OfSide.Fourth,ChessType.JunQi)),//1
                new ChessInfo(new Coord(7,16),new Chess(OfSide.Second,ChessType.JunQi)),//2
                new ChessInfo(new Coord(16,7),new Chess(OfSide.First,ChessType.JunQi)),//3

                new ChessInfo(new Coord(1,7),new Chess(OfSide.Second,ChessType.PaiZhang)),//4
                new ChessInfo(new Coord(7,1),new Chess(OfSide.Third,ChessType.PaiZhang)),//5
                new ChessInfo(new Coord(7,15),new Chess(OfSide.First,ChessType.PaiZhang)),//6
                new ChessInfo(new Coord(15,7),new Chess(OfSide.Fourth,ChessType.PaiZhang)),//7
            };

            gameManager._checkbroad.Recover(chessInfo);

            gameManager.Move(OfSide.First, new Coord(7, 15), new Coord(6, 15));//玩家一正常行走
            Assert.AreEqual(gameManager.Status, GameStatus.Doing);//断言游戏未胜利
            gameManager.Move(OfSide.Second, new Coord(1, 7), new Coord(0,7));//玩家二把玩家三吃了
            Assert.AreEqual(gameManager.Status, GameStatus.Doing);//断言游戏未胜利
            //gameManager.Move(OfSide.Third, new Coord(7, 1), new Coord(7, 0));//玩家三把玩家四吃了
            //Assert.AreEqual(gameManager.Status, GameStatus.Doing);//断言游戏未胜利

            
            OfSide[] thewinners=null;
            bool win=false;
            gameManager.GameOver += (sender, gameresult) =>
            {
                thewinners = gameresult.Winers;
                win = true;
            };
            gameManager.Move(OfSide.Fourth, new Coord(15, 7), new Coord(16,7));//玩家四把玩家一吃了
                                                                               
            Assert.IsTrue(win);//断言游戏已胜利
            //断言游戏胜利者是玩家2和玩家4
            Assert.IsTrue(thewinners.Contains(OfSide.Fourth));
            Assert.IsTrue(thewinners.Contains(OfSide.Second));
        }

        //投降、超时和棋、拒绝和棋、同意和棋
        [TestMethod()]
        public void SianGameExtend()
        {
            GameManager gameManager=initGame();


            bool over=false;
            bool isDraw=false;
            OfSide[] winners = null ;
            gameManager.GameOver += (sender,gameresule) => {
                over = true;
                isDraw = gameresule.IsDraw;
                winners = gameresule.Winers;
            };


            //投降
            gameManager.Surrender(OfSide.First);
            Assert.AreEqual(over, false);//断言游戏未结束
            gameManager.Surrender(OfSide.Third);
            Assert.AreEqual(over, true);//断言游戏结束

           
            gameManager = initGame();
            over = false;
            isDraw = false;
            winners = null;
            gameManager.GameOver += (sender, gameresule) => {
                over = true;
                isDraw = gameresule.IsDraw;
                winners = gameresule.Winers;
            };

            gameManager.Surrender(OfSide.Second);
            Assert.AreEqual(over, false);//断言游戏未结束
            gameManager.Surrender(OfSide.Third);
            Assert.AreEqual(over, false);//断言游戏未结束
            gameManager.Surrender(OfSide.Fourth);
            Assert.AreEqual(over, true);//断言游戏结束
            //断言游戏胜利者为玩家1和玩家3
            Assert.IsTrue(winners.Contains(OfSide.First));
            Assert.IsTrue(winners.Contains(OfSide.Third));

            //超时的和棋请求
            gameManager = initGame();
            over = false;
            isDraw = false;
            winners = null;
            gameManager.GameOver += (sender, gameresule) => {
                over = true;
                isDraw = gameresule.IsDraw;
                winners = gameresule.Winers;
            };
            bool isExpire = false;
            gameManager.ExpireDraw += (sender, e) =>
            {
                isExpire = true;
            };
            gameManager.OfferDraw(OfSide.Fourth);
            gameManager.AgreeDraw(OfSide.First);
            gameManager.AgreeDraw(OfSide.Second);
            System.Threading.Thread.Sleep(35 * 1000);
            Assert.AreEqual(over, false);//断言游戏未结束
            Assert.IsTrue(isExpire);//断言和棋请求超时了

            //及时回应的同意和棋请求
            gameManager = initGame();
            over = false;
            isDraw = false;
            winners = null;
            gameManager.GameOver += (sender, gameresule) => {
                over = true;
                isDraw = gameresule.IsDraw;
                winners = gameresule.Winers;
            };
            gameManager.OfferDraw(OfSide.Fourth);
            gameManager.AgreeDraw(OfSide.First);
            gameManager.AgreeDraw(OfSide.Second);
            gameManager.AgreeDraw(OfSide.Third);
            Assert.IsTrue(over);//断言游戏结束
            Assert.IsTrue(isDraw); //断言和棋
            //断言全部玩家都获胜了
            Assert.IsTrue(winners.Contains(OfSide.First));
            Assert.IsTrue(winners.Contains(OfSide.Second));
            Assert.IsTrue(winners.Contains(OfSide.Third));
            Assert.IsTrue(winners.Contains(OfSide.Fourth));

            //及时回应的拒绝和棋请求
            gameManager = initGame();
            over = false;
            isDraw = false;
            winners = null;
            gameManager.GameOver += (sender, gameresule) => {
                over = true;
                isDraw = gameresule.IsDraw;
                winners = gameresule.Winers;
            };
            isExpire = false;
            gameManager.ExpireDraw += (sender, e) =>
            {
                isExpire = true;
            };
            gameManager.OfferDraw(OfSide.Fourth);
            gameManager.AgreeDraw(OfSide.First);
            gameManager.AgreeDraw(OfSide.Second);
            gameManager.RefuseDraw(OfSide.Third);
            Assert.IsFalse(over);//断言游戏结束
            Assert.IsFalse(isExpire);//断言和棋请求未超时

        }

        //测试所有事件(19个)是否正确激发
        [TestMethod()]
        public void TestAllEventWorks()
        {
            GameManager gameManager = new GameManager();
            int[,] defaultlayout = CheckBroad.GetDefaultLayout();
            //进入3个玩家,玩家1和玩家2准备
            gameManager.Enter(OfSide.First);
            gameManager.Ready(OfSide.First, defaultlayout);
            gameManager.Enter(OfSide.Second);
            gameManager.Ready(OfSide.Second, defaultlayout);
            gameManager.Enter(OfSide.Third);
            
            
            /*
             * 
             * */
            //1.玩家进入游戏：玩家4 进入游戏 
            bool fire1 = false;
            gameManager.PlayerEnter += (sender, e) => {
                fire1 = true;
            };
            gameManager.Enter(OfSide.Fourth);
            Assert.IsTrue(fire1);

            //2.玩家退出游戏：玩家4退出游戏进入游戏
            bool fire2 = false;
            gameManager.PlayerExit += (sender, e) => {
                fire2 = true;
            };
            gameManager.Exit(OfSide.Fourth);
            gameManager.Enter(OfSide.Fourth);
            Assert.IsTrue(fire2);

            //3.玩家准备：玩家4准备 
            bool fire3 = false;
            gameManager.PlayerReady += (sender, e) => {
                fire3 = true;
            };
            gameManager.Ready(OfSide.Fourth, defaultlayout);
            Assert.IsTrue(fire3);

            //4.玩家取消准备:玩家4 取消准备准备
            bool fire4 = false;
            gameManager.PlayerCancelReady += (sender, e) => {
                fire4 = true;
            };
            gameManager.CancelReady(OfSide.Fourth);
            gameManager.Ready(OfSide.Fourth, defaultlayout);
            Assert.IsTrue(fire4);

            //5.游戏开始：玩家3准备即开始游戏
            bool fire5 = false;
            gameManager.GameStart += (sender, e) => {
                fire5 = true;
            };
            gameManager.Ready(OfSide.Third, defaultlayout);
            Assert.IsTrue(fire5);

            //6.玩家行棋，11，10走到10，11 对称再走两步
            bool fire6 = false;
            gameManager.PlayerChessMove += (sender, e) => {
                fire6 = true;
            };
            gameManager.Move(OfSide.First, new Coord(11, 10), new Coord(10, 11));//玩家1的司令在11，6
            gameManager.Move(OfSide.Second, new Coord(6, 11), new Coord(5, 10));
            gameManager.Move(OfSide.Third, new Coord(5, 6), new Coord(6, 5));
            Assert.IsTrue(fire6);

            //7.轮到下一玩家行棋
            bool fire7 = false;
            gameManager.SideNext += (sender, e) => {
                fire7 = true;
            };
            gameManager.Move(OfSide.Fourth, new Coord(6, 5), new Coord(6, 6));//玩家4的司令在6, 6
            Assert.IsTrue(fire7);

            //8.玩家提出和棋
            bool fire8 = false;
            gameManager.PlayerOfferDraw += (sender, e) => {
                fire8 = true;
            };
            gameManager.OfferDraw(OfSide.First);
            Assert.IsTrue(fire8);

            //9.玩家同意和棋
            bool fire9 = false;
            gameManager.PlayerAgrreDraw += (sender, e) => {
                fire9 = true;
            };
            gameManager.AgreeDraw(OfSide.Second);
            Assert.IsTrue(fire9);

            ////10.和棋超时：通过。这步测试会使下方行棋的时间超额，也就是跳过了玩家1的行棋
            //bool fire10 = false;
            //gameManager.ExpireDraw += (sender, e)=>{
            //    fire10 = true;
            //};
            //System.Threading.Thread.Sleep(31 * 1000);
            //Assert.IsTrue(fire10);

            //11.玩家拒绝和棋
            bool fire11 = false;
            gameManager.PlayerRefuseDraw += (sender, e) => {
                fire11 = true;
            };
            gameManager.RefuseDraw(OfSide.Second);
            Assert.IsTrue(fire11);

            //12.玩家阵亡,用投降来模拟
            bool fire12 = false;
            gameManager.PlayerDie += (sender, e) => {
                fire12 = true;
            };
            gameManager.Surrender(OfSide.Second);
            Assert.IsTrue(fire12);


            //13.玩家司令死亡
            bool fire13 = false;
            gameManager.PlayerSiLingDied += (sender, e) => {
                fire13 = true;
            };
            gameManager.Move(OfSide.First, new Coord(11, 6), new Coord(6, 6));//玩家1的司令在11，6
            Assert.IsTrue(fire13);

            //14.玩家投降
            bool fire14 = false;
            gameManager.PlayerSurrender += (sender, e) => {
                fire14 = true;
            };
            gameManager.Surrender(OfSide.Third);
            Assert.IsTrue(fire14);
            //至此玩家2和玩家3阵亡
            //15.玩家强制退出
            bool fire15 = false;
            gameManager.PlayerForceExit += (sender, e) => {
                fire15 = true;
            };
            gameManager.Exit(OfSide.First);
            Assert.IsTrue(fire15);

            ////16.玩家掉线
            //fire = false;
            //gameManager.PlayerLost += (sender, e) => {
            //    fire = true;
            //};
            //Assert.IsTrue(fire);

            ////17.玩家重连
            //fire = false;
            //gameManager.PlayerReconnect += (sender, e) => {
            //    fire = true;
            //};
            //Assert.IsTrue(fire);

            //18.游戏结束
            bool fire18 = false;
            GameManager game = initGame();
            game.GameOver += (sender, e) => {
                fire18 = true;
            };
            game.Surrender(OfSide.First);
            game.Surrender(OfSide.Third);
            Assert.IsTrue(fire18);

            //19.游戏关闭
            bool fire19 = false;
            gameManager.GameClose += (sender, e) => {
                fire19= true;
            };
            gameManager.Close();
            Assert.IsTrue(fire19);

            
        }
    }
}