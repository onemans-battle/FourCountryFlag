using DataStruct;//自主实现的数据结构
using System;
using System.Collections; // 导入ArrayList的命名空间
using System.Collections.Generic;
using TestTool;//发布时可去除
using System.ComponentModel;
namespace GameLogic
{/*棋盘是用于描述、控制、管理在具体某个游戏阶段中的棋子，让它们符合游戏规则，对于棋子的全部操控都是由棋盘来具体实现。
    但棋盘不对游戏进程进行管控，它总是默认外部在某个阶段在调用它的方法，可以说它不关心游戏进程。
    0 1   2 3 4 5   6 7  8 9  10 11 12.13.14 15 16 
* 0 ✡-✡--✡-✡-✡-✡--☐-♖-☐-♖-☐ -✡-✡-✡-✡--✡-✡---
* 1 ✡ ✡  ✡ ✡ ✡ ✡  ☐ ☐ ☐ ☐ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 2 ✡ ✡  ✡ ✡ ✡ ✡  ☐ ⊙ ☐ ⊙ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 3 ✡ ✡  ✡ ✡ ✡ ✡  ☐ ☐ ⊙ ☐ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 4 ✡ ✡  ✡ ✡ ✡ ✡  ☐ ⊙ ☐ ⊙ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 5 ✡ ✡  ✡ ✡ ✡ ✡  ☐ ☐ ☐ ☐ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 6 ☐ ☐ ☐ ☐ ☐ ☐ ✦ ✡  ✦ ✡ ✦ ☐ ☐ ☐ ☐ ☐ ☐
* 7 ♖ ☐ ⊙ ☐ ⊙ ☐ ✡ ✡  ✡ ✡ ✡ ☐ ⊙ ☐ ⊙ ☐ ♖
* 8 ☐ ☐ ☐ ⊙ ☐ ☐ ✦ ✡  ✦ ✡ ✦ ☐ ☐ ⊙ ☐ ☐ ☐
* 9 ♖ ☐ ⊙ ☐ ⊙ ☐ ✡ ✡  ✡ ✡ ✡ ☐ ⊙ ☐ ⊙ ☐ ♖
* 10☐ ☐ ☐ ☐ ☐ ☐ ✦ ✡  ✦ ✡ ✦ ☐ ☐ ☐ ☐ ☐ ☐
* 11✡ ✡  ✡ ✡ ✡ ✡  ☐ ☐ ☐ ☐ ☐  ✡ ✡ ✡ ✡  ✡ ✡  
* 12✡ ✡  ✡ ✡ ✡ ✡  ☐ ⊙ ☐ ⊙ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 13✡ ✡  ✡ ✡ ✡ ✡  ☐ ☐ ⊙ ☐ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 14✡ ✡  ✡ ✡ ✡ ✡  ☐ ⊙ ☐ ⊙ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 15✡ ✡  ✡ ✡ ✡ ✡  ☐ ☐ ☐ ☐ ☐  ✡ ✡ ✡ ✡  ✡ ✡ 
* 16✡ ✡  ✡ ✡ ✡ ✡  ☐ ♖ ☐ ♖ ☐ ✡ ✡ ✡ ✡  ✡ ✡ 
* | 
* 
*/
 //公开信息
 //游戏模式
    public enum GameMode:byte
    {
        SiMing,//四明
        SiAn,//四暗
        ShuangMing,//双明
        Solo//(暗棋)单挑
    }

    //棋子的
    //归属势力
    public enum OfSide
    {
        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4
    }
    public enum ChessType
    {
        SiLing = 40,//司令
        JunZhang = 39,//军长
        ShiZhang = 38,//师长
        LvZhang = 37,//旅长
        TuanZhang = 36,//团长
        YingZhang = 35,//营长
        LianZhang = 34,//连长
        PaiZhang = 33,//排长
        GongBing = 32,//工兵
        JunQi = 31,//军旗
        DiLei = 41, //地雷
        Bomb = 00,//炸弹
        UnKnow = -1 //未知,存在于暗棋游戏模式中
    }
    //坐标
    public struct Coord
    {
        public int i, j;
        public Coord(int ii, int jj)
        {
            i = ii;
            j = jj;
        }
        public static bool operator ==(Coord c1, Coord c2)
        {
            if (c1.i == c2.i && c1.j == c2.j)
                return true;
            else return false;
        }
        public static bool operator !=(Coord c1, Coord c2)
        {
            return !(c1 == c2);
        }
    }
    public class Chess
    {
        public OfSide Side;
        public ChessType Type;
        public Chess(OfSide s, ChessType t)
        {
            Side = s;
            Type = t;
        }
        //获取某方所有的初始棋子
        public static Chess[] GetChessesOf(OfSide side)
        {
            int[] typeT = new int[25]
            {
                40,39,31,
                38,38,37,37,36,36,35,35,00,00,
                34,34,34,33,33,33,32,32,32,41,41,41,
            };
            Chess[] chessT = new Chess[25];
            for (int i = 0; i < typeT.Length; i++)
            {
                chessT[i] = new Chess(side, (ChessType)typeT[i]);
            }
            return chessT;
        }
        //判断该棋子集合是否是某方能拥有的所有棋子，若有暗棋则不予通过
        public static bool IsOneSideAllChesses(Chess[] chessT,OfSide side)
        {
            if (chessT.Length!=25) //棋子数量不等于25
                return false;

            int[,] numInfo = new int[2,12]
            {
                {41,40,39,38,37,36,35,34,33,32,31,00},
                {3 ,1 ,1 ,2 ,2 ,2 ,2 ,3 ,3 ,3 ,1 ,2 }
            };
            bool flag;
            foreach (Chess chess in chessT)//每个棋子
            {
                if (side != chess.Side)//不是归属于指定的一方
                    return false;
                flag = false;//假定此棋子无法识别
                for (int i = 0; i < 12; i++)//在每个信息项中
                {
                    if(numInfo[0,i]==(int)chess.Type)//尝试查找此棋子的数量记录
                    {
                        numInfo[1, i]--;
                        flag = true;
                    }

                }
                if (flag == false) return false;//在每个信息项无此棋子
            }
            for (int i = 0; i < 12; i++)//在每项棋子数量记录中
            {
                if (numInfo[1, i] != 0)//验证
                    return false;
            }
            return true;
        }
        //棋子对撞规则：左边吃右边返回不大于9的正数，同去返回0，右边吃左边返回不小于-9负数
        //异常：暗棋无法比大小、不会发生对撞的棋子对撞、其他不存在规则里的异常
        public static int operator -(Chess c1,Chess c2)
        {
            int o1 = (int)c1.Type, o2 = (int)c2.Type;
            //军阶(包含军旗)比大小:高军阶吃小军阶，同军阶同去
            if (31<= o1 && o1 <= 40 && 31 <= o2 && o2 <= 40)
            {
                return o1 - o2;
            }
            //工兵消地雷，其他军阶棋子遇地雷皆亡。
            if (c1.Type==ChessType.DiLei || c2.Type == ChessType.DiLei)//至少一个为地雷
            {
                if (c1.Type == ChessType.GongBing) return 1;//另一子为工兵
                else if (c2.Type == ChessType.GongBing) return -1;//另一子为工兵
                else if ( (33 <= o1 && o1 <= 40) || (33 <= o2 && o2 <= 40)) //另一子为非工兵的军阶棋子
                    return o1 - o2;
                else if(o1==o2) //同为地雷
                    throw new GameRuleException("棋子对撞发生异常：地雷相撞。"); 
                else if(c1.Type==ChessType.JunQi || c2.Type == ChessType.JunQi) //另一子为军旗
                    throw new GameRuleException("棋子对撞发生异常：地雷不会与军旗相撞。");
            }
            //炸弹遇任何棋子同去。
            if(c1.Type == ChessType.Bomb || c2.Type == ChessType.Bomb) //至少一个为炸弹
            {
                return 0;
            }
            //棋子类型未知
            if(o1==-1||o2==-1)  throw new GameRuleException("棋子对撞发生异常：存在暗棋。");
            //其他异常
            throw new GameRuleException("棋子对撞发生异常：其他不符合规则的异常。");
        }
        public bool Equals(Chess chess)
        {
            if (chess.Side == Side && chess.Type == Type) return true;
            else return false;
        }
    }
    public struct ChessInfo
    {
        public Coord coord;
        public Chess chess;
        public ChessInfo(Coord co, Chess ch)
        {
            coord = co;
            chess = ch;
        }
    }
    //胜利方
    public enum Winner
    {
        Unknown,//未分胜负
        All,//平局
        First,
        Sencond,
        Third,
        Fourth,
        FirstAndTthird,
        SencondAndFourth
    }
    //棋子行走的结果
    public enum MoveResult
    {
        Normal,//正常移动：没有吃棋子行为
        Eat,//吃掉
        BeEated,//被吃
        TheSameTimeToDie//同去
    }
    public struct MoveInfo
    {
        public MoveResult MType;//
        public ChessInfo MovingChess;//要行走的棋子信息
        public ChessInfo CrashedChess;//被碰撞的棋子：当发生碰撞时此信息才有意义
        public Coord[] Path;//行走路径，非法行走为空数组
        
    }




    //有限公开的信息
    //顶点/落棋点
    //落棋点位置类型：0:无 1:兵站 2：行营 3：星宫 4：大本营
    public enum VertexType
    {
        None = 0,
        BingZhan = 1,
        XingYing = 2,
        XinGong = 3,
        DaBenYing = 4
    }
    public enum Place
    {
        First = 1,//一线
        Seconed = 2,//二线
        Third = 3,//三线至四线
        Fourth = 4,
        Fifth = 5,
        Sixth = 6,
        None = 0//星宫和空顶点没有此信息
    }
    //当前路由情况
    public enum Route:byte
    {
        None = 0x0,
        Horizontal = 0x1,//可水平方向行走铁路
        Vertical = 0x2,
        Diagonal = 0x4,//可倾斜方向行走铁路
        Highway =0x8,//可随意走公路
        All = 0xF //所有方式均可行走
    }
    //顶点状态
    public enum VStatus
    {
        UNDISCOVERED,
        DISCOVERED,
        VISITED
    }
    //顶点
    //其中空顶点的信息为:顶点类型为none，位置为none，路由all，所属势力none
    public class VertexData:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private Chess _chess;
        public Chess Chess//顶点数据：棋子
        {
            get
            {
                return _chess;
            }
            set
            {
                _chess = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Chess"));
                }
            }
        }
        internal Route route;
        internal VStatus status;

        //判断顶点上是否有棋子
        public bool ExistChess()
        {
            if (Chess == null) return false;
            else return true;
        }

        internal VertexData()
        {
            PropertyChanged = null;
            route = Route.All;
            status =VStatus.UNDISCOVERED;
            Chess = null;
        }
        
        //设置行走能力
        internal void SetRoute(Route r)
        {
            route = r;
        }

        //根据当前路由能力，判断当前顶点可否行走相邻边edge
        internal bool CanWalkEdge(EdgeNode edge)
        {
            Route option= ExchangeETOR(edge);
            if ((route & option) == option) return true;
            else return false;
        }
        
        //解释行走该边，顶点需拥有的路由能力
        internal static Route ExchangeETOR(EdgeNode edge)
        {
            Route option = Route.None;
            //若边是铁路
            if (edge.type == EdgeType.Railway)
            {
                switch (edge.direction)
                {
                    case EdgeDirection.Horizontal:
                        {
                            option = Route.Horizontal;
                        }
                        break;
                    case EdgeDirection.Vertical:
                        {
                            option = Route.Vertical;
                        }
                        break;
                    case EdgeDirection.Diagonal:
                        {
                            option = Route.Diagonal;
                        }
                        break;
                }
            }
            //若边是公路
            else if (edge.type == EdgeType.HighWay)
            {
                option = Route.Highway;
            }
            return option;
        }

        
    }
    /// <summary>
    /// 顶点固有信息，
    /// </summary>
    public class VertexInfo
    {
        public readonly VertexType Type;
        public readonly Place Place;
        public readonly OfSide Side;
        public VertexInfo(VertexType type, Place place, OfSide side)
        {
            Type = type;
            Place = place;
            Side = side;
        }
    }
    //边
    public enum EdgeType
    {
        HighWay,
        Railway
    }
    public enum EdgeDirection
    {
        Horizontal,
        Vertical,
        Diagonal
    }
    public struct EdgeNode
    {
        internal readonly int y;//邻接点域y=i*n+j
        public readonly EdgeType type;
        public readonly EdgeDirection direction;
        internal EdgeNode(int yy, EdgeType t, EdgeDirection d)
        {
            y = yy;
            type = t;
            direction = d;
        }
    }

    //游戏模式中的阵营信息
    public enum Alignment
    {
        First=1,
        Second=2
    }

    //顶点的道路属性，仅用于创建边集
    public enum HaveRoad
    {
        HighWay = 0x1,
        Railway = 0x2,
        All = 0x3
    }
    //棋盘
    public class CheckBroad
    {
        #region 公开接口，游戏规则的实现：
        public VertexData[,] VertexDataM { get; internal set; }//顶点矩阵：索引为[i,j]，顶点编号x=i*n+j。
        public static VertexInfo[,] VertexInfoM { get; internal set; }//棋子底座信息，不因不同的棋盘对象而不同
        public CheckBroad(GameMode mode)
        {
            _initVertexDataM();
            _initAlig(mode);
        }
        public void ChangeMode(GameMode mode)
        {
            _initAlig(mode);
        }
        # region 游戏开始前的布阵功能 
        /// <summary>
        /// 将棋子布阵图转换为相应方的棋子数据。若布阵图尺寸错误,side无法识别则抛出异常。
        /// </summary>
        /// <param name="ctTable">ChessType的整数形式二维表，无棋子的地方用-2表示。</param>
        /// <param name="toSide"></param>
        /// <exception cref="ArgumentException"></exception>
        public static ChessInfo[] ConvertFromLayoutToCheInfo(int[,] ctTable, OfSide toSide)
        {
            int length = 5, width = 6;//布阵图长5，宽6
            if (ctTable==null)
                throw new ArgumentException("布阵图不能为空", "ctTable");
            if (ctTable.GetLength(0) != width || ctTable.GetLength(1) != length)
                throw new ArgumentException("布阵图尺寸错误。", "ctTable");
            if (!Enum.IsDefined(typeof(OfSide), toSide))
                throw new ArgumentException("side无法识别", "toSide");
            int x, y;//变换到的坐标值
            List<ChessInfo> ci = new List<ChessInfo>(25);
            switch (toSide)
            {
                case OfSide.First://[11,6]开始
                    x = 11;
                    for (int i = 0; i < width; i++, x++)
                    {
                        y = 6;
                        for (int j = 0; j < length; j++, y++)//合法尺寸内 布阵图中的每个位置
                        {
                            if (ctTable[i, j] != -2)
                                ci.Add(
                                    new ChessInfo(new Coord(x, y),
                                                  new Chess(toSide, (ChessType)ctTable[i, j]))
                                       );
                        }
                    }
                    break;
                case OfSide.Second://10,11
                    y = 11;
                    for (int i = 0; i < width; i++, y++)
                    {
                        x = 10; 
                        for (int j = 0; j < length; j++, x--)//合法尺寸内 布阵图中的每个棋子
                        {
                            if (ctTable[i, j] != -2)
                                ci.Add(
                                    new ChessInfo(new Coord(x, y),
                                                  new Chess(toSide, (ChessType)ctTable[i, j]))
                                       );
                        }
                    }
                    break;
                case OfSide.Third://5,10
                    x = 5;
                    for (int i = 0; i < width; i++, x--)
                    {
                        y = 10;
                        for (int j = 0; j < length; j++, y--)//合法尺寸内 布阵图中的每个棋子
                        {
                            if (ctTable[i, j] != -2)
                                ci.Add(
                                    new ChessInfo(new Coord(x, y),
                                                  new Chess(toSide, (ChessType)ctTable[i, j]))
                                       );
                        }
                    }
                    break;
                case OfSide.Fourth://6,5
                    y = 5;
                    for (int i = 0; i < width; i++, y--)
                    {
                        x = 6; 
                        for (int j = 0; j < length; j++, x++)//合法尺寸内 布阵图中的每个棋子
                        {
                            if (ctTable[i, j] != -2)
                                ci.Add(
                                    new ChessInfo(new Coord(x, y),
                                                  new Chess(toSide, (ChessType)ctTable[i, j]))
                                       );
                        }
                    }
                    break;

            }
            return ci.ToArray();
        }
        /// <summary>
        /// 将某方的棋子数据转化为布阵图。同时也检测棋子的布局合法性，不合法抛出异常。
        /// </summary>
        /// <param name="chessInfo"></param>
        /// <exception cref="ArgumentException">参数为null或棋子集合不符合规则</exception>
        /// <exception cref="GameRuleException"></exception>
        /// <returns>ChessType的整数形式二维表，无棋子的地方用-2表示。</returns>
        public static int[,] ConvertFromCheInfoToLayout(ChessInfo[] chessInfo)
        {
            if ( chessInfo == null)
                throw new ArgumentNullException("chessInfo","不能为null");
            //检测棋子是否是某方的所有棋子
            Chess[] che = new Chess[chessInfo.Length];
            OfSide side = chessInfo[0].chess.Side;
            if (!Enum.IsDefined(typeof(OfSide),side))
                throw new ArgumentException("棋子数据side无法识别", "chessInfo");
            for (int i = 0; i < che.Length; i++)
            {
                che[i] = chessInfo[i].chess;  
            }
            if (!Chess.IsOneSideAllChesses(che,side))
                throw new GameRuleException("棋子集合不符合规则,它们并非是某方的所有棋子集合");
            //至此棋子数据Chess、数量合法，开始转换。（坐标未必）
            ChessInfo[] cheArray = new ChessInfo[chessInfo.Length];
            chessInfo.CopyTo(cheArray, 0);
            //按坐标排列
            Array.Sort(cheArray, (che1, che2) => (che1.coord.i * n + che1.coord.j) - (che2.coord.i * n + che2.coord.j));
            int x, y;//当前棋子应与其相对的顶点坐标，总是从某方矩阵的左上角开始
            int length = 5,hight = 6 ;//布阵图长5，高6
            int[,] layout = new int[hight, length];
            Chess unknowChess = new Chess(side, ChessType.UnKnow);
            switch (side)
            {
                case OfSide.First://[11,6]开始,从左至右,从上到下
                    {
                        x = 11;
                        //k为棋子集合的索引,总共25枚棋子
                        int k = 0;
                        for (int i = 0; i < hight; i++, x++)
                        {
                            y = 6;
                            for (int j = 0; j < length; j++, y++)
                            {
                                if (IsLegalLoaction(unknowChess, new Coord(x, y)))//此位置可布阵
                                {
                                    if (cheArray[k].coord == new Coord(x, y) &&
                                        IsLegalLoaction(cheArray[k].chess, cheArray[k].coord))//此位置有棋子且布阵合法
                                    {
                                        layout[i, j] = (int)cheArray[k++].chess.Type;
                                    }
                                    else throw new GameRuleException("布阵图中的("+i+","+j+")位置无棋子可填充");
                                }
                                else layout[i, j] = -2;
                            }
                        }

                    }
                    break;
                case OfSide.Second://[6,11]开始,从左至右,从上到下
                    {
                        //k为棋子集合的索引,总共25枚棋子
                        int k = 0;
                        x = 6;
                        for (int j = length-1; j >= 0; j--, x++)
                        {
                             y = 11;
                            for (int i = 0; i < hight ; i++, y++)
                            {
                                if (IsLegalLoaction(unknowChess, new Coord(x, y)))//此位置可布阵
                                {
                                    if (cheArray[k].coord == new Coord(x, y) &&
                                        IsLegalLoaction(cheArray[k].chess, cheArray[k].coord))//此位置有棋子且布阵合法
                                    {
                                        
                                        layout[i, j] = (int)cheArray[k++].chess.Type;
                                    }
                                    else throw new GameRuleException("布阵图中的(" + i + "," + j + ")位置无棋子可填充");
                                }
                                else layout[i, j] = -2;
                            }
                        }
                    }
                    break;
                case OfSide.Third://[0,6]开始,从左至右,从上到下 5,10
                    {
                        x = 0; 
                        //k为棋子集合的索引,总共25枚棋子
                        int k = 0;
                        for (int i = hight-1; i >= 0; i--, x++)
                        {
                            y = 6;
                            for (int j = length-1; j >= 0; j--, y++)
                            {
                                if (IsLegalLoaction(unknowChess, new Coord(x, y)))//此位置可布阵
                                {
                                    if (cheArray[k].coord == new Coord(x, y) &&
                                        IsLegalLoaction(cheArray[k].chess, cheArray[k].coord))//此位置有棋子且布阵合法
                                    {
                                        
                                        layout[i, j] = (int)cheArray[k++].chess.Type;
                                    }
                                    else throw new GameRuleException("布阵图中的(" + i + "," + j + ")位置无棋子可填充");
                                }
                                else layout[i, j] = -2;
                            }
                        }
                    }
                    break;
                case OfSide.Fourth://[6，0]开始,从左至右,从上到下    6,5
                    {
                        x = 6; 
                        //k为棋子集合的索引,总共25枚棋子
                        int k = 0;
                        for (int j=0; j < length; j++, x++)
                        {
                            y = 0;
                            for (int i = hight-1; i >= 0; i--, y++)
                            {
                                if (IsLegalLoaction(unknowChess, new Coord(x, y)))//此位置可布阵
                                {
                                    if (cheArray[k].coord == new Coord(x, y) &&
                                        IsLegalLoaction(cheArray[k].chess, cheArray[k].coord))//此位置有棋子且布阵合法
                                    {
                                       
                                        layout[i, j] = (int)cheArray[k++].chess.Type;
                                    }
                                    else throw new GameRuleException("布阵图中的(" + i + "," + j + ")位置无棋子可填充");
                                }
                                else layout[i, j] = -2;
                            }
                        }
                    }
                    break;
            }
            return layout;

        }

        /// <summary>
        /// 布阵规则：判断在布阵中，某个棋子(明和暗)能否处于某个位置。
        /// </summary>
        /// <param name="che"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsLegalLoaction(Chess che, Coord c)
        {
            if (c.i < 0 || n <= c.i || c.j < 0 || n <= c.j)//坐标超出棋盘范围
                return false;
            if (VertexInfoM[c.i, c.j].Type == VertexType.None)//在空顶点上 
                return false;

            //棋子分别摆放在自己5×6矩阵范围内的23个兵站和两个大本营中
            if ((VertexInfoM[c.i, c.j].Side == che.Side) && //在自己阵营并
                     (VertexInfoM[c.i, c.j].Type == VertexType.BingZhan//在兵站
                      || VertexInfoM[c.i, c.j].Type == VertexType.DaBenYing))//或在大本营
            {
                //并且：炸弹不能放在一线，地雷只能放在最后两线，军棋只能放在大本营。
                switch (che.Type)
                {
                    case ChessType.Bomb:  //棋子是炸弹，
                        {
                            if (VertexInfoM[c.i, c.j].Place != Place.First)//不在一线
                                return true;
                            else return false;
                        }
                    case ChessType.DiLei://棋子是地雷,
                        {
                            if (VertexInfoM[c.i, c.j].Place == Place.Fourth  //在最后两线
                               || VertexInfoM[c.i, c.j].Place == Place.Sixth)
                                return true;
                            else return false;
                        }
                    case ChessType.JunQi: //棋子为军旗,
                        {
                            if (VertexInfoM[c.i, c.j].Type == VertexType.DaBenYing) //在大本营
                                return true;
                            else return false;
                        }
                    default: return true;//其他棋子（明棋也好，暗棋也好）
                }
            }
            return false;

        }

        //调换位于棋盘上的两个棋子（明）,正确调换返回true,
        //   非法或一个坐标上无棋子则不调换并返回false
        public bool ExchangeChess(Coord c1, Coord c2)
        {
            if ((VertexDataM[c1.i, c1.j].Chess==null) ||
                (VertexDataM[c2.i, c2.j].Chess==null)) //坐标上无棋子
                return false;
            if (IsLegalLoaction(VertexDataM[c1.i, c1.j].Chess, c2) &&
                IsLegalLoaction(VertexDataM[c2.i, c2.j].Chess, c1))//可以调换
            {
                //调换棋子
                Chess cTem = VertexDataM[c1.i, c1.j].Chess;
                VertexDataM[c1.i, c1.j].Chess = VertexDataM[c2.i, c2.j].Chess;
                VertexDataM[c2.i, c2.j].Chess = cTem;
                return true;
            }
            else return false;
        }

        /// <summary>
        /// 把棋子(暗棋和明棋)布阵(覆盖)到棋盘中。若有非法布阵的棋子则不布阵。
        /// 返回非法的棋子编号集合int[]，包括非法布阵的未知棋子。正确返回空数组
        /// </summary>
        /// <param name="ci"></param>
        /// <returns></returns>
        public int[] Layout(ChessInfo[] ci)
        {
            List<int> errorNum =new List<int>(ci.Length/2);
            for (int i = 0; i < ci.Length; i++)//每一个棋子
            {
                if (!IsLegalLoaction(ci[i].chess, ci[i].coord))//布阵非法
                    errorNum.Add(i);//则记录
            }
            if (errorNum.Count == 0)//所有棋子布阵都合法
            {
                foreach (var chessInfo in ci)//根据每一棋子信息
                {
                    VertexDataM[chessInfo.coord.i, chessInfo.coord.j].Chess = chessInfo.chess;//进行布阵
                }
            }
            return errorNum.ToArray();
        }

        public static int[,] GetUnknowLayout()
        {
            return new int[6, 5]{
                {-1,-1,-1,-1,-1},
                {-1,-2,-1,-2,-1},
                {-1,-1,-2,-1,-1},
                {-1,-2,-1,-2,-1},
                {-1,-1,-1,-1,-1},
                {-1,-1,-1,-1,-1}
            };
        }

        public static int[,] GetDefaultLayout()
        {

            return  new int[6, 5]{
                {40,39,38,38,37},
                {37,-2,36,-2,36},
                {35,35,-2,34,34},
                {34,-2,33,-2,33},
                {33,32,32,32,00},
                {00,41,41,31,41}
            };
        }
        #endregion 布阵

        #region 对局中要使用的功能
        //清空当前棋盘上的棋子，并恢复所有指定棋子到棋盘
        public void Recover(ChessInfo[] cIArarry)
        {
            //清空
            ClearAllChesses();
            //复位
            foreach (ChessInfo item in cIArarry)
            {
                VertexDataM[item.coord.i, item.coord.j].Chess = item.chess;
            }
        }
        /// <summary>
        /// 寻找顶点Coord上的棋子可到达的所有顶点及其路径信息，返回以起点为根节点的树。若找不到路径，返回包含根节点的路径树
        /// </summary>
        /// <param name="c"></param>
        /// <returns>
        /// 采用广度优先搜索算法（解决工兵的最短路径问题.
        /// 除地雷和位于大本营的棋子的其他棋子可以行走；
        /// 所有棋子行进不可逆转/回退。
        /// 若走铁路：
        /// 工兵可无阻挡地任意行进或与阻挡的敌方棋子对撞，
        /// 其他棋子只可沿直线或弧线无阻挡地行走或与阻挡的敌方棋子对撞。
        /// 若走公路：
        /// 所有棋子可无阻挡地行进一步或与阻挡的敌方棋子对撞，
        /// 对撞时依据棋子对撞规则判别双方棋子的去留”。
        /// </returns>
        /// <exception cref="GameRuleException">此棋子无法行走、此棋子没有可走的顶点</exception>
        /// <exception cref="ArgumentException">坐标超出棋盘大小、坐标没有棋子</exception>
        public PathTree<Coord> FindAllPaths(Coord c)
        {
            //顶点坐标错误
            if (0 > c.i || c.i >= n || 0 > c.j || c.j >= n) throw new ArgumentException("寻路错误：起点坐标异常");
            //顶点上无棋子
            if (VertexDataM[c.i, c.j].Chess==null) throw new ArgumentException("寻路错误：该顶点上无棋子");
            //不可行走的棋子
            if (VertexInfoM[c.i, c.j].Type == VertexType.DaBenYing ||
                VertexDataM[c.i, c.j].Chess.Type == ChessType.DiLei)
                throw new GameRuleException("此棋子无法行走");


            Queue<Coord> q = new Queue<Coord>();//待寻路的顶点的坐标队列
            PathTree<Coord> pathsTree = new PathTree<Coord>(new Coord(c.i, c.j));//路径信息
            Chess che = VertexDataM[c.i, c.j].Chess;//要寻路的棋子
            Coord CurrC;//当前进行寻路的顶点的坐标
            VertexDataM[c.i, c.j].status = VStatus.DISCOVERED; q.Enqueue(c);//初始化起点

            int x, y;
            while (q.Count != 0) //在q变空之前，不断
            {
                CurrC = q.Dequeue();//取出首顶点
                foreach (var edge in edges[CurrC.i * n + CurrC.j])//枚举相连的所有顶点vertexM[x, y]
                {
                    x = edge.y / n; y = edge.y % n;
                    if (VertexDataM[x, y].status == VStatus.UNDISCOVERED)//若该顶点未发现
                    {
                        //发现该顶点
                        VertexDataM[x, y].status = VStatus.DISCOVERED;
                        if (_canWalkFromToInAStep(che, VertexDataM[CurrC.i, CurrC.j], edge, new Coord(x,y)))//若该顶点可达
                        {
                            //设置该顶点路由能力以及记录发现的路径信息
                            _setVRoute(che, new Coord(CurrC.i, CurrC.j), edge, new Coord(x, y));
                            pathsTree.InsertAt((coord) =>
                                        {
                                            if (coord.i == CurrC.i && coord.j == CurrC.j)
                                                return true;
                                            else return false;
                                        }, new Coord(x, y));
                            //若终点还有路由能力，则到达该顶点还可继续行走
                            if (VertexDataM[x, y].route != Route.None)
                            {
                                q.Enqueue(new Coord(x, y));
                            }
                        }
                    }
                }
                //至此，当前顶点访问完毕
                VertexDataM[CurrC.i, CurrC.j].status = VStatus.VISITED;

            }
            _reset();
            if (pathsTree.Count==1)
                throw new GameRuleException("此棋子没有可走的顶点");
            return pathsTree;
        }
        //服务器端的行走判断，
        //异常：
        //  ArgumentException：参数异常：不是棋子坐标、坐标超出索引，包括FindAllPaths抛出的
        //  GameRuleException：非法移动,包括FindAllPaths抛出的
        public MoveInfo Move(Coord chessC, Coord endC)
        {
            PathTree<Coord> paths ;
            paths = FindAllPaths(chessC);
            Coord[] path = paths.FindPathOf((coord) =>
                                     {
                                         if (coord.i == endC.i && coord.j == endC.j)
                                             return true;
                                         else return false;
                                     });
            ChessInfo moveChessInfo = 
                new ChessInfo(chessC, VertexDataM[chessC.i, chessC.j].Chess);
            if (path.Length != 0)//若可以到达终点endC
            {
                //对棋子进行“前置”移动
                VertexDataM[chessC.i, chessC.j].Chess = null;

                if (VertexDataM[endC.i, endC.j].Chess==null)//而终点无棋子
                {
                    VertexDataM[endC.i, endC.j].Chess = moveChessInfo.chess;
                    return new MoveInfo()
                    {
                        MType = MoveResult.Normal,
                        MovingChess = moveChessInfo,
                        Path=path
                    };
                }
                else //而终点又有敌方棋子
                {
                    ChessInfo crashedChessInfo =new ChessInfo(endC, VertexDataM[endC.i, endC.j].Chess);
                    int x = moveChessInfo.chess - crashedChessInfo.chess;//获取对撞结果
                    //根据对撞结果，设置结果并返回
                    if (x > 0)//主动吃掉
                    {
                        VertexDataM[endC.i, endC.j].Chess = moveChessInfo.chess;
                        return new MoveInfo() {
                            MType= MoveResult.Eat,
                            MovingChess=moveChessInfo,
                            Path=path,
                            CrashedChess=crashedChessInfo
                        };
                    }
                    else if (x == 0)//同去
                    {
                        VertexDataM[endC.i, endC.j].Chess = null;
                        return new MoveInfo()
                        {
                            MType = MoveResult.TheSameTimeToDie,
                            MovingChess = moveChessInfo,
                            Path = path,
                            CrashedChess = crashedChessInfo
                        };
                    }
                    else //被吃
                    {
                        return new MoveInfo()
                        {
                            MType = MoveResult.BeEated,
                            MovingChess = moveChessInfo,
                            Path = path,
                            CrashedChess = crashedChessInfo
                        };
                    }
                }
            }
            else //非法移动
                throw new GameRuleException("非法的行棋请求：行棋路径不符合规则");
        }
        //根据行走结果，将棋子放置到相应位置。
        public void Move( SimpleMoveInfo mInfo)
        {
            Coord chessC = mInfo.StartC, endC = mInfo.EndC;
            switch (mInfo.MoveR)
            {
                case MoveResult.Normal:
                case MoveResult.Eat:
                    VertexDataM[endC.i, endC.j].Chess = VertexDataM[chessC.i, chessC.j].Chess;
                    VertexDataM[chessC.i, chessC.j].Chess = null;
                    break;
                case MoveResult.BeEated:
                    VertexDataM[chessC.i, chessC.j].Chess = null;
                    break;
                case MoveResult.TheSameTimeToDie:
                    VertexDataM[endC.i, endC.j].Chess = null;
                    VertexDataM[chessC.i, chessC.j].Chess = null;
                    break;
            }
        }
        //清空某方所有的棋子
        public ChessInfo[] ClearChessOf(OfSide side)
        {
            List<ChessInfo> ci = new List<ChessInfo>(25);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (VertexDataM[i, j].ExistChess())
                        if (VertexDataM[i, j].Chess.Side == side)
                        {
                            ci.Add(
                                new ChessInfo(new Coord(i, j),
                                              VertexDataM[i, j].Chess)
                                );
                            VertexDataM[i, j].Chess = null;
                        }
                }
            }
            return ci.ToArray();
        }


        //寻找所有军旗的位置,没找到返回0大小的数组
        public ChessInfo[] FindAllJunQi()
        {
            //8个大本营位置
            int[,] dabenyingC = new int[8, 2] {
                                    {0,7 },
                                    {0,9 },
                                    {7,0 },
                                    {9,0 },
                                    {7,16 },
                                    {7,16 },
                                    {16,7 },
                                    {16,9 }
                                    };
            List<ChessInfo> cList = new List<ChessInfo>(4);
            int x, y;//顶点坐标
            for (int i = 0; i < 8; i++)//遍历每个大本营
            {
                x = dabenyingC[i, 0]; y = dabenyingC[i, 1];
                if (VertexDataM[x, y].Chess != null)//且其上有棋子
                {
                    if (VertexDataM[x, y].Chess.Type == ChessType.JunQi)//且棋子是军旗
                        cList.Add(new ChessInfo(new Coord(x,y),
                                                new Chess(VertexDataM[x,y].Chess.Side,ChessType.JunQi)
                                               )
                                 );
                }
            }
            return cList.ToArray();

        }

        //寻找某方军旗的位置,没找到返回null
        public ChessInfo? FindJunQi(OfSide side)
        {
            ChessInfo[] junqiInfo = FindAllJunQi();
            foreach (var item in junqiInfo)
            {
                if (item.chess.Side==side)
                {
                    return item;
                }
            }
            return null;
        }

        #endregion 对局中要使用的功能


        /// <summary>
        /// 获取当前棋盘上的所有棋子
        /// </summary>
        /// <returns></returns>
        public ChessInfo[] GetCurrentChesses()
        {
            List<ChessInfo> ci = new List<ChessInfo>(100);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (VertexDataM[i, j].Chess!=null)
                    {
                        ci.Add(new ChessInfo(new Coord(i, j), VertexDataM[i, j].Chess));
                    }
                }
            }
            return ci.ToArray();
        }
        /// <summary>
        /// 获取当前棋盘上的某方所有棋子
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public ChessInfo[] GetCurrentChesses(OfSide side)
        {
            List<ChessInfo> ci = new List<ChessInfo>(100);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (VertexDataM[i, j].Chess != null && VertexDataM[i, j].Chess.Side==side)
                    {
                        ci.Add(new ChessInfo(new Coord(i, j), VertexDataM[i, j].Chess));
                    }
                }
            }
            return ci.ToArray();
        }
        //清空棋盘上的所有棋子
        public void ClearAllChesses()
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    VertexDataM[i, j].Chess = null;
                }
            }
        }
        #endregion 公开接口


        #region 内部实现
        private static List<EdgeNode>[] edges; //使用邻接表描述边信息 E(x,y) edges[x][???]
        private Alignment[] alignment;//索引0-4分别表示OfSide中各方的联盟。a[0]表示OfSide.First的联盟
        private const int n = 17;//棋盘顶点矩阵的宽度
        //初始化棋盘底座固有信息
        static CheckBroad()
        {
            _initVertexInfoM();
            _initEdges();

        }
        private static void _initVertexInfoM()
        {
            VertexInfoM = new VertexInfo[n, n];
            //顶点类型列表
            byte[,] typeTable = new byte[n, n]{
            {0,0,0,0,0,0,1,4,1,4,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,2,1,2,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,2,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,2,1,2,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //
            {1,1,1,1,1,1,3,0,3,0,3,1,1,1,1,1,1},
            {4,1,2,1,2,1,0,0,0,0,0,1,2,1,2,1,4},
            {1,1,1,2,1,1,3,0,3,0,3,1,1,2,1,1,1},
            {4,1,2,1,2,1,0,0,0,0,0,1,2,1,2,1,4},
            {1,1,1,1,1,1,3,0,3,0,3,1,1,1,1,1,1},
            //
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,2,1,2,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,2,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,2,1,2,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,4,1,4,1,0,0,0,0,0,0}
        };
            //顶点的位置信息表
            byte[,] placeTalbe = new byte[n, n]{
            {0,0,0,0,0,0,6,6,6,6,6,0,0,0,0,0,0},
            {0,0,0,0,0,0,5,5,5,5,5,0,0,0,0,0,0},
            {0,0,0,0,0,0,4,4,4,4,4,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            {0,0,0,0,0,0,2,2,2,2,2,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //
            {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            //
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,2,2,2,2,2,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            {0,0,0,0,0,0,4,4,4,4,4,0,0,0,0,0,0},
            {0,0,0,0,0,0,5,5,5,5,5,0,0,0,0,0,0},
            {0,0,0,0,0,0,6,6,6,6,6,0,0,0,0,0,0}
        };
            //顶点的所属势力表
            byte[,] ofSideTalbe = new byte[n, n]{
            {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //
            {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            //
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0}
        };
            //创建顶点添加到顶点集中
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    VertexInfoM[i, j] = new VertexInfo(
                       (VertexType)typeTable[i, j],
                       (Place)placeTalbe[i, j],
                       (OfSide)ofSideTalbe[i, j]);
                }
            }
        }
        //初始化顶点集合：构建棋盘中的顶点，无棋子
        private void _initVertexDataM()
        {
                VertexDataM = new VertexData[n, n];
            //    //顶点类型列表
            //    byte[,] typeTable = new byte[n, n]{
            //    {0,0,0,0,0,0,1,4,1,4,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,2,1,2,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,2,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,2,1,2,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    //
            //    {1,1,1,1,1,1,3,0,3,0,3,1,1,1,1,1,1},
            //    {4,1,2,1,2,1,0,0,0,0,0,1,2,1,2,1,4},
            //    {1,1,1,2,1,1,3,0,3,0,3,1,1,2,1,1,1},
            //    {4,1,2,1,2,1,0,0,0,0,0,1,2,1,2,1,4},
            //    {1,1,1,1,1,1,3,0,3,0,3,1,1,1,1,1,1},
            //    //
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,2,1,2,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,2,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,2,1,2,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,4,1,4,1,0,0,0,0,0,0}
            //};
            //    //顶点的位置信息表
            //    byte[,] placeTalbe = new byte[n, n]{
            //    {0,0,0,0,0,0,6,6,6,6,6,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,5,5,5,5,5,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,4,4,4,4,4,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,2,2,2,2,2,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    //
            //    {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            //    {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            //    {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            //    {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            //    {6,5,4,3,2,1,0,0,0,0,0,1,2,3,4,5,6},
            //    //
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,2,2,2,2,2,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,4,4,4,4,4,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,5,5,5,5,5,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,6,6,6,6,6,0,0,0,0,0,0}
            //};
            //    //顶点的所属势力表
            //    byte[,] ofSideTalbe = new byte[n, n]{
            //    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
            //    //
            //    {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            //    {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            //    {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            //    {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            //    {4,4,4,4,4,4,0,0,0,0,0,2,2,2,2,2,2},
            //    //
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
            //    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0}
            //};
            //创建顶点添加到顶点集中
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    //VertexDataM[i, j] = new VertexData(
                    //   (VertexType)typeTable[i, j],
                    //   (Place)placeTalbe[i, j],
                    //   (OfSide)ofSideTalbe[i, j]);
                    VertexDataM[i,j] = new VertexData();
                }
            }
        }
        //查找x与y之间的连边,返回edges数组中列表的索引，没有返回-1
        private static int _indexOfEdge(int x, int y)
        {
            for (int j = 0; j < edges[x].Count; j++)
            {
                if (edges[x][j].y == y) return j;
            }
            return -1;
        }
        //添加顶点x与y之间的连边，忽略自环边和已存在的边。返回邻接编号,忽略则返回-1
        private static int _addEdge(int x, int y, EdgeType t)
        {
            if (x == y) return -1;
            if (_indexOfEdge(x, y) != -1) return -1;
            //添加(x,y)(y,x)两条边
            //判定边的方向
            EdgeDirection d;
            int sub = Math.Abs(x - y);
            if (sub % n == 0) //垂直边
                d = EdgeDirection.Horizontal;
            else if (sub >= 1 && sub <= 2) //水平边
                d = EdgeDirection.Vertical;
            else //对角边
                d = EdgeDirection.Diagonal;

            edges[x].Add(new EdgeNode(y, t, d));
            edges[y].Add(new EdgeNode(x, t, d));
            return y;
        }
        private static void _initEdges()
        {
            edges = new List<EdgeNode>[n * n];
            for (int x = 0; x < n * n; x++)
            {
                edges[x] = new List<EdgeNode>();

            }
            //顶点的道路属性列表：VertexProOf
            byte[,] vertexRoadTable = new byte[n, n]{
                    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,1,1,1,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,1,1,1,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,1,1,1,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                    //
                    {1,3,3,3,3,3,2,0,2,0,2,3,3,3,3,3,1},
                    {1,3,1,1,1,3,0,0,0,0,0,3,1,1,1,3,1},
                    {1,3,1,1,1,3,2,0,2,0,2,3,1,1,1,3,1},
                    {1,3,1,1,1,3,0,0,0,0,0,3,1,1,1,3,1},
                    {1,3,3,3,3,3,2,0,2,0,2,3,3,3,3,3,1},
                    //
                    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,1,1,1,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,1,1,1,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,1,1,1,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,3,3,3,3,3,0,0,0,0,0,0},
                    {0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0}
                    };
            //选取两顶点并连接
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {//每一顶点
                    if (VertexInfoM[i, j].Type == VertexType.None) continue;//为空顶点

                    //顶点为行营：选取八个方向的顶点连接
                    if (VertexInfoM[i, j].Type == VertexType.XingYing)
                    {
                        for (int ii = i - 1; ii <= i + 1; ii++)
                        {
                            for (int jj = j - 1; jj <= j + 1; jj++)
                            {
                                _addEdge(i * n + j, ii * n + jj, EdgeType.HighWay);
                            }
                        }
                    }
                    //顶点为一线:检测八个方向，同为一线才连接
                    else if (VertexInfoM[i, j].Place == Place.First)
                    {
                        for (int ii = i - 1; ii <= i + 1; ii++)
                        {
                            for (int jj = j - 1; jj <= j + 1; jj++)
                            {
                                if (VertexInfoM[ii, jj].Type == VertexType.None) continue;//另一个顶点为空
                                if (VertexInfoM[ii, jj].Place == Place.First)
                                {//同为一线
                                    _addEdge(i * n + j, ii * n + jj, EdgeType.Railway);
                                }
                            }
                        }
                    }
                    //顶点为星宫：一定连接最近的四个方向
                    else if (VertexInfoM[i, j].Type == VertexType.XinGong)
                    {
                        for (int ii = i - 1; ii <= i + 1; ii++)
                        {
                            for (int jj = j - 1; jj <= j + 1; jj++)
                            {
                                if (i != ii && j != jj) continue;//另一个顶点在对角线方向
                                int x = i * n + j, y = ii * n + jj;
                                if (VertexInfoM[ii, jj].Type == VertexType.None)//顶点不相邻
                                {

                                    if (ii < i) y -= n;//上
                                    else if (ii > i) y += n;//下
                                    else if (jj < j) y -= 1;//左
                                    else if (jj > j) y += 1;//右
                                }
                                _addEdge(x, y, EdgeType.Railway);
                            }
                        }
                    }
                    //其他：四个方向有顶点则连接
                    else
                    {
                        for (int ii = i - 1; ii <= i + 1; ii++)
                        {
                            for (int jj = j - 1; jj <= j + 1; jj++)
                            {
                                if (ii < 0 || ii >= n) break;//ii越界
                                if (jj < 0 || jj >= n) continue;//jj越界

                                if (VertexInfoM[ii, jj].Type == VertexType.None) continue;//另一个顶点为空
                                if (i != ii && j != jj) continue;//另一个顶点在对角线方向
                                if (((HaveRoad)vertexRoadTable[i, j] & HaveRoad.Railway) != 0 &&
                                    ((HaveRoad)vertexRoadTable[ii, jj] & HaveRoad.Railway) != 0)  //两顶点都有铁路
                                    _addEdge(i * n + j, ii * n + jj, EdgeType.Railway);
                                else if (((HaveRoad)vertexRoadTable[i, j] & HaveRoad.HighWay) != 0 &&
                                    ((HaveRoad)vertexRoadTable[ii, jj] & HaveRoad.HighWay) != 0)  //两顶点都有公路
                                    _addEdge(i * n + j, ii * n + jj, EdgeType.HighWay);
                            }
                        }
                    }
                }
            }
        }

        //初始化联盟信息：根据游戏模式
        private void _initAlig(GameMode mode)
        {
            alignment = new Alignment[4];
            switch (mode)
            {
                case GameMode.Solo:
                    {
                        alignment[0] = Alignment.First;
                        alignment[1] = Alignment.First;
                        alignment[2] = Alignment.Second;
                        alignment[3] = Alignment.Second;
                    }; break;
                case GameMode.SiMing:
                case GameMode.ShuangMing:
                case GameMode.SiAn:
                default:
                    {
                        alignment[0] = Alignment.First;
                        alignment[1] = Alignment.Second;
                        alignment[2] = Alignment.First;
                        alignment[3] = Alignment.Second;
                    }; break;
            }
        }
        //复位与棋盘寻路状态有关的信息
        private void _reset()
        {
            //顶点复位
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)//每一顶点
                {
                    VertexDataM[i, j].route = Route.All;
                    VertexDataM[i, j].status = VStatus.UNDISCOVERED;
                }
            }


        }
       

        
        //棋子che经过相连边edge可以从起始顶点到达终顶点吗
        //前提：边edge连接的是起始与终顶点
        private bool _canWalkFromToInAStep(Chess che, VertexData startV, EdgeNode edge, Coord endVC)
        {
            //有到此顶点的行走能力；
            if (startV.CanWalkEdge(edge))
            {
                VertexData endDV = VertexDataM[endVC.i,endVC.j];
                //终顶点若有棋子：
                if (endDV.ExistChess())
                {
                    //该棋子不是同一联盟 并且 该顶点不是行营。
                    if ((alignment[(int)che.Side - 1] != alignment[(int)endDV.Chess.Side - 1]) &&
                        (VertexInfoM[endVC.i, endVC.j].Type != VertexType.XingYing))
                        return true;
                    //
                    else return false;
                }
                //终顶点若无棋子：
                else return true;
            }
            else return false;
        }

        //根据棋子从起始顶点经过边edge到达终顶点后，设置棋子在该终顶点的行走(路由)能力
        //前提：边edge连接的是起始与终顶点
        private void _setVRoute(Chess che, Coord startVC, EdgeNode edge, Coord endVC)
        {
            Route power = Route.None;
            //若走公路，则到终点后再无能力行走
            if (edge.type == EdgeType.HighWay)
            {
                power = Route.None;
            }
            //若走铁路
            else
            {
                //若到达的终点有棋子
                if (VertexDataM[endVC.i, endVC.j].Chess!=null)
                    power = Route.None;
                //终点无棋子，起始顶点的棋子是工兵
                else if (che.Type == ChessType.GongBing)
                    power = Route.All ^ Route.Highway;
                //终点无棋子，起始顶点的棋子不是工兵
                else
                {
                    VertexInfo startIV = VertexInfoM[startVC.i, startVC.j];
                    VertexData startDV= VertexDataM[startVC.i, startVC.j];
                    if (startIV.Place == Place.Seconed //起点是二线且终点是一线
                        && VertexInfoM[endVC.i, endVC.j].Place == Place.First)
                        power = Route.Diagonal | VertexData.ExchangeETOR(edge);
                    else if (startIV.Place == Place.First //起点是一线且终点是另一个一线
                              && VertexInfoM[endVC.i, endVC.j].Place == Place.First)
                    {
                        if((startDV.route & Route.All) != 0)//棋子是从此点开始走的
                        {
                            if(startIV.Side== OfSide.First|| startIV.Side==OfSide.Third)
                                power = Route.Vertical;
                            else power = Route.Horizontal; 
                        }
                        else if ((startDV.route & Route.Vertical) !=0)//水平
                            power = Route.Horizontal;
                        //垂直
                        else power = Route.Vertical;
                    }
                    else power = VertexData.ExchangeETOR(edge);
                }
            }
            VertexDataM[endVC.i, endVC.j].SetRoute(power);
        }
        #endregion 内部实现

        //用于测试棋盘的数据结构的正确性   通过！
        //发布时可去除
        public string ToStrings()
        {
            char[] vertexmap = new char[] { ' ', '☐', '⊙', '✦', '♖' };
            //char[] vertexmap = new char[] { ' ', '1', '2', '3', '4' };
            char[] edgemap = new char[] { 'H', 'R' };
            //二维字符数组[2n-1,2n-1]
            char[,] chs = new char[2 * n - 1, 2 * n - 1];
            //填充顶点信息，初始化边信息
            for (int i = 0; i < 2 * n - 1; i++)
            {
                for (int j = 0; j < 2 * n - 1; j++)
                {
                    if (i % 2 == 0 && j % 2 == 0)//若表示的是顶点
                        chs[i, j] = vertexmap[(int)VertexInfoM[i / 2, j / 2].Type];
                    else //是边
                        chs[i, j] = ' ';
                }
            }
            //填写边信息
            for (int x = 0; x < n * n; x++)
            {

                for (int i = 0; i < edges[x].Count; i++) //顶点若存在边,依次将边信息写入chs
                {
                    int y = edges[x][i].y;
                    chs[x % n + y % n, x / n + y / n] = edgemap[(int)edges[x][i].type];
                }

            }
            //格式化
            //char[] cha = new char[2 * n * (2 * n - 1)];
            //for (int i = 0; i < 2 * n - 1; i++)
            //{
            //    for (int j = 0; j < 2 * n - 1; j++)
            //    {
            //        cha[i * 2 * n + j] = chs[i, j];
            //    }
            //    cha[i * 2 * n + 2 * n - 1] = '\n';
            //}

            //return new string(cha);
            return ExchangeToString.Array(chs,item =>item.ToString()
                                                );
        }

        /// <summary>
        /// 坐标变换
        /// </summary>
        /// <param name="c"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        //public Coord exchenge(Coord c,OfSide side)
        //{

        //}
    }

}