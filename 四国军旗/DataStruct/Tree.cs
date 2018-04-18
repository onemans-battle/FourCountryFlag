using System;
using System.Collections.Generic;


namespace DataStruct
{

    /*路径树实现(用于描述棋子可行走的顶点)：
     * 操作：初始化根节点；插入在某个节点下；输出到达某节点的路径
     *       输出所有节点的数据；
     *     
    */

    internal class PathTreeNode<DT>
    {
        private int _parentID;
        internal DT Data;//用于标识位置信息的唯一的数据
        internal PathTreeNode(DT d ,int parentID=-1)
        {
            Data = d;
            _parentID = parentID;
        }
        internal bool IsRoot()
        {
            if (_parentID == -1) return true;
            else return false;
        }

        internal int ParentID()
        {
            return _parentID;
        }

    }
    //描述根节点的路径信息
    public class PathTree<DT>
    {
        
        public int Count
        {
            get
            {
                return _nodelist.Count;
            }
        }
        //初始化根节点
        public PathTree(DT rootDT, int defaultSize = 20)
        {
            _nodelist = new List<PathTreeNode<DT>>(defaultSize);
            _nodelist.Add(new PathTreeNode<DT>(rootDT));
        }
        //插入在某个节点下
        public void InsertAt(Predicate<DT> match, DT d)
        {
            int index = indexOf(match);
            if (index != -1)
            {
                _nodelist.Add(new PathTreeNode<DT>(d, index));
            }
            else throw new Exception("插入路径点发生错误：无匹配的根节点");
        }
        //输出到达某节点的路径(包含起始和终顶点DT)。
        //若无到此节点的路径或此节点是根节点，则返回空数组DT[0]
        public DT[] FindPathOf(Predicate<DT> match)
        {
            int index = indexOf(match);
            if(index==-1) return new DT[0];//路径中无此节点
            if(_nodelist[index].IsRoot()) return new DT[0];//节点是根节点

            Stack<DT> stack = new Stack<DT>(20);
            while (!_nodelist[index].IsRoot())//在到达根节点之前，不断地
            {
                stack.Push(_nodelist[index].Data);
                index = _nodelist[index].ParentID();
            }
            stack.Push(_nodelist[index].Data);//根节点
            return stack.ToArray();

        }
        /// <summary>
        /// 输出所有节点的数据；包括根节点(在索引0位置)
        /// </summary>
        /// <returns></returns>
        public DT[] ToArray()
        {
            DT[] dtArray = new DT[_nodelist.Count];
            for (int i = 0; i < _nodelist.Count; i++)
            {
                dtArray[i] = _nodelist[i].Data;
            }
            return dtArray;
        }


        private List<PathTreeNode<DT>> _nodelist;
        //查找不到返回-1
        private int indexOf(Predicate<DT> match)
        {
            for (int i = 0; i < _nodelist.Count; i++)
            {
                if (match(_nodelist[i].Data)) return i;
            }
            return -1;
        }

    }

    /*对吃树：
     * 操作：初始化根节点；插入在某个节点下；
     */
    //public enum Status
    //{
    //    UNDISCOVERED,
    //    DISCOVERED,
    //    VISITED
    //}

}
