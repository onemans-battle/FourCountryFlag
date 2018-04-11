using GameLogic;
using Server;
namespace NetConnector.MsgType
{
    /* 网络组件
     * 负责编码发送和接收解码在MsgType.cs中定义的结构体消息，将网络二进制数据打包成消息结构体通过激发事件通知给上层。
     * 网络发生错误也将激发网络错误事件。

     * 总之，它只负责××编解码××以下定义为结构体的数据，（并有限地确保连接不被窃听、伪装？？？：未实现）
     * 作用是让上层忽略网络流的编码复杂性，忽略网络连接的细节，忽略传输信息的不完整性
     * 
     */


     //此文件中的结构体定义了所有网络信息的种类,便于查看和编程

    #region 登录模块
    /// <summary>
    /// 1.登录的请求
    /// </summary>
    public struct LoginIn
    {
        public string UserName;
        public string Password;
    }
    /// <summary>
    /// 1.登录的响应
    /// </summary>
    public struct LoginInfo
    {
        public bool IsLogin;
        public ulong PlayerID;
        public string Info;//登录的详细信息:密码错误或账号错误等
    }
    #endregion
    #region 玩家信息获取模块
    /// <summary>
    /// 2.获取玩家详细信息的请求
    /// </summary>
    public struct GetPlayerInfo
    {
        public ulong PlayerID;
    }
    /// <summary>
    /// 2.获取玩家详细信息的响应
    /// </summary>
    public struct PlayerInfo
    {
        public ulong PlayerID;
        public string NickName;
        public ushort Win;
        public ushort Fail;
        public ushort Escape;
        public ushort Score;
        public string Grade;
        //public byte[] image;
    }

    #endregion

    #region 匹配模块
    //匹配
    public struct Match
    {
        public GameMode GameMode;
    }
    public struct MatchInfo
    {
        public bool HasAGame;//是否匹配到对局了
        public bool HasCancel;//是否被服务器取消了
        public ulong RoomID; //0-18446744073709551615; 未匹配到则为0
        public GameMode GameMode;
        public Room.PlayerInfo[] PlayerInfo;
    }
    public struct CancelMatch
    {
        
    }
    public struct CancelMatchInfo
    {
        public bool IsCancel;
        public string Info;
    }
    #endregion

    #region 房间模块
    //房间模块接收的无应答请求(通过广播获得结果)，所有与房间内有关的请求，都包含请求头RoomID
    /// <summary>
    /// 请求类型有：进入房间、退出房间、准备、取消准备、行棋、跳过行棋、投降、
    /// 请求和棋、同意和棋、拒绝和棋、请求游戏信息
    /// </summary>
    public enum RoomReqType
    {
        Enter = 1,
        Exit,
        Ready,
        CReady,
        Move,
        Skip,
        Surr,
        ODraw,
        Adraw,
        RDraw,
        GetInfo
    }
    public struct Enter
    {
        public ulong RoomID;
    }
    public struct Exit
    {
        public ulong RoomID;
    }
    public struct Ready
    {
        public ulong RoomID;
        public int[,] CheLayout;
    }
    public struct CReady
    {
        public ulong RoomID;
    }
    public struct Move
    {
        public ulong RoomID;
        public Coord ChessCoord;//棋子信息
        public Coord TargetCoord;//目标顶点的坐标
    }
    public struct Skip
    {
        public ulong RoomID;
    }
    public struct Surr
    {
        public ulong RoomID;
    }
    public struct ODraw
    {
        public ulong RoomID;
    }
    public struct ADraw
    {
        public ulong RoomID;
    }
    public struct RDraw
    {
        public ulong RoomID;
    }
    public struct GetInfo
    {
        public ulong RoomID;
    }
    //房间中的广播,都包含RoomID
    /// <summary>
    /// 20个事件
    /// </summary>
    public enum RoomBroType
    {
        PEnter=1,
        PExit,
        PForceExit,
        PReady,
        PCReady,
        PMove,
        PDie,
        PSkip,
        PSurr,
        PODraw,
        PAdraw,
        PRDraw,
        ExpireDraw,
        PSiLingDied,
        PLostC,//玩家掉线
        PReC,//玩家重连
        SideNext,

        GameStart,
        GameOver,
        GameClose,
    }
    public struct PEnter
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct PExit
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct PForceExit
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct PReady
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
        public int[,] CheLayout;
    }
    public struct PMove
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
        public SimpleMoveInfo SMInfo;
    }
    public struct PDie
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
        public Room.PlayerInfo Player;
    }
    public struct PSkip
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct PSurr
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
        public ushort Steps;
    }
    public struct PODraw
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct PADraw
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct PRDraw
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
        public DrawRecord DRecord;
    }
    public struct ExpireDraw
    {
        public ulong RoomID;
        public DrawRecord DRecord;
    }
    public struct PSiLingDied
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
        public ChessInfo[] chessInfo;
    }
    public struct PLostC
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct PReC
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct SideNext
    {
        public ulong RoomID;
        public ushort Steps;
        public Room.PlayerInfo PlayerInfo;
    }
    public struct GameLayouting
    {
        public ulong RoomID;
    }
    public struct GameStart
    {
        public ulong RoomID;
        public System.Collections.Generic.Dictionary<OfSide, int[,]> LayoutDic;
    }
    public struct GameOver
    {
        public ulong RoomID;
        public GameResult GResult;
    }
    public struct GameClose
    {
        public ulong RoomID;
    }

    //有关房间中的请求、应答
    /// <summary>
    /// 请求游戏信息：用于断线重连或异常时恢复
    /// </summary>
    public struct GetGInfo
    {
        public ulong RoomID;
    }
    /// <summary>
    /// 玩家列表和当前棋盘信息
    /// </summary>
    public struct RGInfo
    {
        public ulong RoomID;
        public Room.PlayerInfo PlayerInfo;
        public GameMode GMode;
        public GameStatus GStatus;
        public ushort Step;//0-65535
        /// <summary>
        /// （CurrentChessesInfo）:当前游戏棋盘上所有棋子的信息
        /// </summary>
        public ChessInfo[] CChessesInfo;
    }

    #endregion

    //请求非法时返回
    public struct GetquestError
    {
        /// <summary>
        /// 101:账号未登录
        /// </summary>
        public ushort Code;
        public string ClientMsgType;
        public string ErrorInfo;
    }
    //服务器的发送：如客户端满载
    public struct ServerError
    {
        public string ErrorInfo;
    }


}
