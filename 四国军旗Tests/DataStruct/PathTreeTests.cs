using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStruct.Tests
{
    [TestClass()]
    public class PathTreeTests
    {
        [TestMethod()]
        public void PathTreeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void InsertAtTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void FindPathOfTest()
        {
            int x=0;
            PathTree<int> paths = new PathTree<int>(x);
            paths.InsertAt(y =>
            {
                if (x == y) return true;
                else return false;
            }, 1);
            int[] i=paths.FindPathOf(y =>
            {
                if (y == -1) return true;
                else return false;
            });
            x = 3;
        }

        [TestMethod()]
        public void ToArrayTest()
        {
            Assert.Fail();
        }
    }
}