using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 四国军旗.DataBase.Tests
{
    [TestClass()]
    public class DataBaseAccessTests
    {
        static DBAcess d = new DBAcess();
        [TestMethod()]
        public void AuthenAccountTest()
        {
            bool b= d.AuthenAccount("123456","123456",out UInt64 ID);
            Assert.AreEqual(b, true);
            Assert.AreEqual(ID,( UInt64) 1);
            b = d.AuthenAccount("Student", "123456", out ID);
            Assert.AreEqual(b, false);
            Assert.AreEqual(ID, (UInt64)0);
        }

        [TestMethod()]
        public void TryQueryPlayerInfoTest()
        {
            bool b = d.TryQueryPlayerInfo(1, out DBAcess.PNGDBRecord playerInfo);
            Assert.AreEqual(b, true);
            Assert.AreEqual(playerInfo.Nickname, "FirstAccount");
            b = d.TryQueryPlayerInfo(5, out  playerInfo);
            Assert.AreEqual(b, false);
            Assert.AreEqual(playerInfo, default(DBAcess.PNGDBRecord));
        }

        [TestMethod()]
        public void TryQueryNormalWeightTest()
        {
            GameLogic.GameMode mode = GameLogic.GameMode.SiAn;
            bool b = d.TryQueryNormalWeight(1, Enum.GetName(typeof(GameLogic.GameMode),mode),out ushort weight);
            Assert.AreEqual(b, true);
            Assert.AreEqual(weight, 0);

            mode=GameLogic.GameMode.Solo;
            b = d.TryQueryNormalWeight(3, Enum.GetName(typeof(GameLogic.GameMode), mode),  out weight);
            Assert.AreEqual(b, false);
            Assert.AreEqual(weight, default(ushort));
        }

    }
}