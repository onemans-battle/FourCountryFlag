using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestTool;
namespace GameLogic.Tests
{
    [TestClass()]
    public class CheckBroadTests
    {
        private CheckBroad CreateDefaultCheckboard()
        {
            CheckBroad cb = new CheckBroad(GameMode.SiMing);
            List<ChessInfo> cIL = new List<ChessInfo>();
            
            byte[,] typeTable=new byte[17, 17]{
                {00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,37,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,32,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,32,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,00,32,00,00,00,00,00,00,00,00},
                //
                {00,00,00,00,00,00,00,00,00,00,36,00,00,00,00,00,00},
                {00,00,00,00,00,38,00,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,33,00,37,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,37,00,00,00,00,00,00,00,00,00,00,00},
                //
                {00,00,00,00,00,00,00,33,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,38,00,00,00,00,00,00,00,00,00,00},
                {00,00,00,00,00,00,00,31,00,33,36,00,00,00,00,00,00}
            };
            byte[,] sideTalbe=new byte[17, 17]
                {
                {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                //
                {4,4,4,4,4,4,1,0,1,0,1,2,2,2,2,2,2},
                {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
                {4,4,4,4,4,4,1,0,1,0,1,2,2,2,2,2,2},
                {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
                {4,4,4,4,4,4,1,0,1,0,1,2,2,2,2,2,2},
                //
                {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
                {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
                {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
                {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
                {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
                {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0}
            };
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 17; j++)
                {
                    if (typeTable[i, j] != 0)
                    {
                        cIL.Add(new ChessInfo(new Coord(i,j),
                                              new Chess((OfSide)sideTalbe[i,j],(ChessType)typeTable[i,j]))
                                );
                    }
                }
            }
            cb.Recover(cIL.ToArray());
            return cb;
        }
        [TestMethod()]
        public void CheckBroadTest()
        {
            Chess c1 = new Chess(OfSide.First, ChessType.SiLing);
            Chess c2 = new Chess(OfSide.First, ChessType.SiLing);
            Assert.AreEqual(c1.Equals(c2), true);
        }

        [TestMethod()]
        public void ConvertTest()
        {
            //
            int[,] layout = CheckBroad.GetDefaultLayout();
            ChessInfo[] chessInfo = CheckBroad.ConvertFromLayoutToCheInfo(null,OfSide.First);
            int[,] endlayout= CheckBroad.ConvertFromCheInfoToLayout(chessInfo);
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Assert.AreEqual(layout[i,j], endlayout[i, j]);
                }
            }

            chessInfo = CheckBroad.ConvertFromLayoutToCheInfo(layout, OfSide.Second);
            endlayout = CheckBroad.ConvertFromCheInfoToLayout(chessInfo);
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Assert.AreEqual(layout[i, j], endlayout[i, j]);
                }
            }

            chessInfo = CheckBroad.ConvertFromLayoutToCheInfo(layout, OfSide.Third);
            endlayout = CheckBroad.ConvertFromCheInfoToLayout(chessInfo);
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Assert.AreEqual(layout[i, j], endlayout[i, j]);
                }
            }

            chessInfo = CheckBroad.ConvertFromLayoutToCheInfo(layout, OfSide.Fourth);
            endlayout = CheckBroad.ConvertFromCheInfoToLayout(chessInfo);
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Assert.AreEqual(layout[i, j], endlayout[i, j]);
                }
            }


        }

        [TestMethod()]
        public void ExchangeChessTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void LayoutTest()
        {
            int[,] layout = new int[6, 5]
            {
                {00,36,32,33,33 },
                {37,-2,32,-2,33 },
                {37,33,-2,33,33 },
                {37,-2,32,-2,33 },
                {37,32,32,32,33 },
                {37,36,32,33,33 }
            };
            CheckBroad cb = new CheckBroad(GameMode.SiAn);
            cb.Layout(CheckBroad.ConvertFromLayoutToCheInfo(layout, OfSide.First));
            //Assert.Fail();
        }

        [TestMethod()]
        public void RecoverTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void FindAllPathsTest()
        {
            //师长处于五线【15,6】测试通过 处于一线【7,5】测试通过
            //工兵寻路正确【5,7】
            //处在行营，或者在特殊兵站上：【2,7】【3,7】 测试通过
            //星宫 通过
            CheckBroad cb =CreateDefaultCheckboard();
            Coord[] paths = cb.FindAllPaths(new Coord(15, 6)).ToArray();
                                //FindPath((c) =>
                                //{
                                //    if (c.i == 14 && c.j == 6)
                                //    {
                                //        return true;
                                //    }
                                //    else return false;
                                //});

            byte[,] table = new byte[17, 17];
            for (int i = 0; i < paths.Length; i++) //每一可到达的顶点
            {
                table[paths[i].i, paths[i].j] = 1;
            }
            string s = ExchangeToString.Array(table,
                                                (item) => item.ToString()
                                                );
            
            //Assert.Fail();
        }

        [TestMethod()]
        public void MoveTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SurrenderTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetWinnerTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetCurrentChessesTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToStringsTest()
        {
            CheckBroad cb = CreateDefaultCheckboard();
            cb.ToStrings();
        }

        [TestMethod()]
        public void IsOneSideChessesTest()
        {
            //true
            int[] typeT = new int[25]
            {
                40,39,31,
                38,38,37,33,36,36,35,35,00,00,
                34,34,34,37,33,33,32,32,32,41,41,41,
            };
            Chess[] chessT = new Chess[25];
            for (int i = 0; i < typeT.Length; i++)
            {
                chessT[i] = new Chess(OfSide.First, (ChessType)typeT[i]);
            }
            bool b= Chess.IsOneSideAllChesses(chessT,OfSide.First);
            
            Assert.AreEqual(b,true);
            //true
            b = Chess.IsOneSideAllChesses(Chess.GetChessesOf(OfSide.First), OfSide.First);
            Assert.AreEqual(b, true);
            //false:
            b = Chess.IsOneSideAllChesses(Chess.GetChessesOf(OfSide.Fourth), OfSide.First);
            Assert.AreEqual(b, false);
            //false:
            typeT = new int[25]
            {
                41,39,31,
                38,38,37,37,36,36,35,35,00,00,
                34,34,34,33,33,33,32,32,32,41,41,41,
            };
            for (int i = 0; i < typeT.Length; i++)
            {
                chessT[i] = new Chess(OfSide.First, (ChessType)typeT[i]);
            }
            b = Chess.IsOneSideAllChesses(chessT, OfSide.First);

            Assert.AreEqual(b, false);
            //false
            typeT = new int[25]
            {
                40,39,31,
                38,38,37,37,36,36,35,35,00,00,
                34,35,34,33,33,33,32,32,32,41,41,41,
            };
            for (int i = 0; i < typeT.Length; i++)
            {
                chessT[i] = new Chess(OfSide.First, (ChessType)typeT[i]);
            }
            b = Chess.IsOneSideAllChesses(chessT, OfSide.First);

            Assert.AreEqual(b, false);
        }
    }
}