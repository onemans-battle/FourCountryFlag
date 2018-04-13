using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using DataStruct;
namespace GameLogic
{
    /* 服务器端的一场对局总管理，它是 一场游戏 从开始到结束 的所有游戏规则（游戏进程）呈现。
     * 也就是说，它不关心除一局游戏进行以外的事物：账号认证、游戏匹配、记录此场游戏的ID等。
     * 对它的操作如果不符合游戏规则，将引发游戏规则异常：GameRuleException和GameManagerException
     * 另外还有参数异常
     * 
     * 未实现的功能：玩家掉线、玩家重连、玩家获取复盘
     */
    //游戏状态
    public enum GameStatus
    {
        Layouting,  //调整布局中，开始前
        Doing,      //游戏进行中
        Over,       //游戏结束
        Closed //游戏关闭
    }
    //角色（某方）的游戏状态
    public enum PlayerStatus
    {
        UnReady,
        Ready,
        Alive,//解释为活动的更为恰当，因为失去连接时会覆盖此枚举值，而玩家可能依然是存活的
        Death,
        Surrendered,
        ForceEixted,//该玩家未结束游戏，强制退出
        Eixted,//玩家死亡后正常退出
        LostConnection//玩家失去连接,超时将视为强退
    }
    //把游戏中有关玩家的信息组织为一个“结构体”，易于管理，清晰明确。投入使用
    //在游戏结束和游戏开始之前离开的玩家没有此信息数据，没有坐在哪个位置的信息，位置信息由对局管理类来保存
    //使用类单纯是为了从字典中取出时能够修改此内的值，我觉得使用类而不是结构体来单纯存储这样小的数据 有些违反了类的意义，类是一个对象不是，可它在这仅是玩家的数据，对象的数据。
    public class SideInfo
    {
        public OfSide Side;
        public PlayerStatus Status;//角色状态
        public ushort OfferDrawNum;//已提出和棋的次数
        public ushort SkipNum;//跳过的回合次数
        public SideInfo(OfSide side,PlayerStatus status=PlayerStatus.UnReady,ushort drawNum=0,ushort skipNum=0)
        {
            Side = side;
            Status = status;
            OfferDrawNum = drawNum;
            SkipNum = skipNum;
        }
    }

    public struct GameResult
    {
        public readonly bool IsDraw;
        public readonly OfSide[] Winers;//无为空数组
        public readonly OfSide[] Losers;//
        public readonly OfSide[] ForceEixte;//强退玩家，
        DrawRecord DrawRecord;//无则为null
        public GameResult(bool isDraw, OfSide[] winers, OfSide[] losers, OfSide[] forceEixte, DrawRecord drawRecord=null)
        {
            IsDraw = isDraw;
            Winers = winers;
            Losers = losers;
            ForceEixte = forceEixte;
            DrawRecord = drawRecord;
        }
    }
    //专用于网络传送的移动信息
    public struct SimpleMoveInfo
    {
        public ushort Steps;
        public OfSide Side;
        public MoveResult MoveR;//
        public Coord StartC;//起点
        public Coord EndC;//终点
        public SimpleMoveInfo(ushort steps, OfSide side,MoveResult moveR, Coord startC, Coord endC)
        {
            Steps=steps;
            Side=side;
            MoveR = moveR;//
            StartC= startC;//起点
            EndC= endC;//终点
        }
    }

    public struct ChessMoveRecord
    {
        public readonly MoveInfo MoveInfo;
        public readonly ushort Steps;
        public ChessMoveRecord(ushort steps, MoveInfo moveInfo)
        {
            MoveInfo = moveInfo;
            Steps = steps;
        }
    }
    public class DrawRecord
    {
        public ushort Steps;
        public OfSide OfferSide;//提出和棋的玩家
        public Dictionary<OfSide,bool> RigthSideAgree;//有权利投票的玩家，开始投票时都是false值,通过判断其对应的IsDoDraw值来判别是否已经进行了投票
        public Dictionary<OfSide, bool> IsDoDraw;
        public DrawRecord(ushort steps,OfSide offerSide)
        {
            Steps = steps;
            OfferSide = offerSide;
            RigthSideAgree = new Dictionary<OfSide, bool>();
            IsDoDraw = new Dictionary<OfSide, bool>();
        }
    }
    public struct SkipMoveRecord
    {
        public ushort Steps;
        public OfSide OfferSide;//跳过行棋的玩家
        public SkipMoveRecord(ushort steps,OfSide offerSide)
        {
            Steps = steps;
            OfferSide = offerSide;
        }
    }
    public class SurrenderEventArgs : EventArgs
    {
        public readonly ushort Steps;
        public readonly OfSide Side;
        public readonly ChessInfo[] ChessInfo;
        public SurrenderEventArgs(ushort steps, OfSide side, ChessInfo[] chessInfo)
        {
            Steps=steps;
            Side = side;
            ChessInfo = chessInfo;
        }
    }
    public class PlayerRefuseDrawEventArgs : EventArgs
    {
        public OfSide Side;
        public DrawRecord DrawRecord;
        public PlayerRefuseDrawEventArgs(OfSide side,DrawRecord drawRecord)
        {
            Side = side;
            DrawRecord = drawRecord;
        }
    }
    public class PlayerForceExitEventArgs: EventArgs
    {
        public readonly ushort Steps;
        public readonly OfSide Side;
        public readonly ChessInfo[] ChessInfo;
        public PlayerForceExitEventArgs(ushort steps, OfSide side, ChessInfo[] chessInfo)
        {
            Steps = steps;
            Side = side;
            ChessInfo = chessInfo;
        }
    }
    public class PlayerReadyEventArgs : EventArgs
    {
        public OfSide Side;
        public int[,] CheLayout;

        public PlayerReadyEventArgs(OfSide side, int[,] cheLayout)
        {
            Side = side;
            CheLayout = cheLayout;
        }
    }
    public delegate void GameEventHandler(object obj);
    /*游戏进程的管理者：操控棋盘，管理游戏进度，不参与实现棋盘。一盘游戏所有的操作和发生的事件都在此进行。
     * 
     * 对游戏的操作(方法)有：设置或改变游戏模式、玩家进入游戏、玩家退出游戏、准备、取消准备、行棋、投降、
     *                     请求和棋或同意和棋、彻底关闭游戏、玩家想获取此盘游戏的所有相关信息、获取复盘。
     * 未实现的扩展：暂停游戏、继续游戏进程。另外针对网络的特殊情况：玩家掉线、玩家重登恢复游戏。
     *      
     * 游戏会发生的事情（事件）有：有玩家进入或退出游戏（正常或强制）、有玩家准备或取消准备、游戏开始了、玩家行棋了、
     *          有玩家投降、有玩家请求和棋、有玩家同意和棋、玩家的司令死亡有军旗军棋亮起、玩家的军旗被吃并失败了、游戏结束
     *          玩家掉线了、玩家重连了
     *          
     * 异常：参数错误、以及在异常文件中定义的前三种
     */
    public class GameManager
    {
        public GameMode Mode { get { return _mode; }
            set {
                if (_mode == value) return;
                if (Status == GameStatus.Layouting)//允许更改
                {
                        _mode = value;
                        _checkbroad.ChangeMode(value);
                        _updateGameStatus();
                }
                else throw new GameRuleException("当前不可修改游戏模式：因为游戏已开始或结束或关闭");
            } }//游戏模式
        /*游戏状态、进程:产生变化(除变为布阵状态外)时，它只关心和设置游戏进入此状态后需要的数据和信息,不考虑是如何造成游戏状态改变的，如设置游戏结果等
        * 激发相应的游戏开始、结束事件。请在游戏结束事件内获取游戏结果和复盘*/
        public GameStatus Status
        {
            private set {
                
                _status = value;
                if (value == GameStatus.Layouting)//进入布阵态
                {
                    //刷新所有在场玩家的状态
                    _PlayersInfo.Map((side, dic) => dic[side].Status = PlayerStatus.UnReady
                        );
                    AllChesesLaout.Clear();
                    GameLayouting?.Invoke(this,EventArgs.Empty);
                }
                else if (value == GameStatus.Doing)//进入游戏态
                {//初始化与游戏开始相关的必要数据
                    //_allChessesInfo = _checkbroad.GetCurrentChesses();
                    Steps = 0;
                    //更新玩家状态、刷新已请求和棋与跳过行棋的次数
                    _PlayersInfo.Map((side, dic) => {
                        SideInfo playerInfo = dic[side];
                        playerInfo.Status = PlayerStatus.Alive;//更新玩家状态
                        playerInfo.OfferDrawNum=0;//刷新可请求和棋的次数
                        playerInfo.SkipNum = 0;//刷新跳过行棋的次数
                    });
                    //创建行棋定时器
                    _moveTimer = new System.Timers.Timer(ChessMoveInterval) { AutoReset = false };
                    _moveTimer.Elapsed += (sender, elapsedEventArgs) => {
                        _PlayersInfo[NowCanMoveSide].SkipNum+=1;//记录此次跳过行棋
                        Steps++;
                        PlayerMoveSkip?.Invoke(this,NowCanMoveSide);
                        if (_PlayersInfo[NowCanMoveSide].SkipNum>=SkipMaxNum)
                        {
                            _PlayersInfo[NowCanMoveSide].Status = PlayerStatus.Death;
                            _updateGameStatus();
                            if(Status==GameStatus.Doing) _updateNextSide();
                        }
                        else _updateNextSide();//超时将自动将行棋权利移交给下一方
                    };
                    //通知
                    GameStart?.Invoke(this, AllChesesLaout);
                    _updateNextSide();
                }
                else if(value == GameStatus.Over)//游戏结束了
                {
                    _moveTimer.Close();
                    _moveTimer = null;
                    if (_drawTimer!=null)
                    {
                        _drawTimer.Close();
                        _drawTimer = null; 
                    }
                    _checkbroad.ClearAllChesses();
                    GameOver?.Invoke(this, _gameResult);
                    //清理此盘的游戏记录
                    _chessMoveRecords.Clear();
                    _drawRecords.Clear();
                    //清理已离开的玩家
                    _PlayersInfo.Map((side, dic) => {
                        PlayerStatus playerStatus = dic[side].Status;
                        if (playerStatus ==PlayerStatus.ForceEixted||
                            playerStatus==PlayerStatus.Eixted||
                            playerStatus==PlayerStatus.LostConnection)
                            dic.Remove(side);
                    });
                    if (_autoRestart) Status = GameStatus.Layouting;
                }
                else if (value == GameStatus.Closed)//游戏关闭了
                {
                    GameClose?.Invoke(this, EventArgs.Empty);
                }
            }
            get { return _status; }
        }
        public int PlayerNumber { get {
                return _PlayersInfo.Count;
            } }
        public ushort Steps { get; private set; }//当前行走过的总步数
        public OfSide NowCanMoveSide  //当前轮到的行棋方,每当改变为不同值时，激发SideNext事件
        {        
            get { return _side; }
            private set {
                //参数超出枚举值检测？不需要，因为对其的设置都在内部进行。
                _side = value;
                _moveTimer.Stop();//重置定时器
                _moveTimer.Start();
                SideNext?.Invoke(this,value);
            }
        }
        System.Timers.Timer _moveTimer;//行棋定时器

        public bool _autoRestart;//游戏结束后自动重新开始

        public const ushort FourPlayerMaxSteps = 500;//四人游戏中最大步数，大于则和棋
        public const ushort TwoPlayerMaxSteps = 250;//二人游戏中最大步数，大于则和棋
        public const ushort ChessMoveInterval = 30*1000;//每方的行棋时间，单位为毫秒
        public const ushort DrawInterval = 30 * 1000;//和棋最长持续时间，单位为毫秒
        public const int    LostInterval =5* 60 * 1000;//掉线最长持续时间，单位为毫秒，超过自动投降
        public const ushort LostConnectionInterval = 15 * 1000;//断线最长持续时间，单位为毫秒
        public const ushort OfferDrwaMaxNum = 3;//每个玩家最大的请求和棋次数
        public const ushort SkipMaxNum = 5;//每个玩家跳过自己行棋的次数，超时也算在内

        //事件
        //在方法体内直接激发的事件：
        //public event EventHandler<OfSide> PlayerEnter;                  //玩家进场：不会导致游戏进程变化，因此废弃
        public event EventHandler<OfSide> PlayerExit;                   //玩家退出
        public event EventHandler<PlayerForceExitEventArgs> PlayerForceExit;//玩家强制退出
        public event EventHandler<PlayerReadyEventArgs> PlayerReady;    //玩家准备
        public event EventHandler<OfSide> PlayerCancelReady;            //玩家取消准备
        public event EventHandler<SimpleMoveInfo> PlayerChessMove;      //某个棋子移动了
        public event EventHandler<OfSide> PlayerMoveSkip;               //玩家跳过行棋
        public event EventHandler<OfSide> PlayerDie;                    //玩家死亡
        public event EventHandler<SurrenderEventArgs> PlayerSurrender;  //玩家投降
        public event EventHandler<OfSide> PlayerOfferDraw;              //玩家请求和棋
        public event EventHandler<OfSide> PlayerAgrreDraw;              //某玩家同意和棋
        public event EventHandler<PlayerRefuseDrawEventArgs> PlayerRefuseDraw;         //某玩家拒绝和棋
        public event EventHandler<DrawRecord> ExpireDraw;               //和棋超时
        public event EventHandler<ChessInfo[]> PlayerSiLingDied;        //某方的司令死亡
        public event EventHandler<OfSide> PlayerLost;                   //玩家掉线
        public event EventHandler<OfSide> PlayerReconnect;              //玩家重连
        public event EventHandler GameClose;                            //游戏关闭:释放此游戏管理者时激发
        //通过属性设置器间接激发的事件：
        public event EventHandler GameLayouting;
        public event EventHandler<Dictionary<OfSide, int[,]>> GameStart;            //开始游戏了，事件数据为布阵字典
        public event EventHandler<GameResult> GameOver; //游戏结束了
        public event EventHandler<OfSide> SideNext;     //轮到某方行棋了
        

        private static readonly OfSide[] _playerMoveOrder //默认的行棋顺序
            = new OfSide[4] { OfSide.First, OfSide.Second, OfSide.Third, OfSide.Fourth };

        //以下为3个属性的内置字段
        private GameStatus  _status;//游戏状态、进程
        private OfSide      _side; //当前轮到的行棋方
        private GameMode    _mode;
        /// <summary>
        /// 各方的玩家信息。
        /// 玩家进入游戏时添加。在游戏开始前和结束后退出时剔除。
        /// </summary>
        private Dictionary<OfSide, SideInfo> _PlayersInfo;
        public SideInfo[] PlayersInfo { get {
                SideInfo[] sideInfos=new SideInfo[_PlayersInfo.Count];
                _PlayersInfo.Values.CopyTo(sideInfos, 0);
                return sideInfos;
            } }
        private DrawRecord  _playerDrawRecord;//最近一次的玩家的和棋记载
        System.Timers.Timer _drawTimer;//和棋请求定时器
        
        //旁观者字典？？扩展！未实现

        public CheckBroad _checkbroad; //棋盘。
        public Dictionary<OfSide,int[,]> AllChesesLaout;//布阵信息,进入布阵态时会清空
        private GameResult _gameResult;//游戏结果
        private Queue<ChessMoveRecord> _chessMoveRecords;//行棋记录
        private Queue<DrawRecord> _drawRecords;//和棋记录
        
        /// <summary>
        /// 初始化游戏管理者
        /// </summary>
        /// <param name="mode"></param>
        public GameManager(GameMode mode=GameMode.SiAn, bool autoRestart=true)
        {
            if (!Enum.IsDefined(typeof(GameMode), mode))
                throw new ArgumentOutOfRangeException("mode","不支持的游戏模式");

            _PlayersInfo = new Dictionary<OfSide, SideInfo>(4);
            AllChesesLaout = new Dictionary<OfSide, int[,]>(4);

            if (mode==GameMode.Solo)
                _chessMoveRecords = new Queue<ChessMoveRecord>(TwoPlayerMaxSteps / 4);
            else
                _chessMoveRecords = new Queue<ChessMoveRecord>(FourPlayerMaxSteps / 4);
            _drawRecords = new Queue<DrawRecord>(4);
            
            _checkbroad = new CheckBroad(Mode);
            Mode = mode;
            _autoRestart = autoRestart;
        }
        public void StartUp()
        {
            //启动游戏
            Status = GameStatus.Layouting;
        }
        public void RestartGame()
        {
            if (Status==GameStatus.Doing)
            {
                Status = GameStatus.Over;
            }
            Status = GameStatus.Layouting;//重新开始一盘游戏
        }
        /// <summary>
        /// 修改游戏模式
        /// </summary>
        /// <param name="mode">游戏模式</param>
        public void ChangeGameMode(GameMode mode)
        {
            if (!Enum.IsDefined(typeof(GameMode), mode))
                throw new ArgumentOutOfRangeException("mode", "不支持的游戏模式");
            Mode = mode;
        }

        //14种游戏操作：

        /// <summary>
        /// 玩家进场。
        /// 游戏在布阵态且该模式下人数未满时，允许玩家进入符合游戏模式的位置。
        /// 不允许在游戏中退出、强退的玩家再次进入游戏，请通过重连（掉线的玩家）来达到此目的
        /// </summary>
        /// <param name="side">游戏进场的势力方</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="GameManagerException"></exception>
        public void Enter(OfSide side)
        {
            if (!Enum.IsDefined(typeof(OfSide), side))
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if (_status!=GameStatus.Layouting)//游戏不处于布阵态
                throw new GameManagerException("玩家进场", "非法的玩家进场请求：游戏进程不允许");
            if (_PlayersInfo.ContainsKey(side))//座位有玩家了
                throw new GameManagerException("玩家进场", "非法的玩家进场请求：此座位已有玩家");
            if (Mode==GameMode.Solo )//并且游戏模式为单挑
            {
                if (_PlayersInfo.Count == 1) //游戏现在只有一人在座位
                {
                    
                    OfSide existSide = _PlayersInfo[0].Side;
                    int abs = Math.Abs(existSide - side);
                    if (abs != 2)//但不是坐进对面
                        throw new GameManagerException("玩家进场", "非法的玩家进场请求：当前游戏模式为单挑，只能坐进当前玩家的对面");
                }
                else if(_PlayersInfo.Count>1)//人数有两个及以上：当人数在3、4人时更改游戏模式可能出现人数超出2人的情况（未加以处理）
                    throw new GameManagerException("玩家进场", "非法的玩家进场请求：当前游戏模式为单挑，人数已满。可通过改更游戏模式来允许更多人进入");
               
            }
            else if(_PlayersInfo.Count>=4)//四人游戏模式下人数已满
                throw new GameManagerException("玩家进场", "非法的玩家进场请求：当前游戏人数已满。");
            
            //至此允许玩家进场
            _PlayersInfo.Add(side, new SideInfo(side));
                //PlayerEnter?.Invoke(this, side);
        }
        /// <summary>
        /// 随机进场
        /// </summary>
        /// <returns></returns>
        /// <exception cref="GameManagerException"></exception>
        /// <remarks>游戏人数为0时，默认坐在第一个位置。
        /// 否则，单挑模式下坐进对面，四人游戏模式下逆时针坐上座位</remarks>
        /// <exception cref="GameManagerException"></exception>
        public OfSide Enter()
        {
            if (_status != GameStatus.Layouting)//游戏不处于布阵态
                throw new GameManagerException("玩家进场", "非法的玩家进场请求：游戏进程不允许");
            OfSide side=OfSide.First;
            if (_PlayersInfo.Count!=0)//游戏人数有一人及以上
            {
                //两人游戏只能坐进对面
                if (Mode == GameMode.Solo)
                {
                    if (_PlayersInfo.Count == 1) //游戏现在只有一人在座位
                    {
                        OfSide existSide= OfSide.First;
                        foreach (var item in _PlayersInfo)
                        {
                            existSide = item.Key;
                            break;
                        }
                        
                        //获取对面的座位
                        if ((int)existSide >= 3)
                            side = existSide - 2;
                        else side = existSide + 2;
                    }
                    else //人数有两个及以上：当人数在3、4人时更改游戏模式可能出现人数超出2人的情况
                        throw new GameManagerException("玩家进场", "非法的玩家进场请求：当前游戏模式为单挑，人数已满。可通过改更游戏模式来允许更多人进入");
                }
                //四人游戏逆时针安排座位
                else if (_PlayersInfo.Count <4)//四人游戏模式下人数未满
                {
                    List<OfSide> list = new List<OfSide>(4);
                    foreach (var item in _PlayersInfo)
                    {
                        list.Add(item.Key);
                    }
                    list.Sort((s1, s2) =>
                    {
                        return s1 - s2;
                    });
                    side = list[list.Count - 1] + 1;
                }
                else//四人游戏模式下人数已满
                    throw new GameManagerException("玩家进场", "非法的玩家进场请求：当前游戏人数已满。");
            }

            //至此允许玩家进场
            _PlayersInfo.Add(side, new SideInfo(side));
            //PlayerEnter?.Invoke(this, side);
            return side;

        }
        ///// <summary>
        ///// 调换座位
        ///// </summary>
        ///// <param name="from"></param>
        ///// <param name="to"></param>
        //public void ExchangeSide(OfSide from,OfSide to)
        //{
        //    if (_status != GameStatus.Layouting)//游戏不处于布阵态
        //        throw new GameManagerException("玩家进场", "非法的玩家进场请求：游戏进程不允许");
        //}
        /// <summary>
        ///玩家退出,返回退出的方式：正常退出、强制退出。
        ///布阵时正常退出，玩家阵亡或已投降时可退出但保留玩家信息，
        ///存活时退出视为强制退出:清理棋子更新游戏状态更新下一行棋玩家
        /// </summary>
        /// <param name="side"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="GameManagerException"></exception>
        /// <exception cref="GameManagerCodeException"></exception>
        public void Exit(OfSide side)
        {
            if (!Enum.IsDefined(typeof(OfSide), side))
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if (!_PlayersInfo.ContainsKey(side))//游戏中不包含此玩家
                throw new GameManagerException("玩家退出", "游戏中不存在此玩家");
            
            //至此，玩家属于此游戏
            switch (Status)
            {
                case GameStatus.Over:
                    //正常退出
                    _PlayersInfo.Remove(side);
                    PlayerExit?.Invoke(this, side);
                    break;
                case GameStatus.Layouting://且游戏在布阵态或此盘游戏结束
                    //正常退出
                    _PlayersInfo.Remove(side);
                    PlayerExit?.Invoke(this, side);
                    _updateGameStatus();
                    break;
                case GameStatus.Doing://游戏开始了
                    PlayerStatus playerStatus = _PlayersInfo[side].Status;
                    if (playerStatus == PlayerStatus.Death|| playerStatus==PlayerStatus.Surrendered)//但玩家阵亡或已投降
                    {
                        _PlayersInfo[side].Status = PlayerStatus.Eixted;//可以正常退出，对游戏进程无影响
                    }
                    else if (playerStatus == PlayerStatus.Alive)//而玩家尚且存活
                    {//强制退出
                        _PlayersInfo[side].Status = PlayerStatus.ForceEixted;
                        if(_drawTimer!=null)//若正在进行和棋投票
                            AgreeDraw(side);//则默认同意和棋
                        ChessInfo[] ci = _checkbroad.ClearChessOf(side);//清理其所有棋子
                        PlayerForceExit?.Invoke(this, new PlayerForceExitEventArgs(Steps, side, ci));
                        _updateGameStatus();
                        if (Status == GameStatus.Doing)//若游戏可继续
                            _updateNextSide();//则轮到下一方行棋
                    }
                    else if(playerStatus==PlayerStatus.Eixted|| playerStatus == PlayerStatus.ForceEixted|| playerStatus==PlayerStatus.LostConnection)
                        throw new GameManagerException("玩家退出", "玩家已退出或掉线，无法正确完成此退出请求！");
                    else //玩家未准备、玩家准备了，这只能说明是代码出错了
                        throw new GameManagerCodeException("GameManager.Exit()", "调用玩家退出方法时，代码存在逻辑错误！");
                    break;
                case GameStatus.Closed:
                    throw new GameManagerException("玩家退出", "非法的玩家退出请求：游戏已关闭");
                default:
                    throw new GameManagerCodeException("GameManager.Exit()", "游戏状态Status错误，代码存在逻辑错误！");
            }

        }
        /// <summary>
        /// 玩家准备。非法抛出异常，并且不做任何操作。
        /// </summary>
        /// <param name="side"></param>
        /// <param name="layout"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="GameRuleException"></exception>
        /// <exception cref="GameManagerException"></exception>
        public void Ready(OfSide side, int[,] layout)
        {
            if (!Enum.IsDefined(typeof(OfSide), side))
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if (Status != GameStatus.Layouting)//若游戏进程不允许布阵
                throw new GameManagerException("玩家准备", "准备和布局错误：当前游戏进程不允许进行此操作");
            if (!_PlayersInfo.ContainsKey(side))//若该方未进入游戏
                throw new GameManagerException("玩家准备", "此方未进入游戏，请进入游戏后再进行准备");

            PlayerStatus playerStatus= _PlayersInfo[side].Status;
            //至此玩家进入游戏并且游戏未开始
            if (playerStatus != PlayerStatus.UnReady)//若该方不处于未准备状态
                if (playerStatus== PlayerStatus.Ready)
                    throw new GameManagerException("玩家准备", "此方已经准备，请取消准备后，再重新准备并布阵");
                else
                    throw new GameManagerCodeException("Ready", "玩家的状态错误，属于代码逻辑错误");


            //至此，开始玩家的准备工作
            ChessInfo[] ci = CheckBroad.ConvertFromLayoutToCheInfo(layout, side);
            Chess[] cheList = new Chess[ci.Length];
            for (int i = 0; i < ci.Length; i++)
            {
                cheList[i] = ci[i].chess;
            }
            if (Chess.IsOneSideAllChesses(cheList, side)) //若要求布阵的棋子都是此方能拥有的所有棋子
            {
                if (_checkbroad.Layout(ci).Length != 0)//若布阵非法
                    throw new GameRuleException("布阵错误：布阵非法");
                else //合法布阵
                {
                    _PlayersInfo[side].Status = PlayerStatus.Ready;
                    AllChesesLaout.Add(side, layout);
                    PlayerReady?.Invoke(this, new PlayerReadyEventArgs(side,layout));
                    _updateGameStatus();
                }
            }
            else
                throw new GameRuleException("准备错误：布阵非法，进行布阵的棋子不能有" + ci.Length.ToString() +"个、或进行布阵的棋子并非都是此方能拥有的所有棋子");

        }
        //4.取消准备,Ready成功后的撤销。参数异常、游戏规则异常：未准备、已开始游戏。
        public void CancelReady(OfSide side)
        {
            if (!Enum.IsDefined(typeof(OfSide), side))
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if (Status != GameStatus.Layouting)//若游戏进程不允许取消准备
                throw new GameRuleException("不允许取消准备：游戏已开始或结束或关闭");

            if (_PlayersInfo.TryGetValue(side,out SideInfo playerInfo))
            {
                if (playerInfo.Status == PlayerStatus.Ready)//且该方已经准备
                {
                    _checkbroad.ClearChessOf(side);
                    playerInfo.Status = PlayerStatus.UnReady;
                    AllChesesLaout.Remove(side);
                    PlayerCancelReady?.Invoke(this, side);
                }
                else throw new GameManagerException("取消准备", "玩家取消准备错误，该玩家未准备");
            }
            else throw new GameManagerException("取消准备", "玩家取消准备错误，该玩家未进入游戏");


        }


        #region 对局中
        /// <summary>
        /// 行棋：某方要求行棋，返回行走结果(名棋，需经过FuzzifyMoveInfo处理).
        /// 参数错误将抛出ArgumentOutOfRangeException或ArgumentException 
        /// </summary>
        /// <remarks>
        /// 合法行棋后，记录棋子行走信息。
        /// 军旗死亡清空其方所有棋子，该方状态为死亡，进一步判断游戏是否结束;
        /// 司令死亡，激发司令死亡事件 </remarks>
        /// <param name="side">行棋方</param>
        /// <param name="startC">要行走的棋子的坐标</param>
        /// <param name="endC">目标坐标</param>
        /// <exception cref="GameRuleException"></exception>
        /// <exception cref="GameManagerCodeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Move(OfSide side, Coord startC, Coord endC)
        {
            if (Status != GameStatus.Doing)//若游戏状态不允许
                throw new GameManagerException("行棋","行棋失败：游戏未开始或已经结束");
            //参数检测：side
            if (!Enum.IsDefined(typeof(OfSide), side))
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if (side != NowCanMoveSide)//若未轮到该方行棋
            {
                throw new GameManagerException("行棋","行棋失败：未轮到该方行棋");
            }
            //参数检测startC
            Chess ches;
            try
            {
                ches = _checkbroad.VertexDataM[startC.i, startC.j].Chess;//获取起点的棋子
                if (ches == null)//棋子不存在
                    throw new ArgumentException("startC", "行棋失败：该点无棋子");
                if (ches.Side != side)//且棋子属于该请求方
                    throw new GameManagerException("行棋", "行棋失败：该方无权限行走该棋子");
            }
            catch (ArgumentException)
            {
                throw new ArgumentOutOfRangeException("startC", "行棋失败：起点坐标错误");
            }
            MoveInfo mi;
            try
            {
                mi = _checkbroad.Move(startC, endC);//获取行走结果,非法的话此方法会抛出异常
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("endC", "行棋失败：目的坐标错误");
            }
            catch(GameRuleException e)
            {
                throw e;//非法行走
            }
            //至此行走合法,行棋结束。
            //记录次步行走，激发事件
            ChessMoveRecord record = new ChessMoveRecord(++Steps, mi);
            _chessMoveRecords.Enqueue(record);
            PlayerChessMove?.Invoke(this, new SimpleMoveInfo(Steps,side,mi.MType,startC,endC));

            //检测有无司令或军旗死亡
            ChessInfo[] deadChessInfo ;
            switch (mi.MType)
            {
                case MoveResult.Eat:
                    deadChessInfo = new ChessInfo[1] { mi.CrashedChess };
                    break;
                case MoveResult.BeEated:
                    deadChessInfo = new ChessInfo[1] { mi.MovingChess };
                    break;
                case MoveResult.TheSameTimeToDie:
                    deadChessInfo = new ChessInfo[2] { mi.CrashedChess,mi.MovingChess };
                    break;
                case MoveResult.Normal:
                default:
                    deadChessInfo = new ChessInfo[0] ;
                    break;
            }
            foreach (var chessInfo in deadChessInfo)
            {
                switch (chessInfo.chess.Type)
                {
                    case ChessType.SiLing://是司令死亡
                        ChessInfo? junqiInfo = _checkbroad.FindJunQi(chessInfo.chess.Side);
                        if (!junqiInfo.HasValue)
                            throw new GameManagerCodeException("行棋", "玩家司令死亡，却找不到军旗位置");
                        PlayerSiLingDied?.Invoke(this, new ChessInfo[] { chessInfo, junqiInfo.Value });
                        break;
                    case ChessType.JunQi://是军旗死亡
                        _checkbroad.ClearChessOf(chessInfo.chess.Side);
                        _PlayersInfo[chessInfo.chess.Side].Status = PlayerStatus.Death;
                        PlayerDie?.Invoke(this, chessInfo.chess.Side);
                        break;
                    default:
                        break;
                }
            }
            //更新游戏状态、返回结果
            _updateGameStatus();
            if (side==NowCanMoveSide&& Status == GameStatus.Doing)//若游戏还未结束且此时是轮到该玩家行棋
                _updateNextSide();//轮到下一方行棋
        }
        //6.跳过行棋
        public void SkipMove(OfSide side)
        {
            if (Status != GameStatus.Doing)//若游戏状态不允许
                throw new GameRuleException("跳过行棋失败：游戏未开始或已经结束");
            if (!Enum.IsDefined(typeof(OfSide), side))//参数检测：side
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if (side != NowCanMoveSide)//若未轮到该方行棋
            {
                throw new GameRuleException("跳过行棋失败：未轮到该方行棋");
            }

            //至此开始跳过行棋
            Steps++;
            if (++_PlayersInfo[side].SkipNum>=SkipMaxNum)//若超出允许的次数
                Surrender(side);//投降
            else  _updateNextSide();

        }

        /// <summary>
        /// 投降，更新玩家状态信息。
        /// 需满足条件：游戏进行中，且玩家属于此盘游戏并在游戏中
        /// </summary>
        /// <param name="side">投降方</param>
        /// <exception cref="GameRuleException">游戏未开始或结束或关闭</exception>
        /// <exception cref="ArgumentOutOfRangeException">非法的投降方数值</exception>
        /// <exception cref="GameManagerException">此盘游戏没有该玩家，该玩家不在进行游戏或已阵亡</exception>
        public void Surrender(OfSide side)
        {
            if (Status != GameStatus.Doing)//若游戏不在进行中
                throw new GameRuleException("行棋失败：游戏未开始或结束或关闭");
            if (!Enum.IsDefined(typeof(OfSide), side))//参数检测：side
                throw new ArgumentOutOfRangeException("side", "非法的投降方数值");
            if (!_PlayersInfo.ContainsKey(side))//此盘游戏没有此玩家
                throw new GameManagerException("玩家投降", "此盘游戏没有该玩家");
            if (_PlayersInfo[side].Status!=PlayerStatus.Alive)//当前玩家掉线或未存活：不可活动的
                throw new GameManagerException("玩家投降", "该玩家不在进行游戏或已阵亡");

            //至此，满足投降条件：游戏进行中，且玩家在属于此盘游戏并在游戏中
            ChessInfo[] ci = _checkbroad.ClearChessOf(side);
            _PlayersInfo[side].Status = PlayerStatus.Surrendered;
            PlayerSurrender?.Invoke(this, new SurrenderEventArgs(Steps, side, ci));
            //PlayerDie?.Invoke(this, side); //投降非死亡，相同的是清理其所有在棋盘上的棋子
            _updateGameStatus();
        }

        /* 8.请求和棋。玩家在游戏进行中在某步提出和棋请求，不需征求失败或退出的玩家，只征求在游戏中的玩家
         * 以和棋定时器非空判定和棋在进行中。
         * 异常：
         *  ArgumentOutOfRangeException:参数side超出枚举
         *  GameManagerException:此游戏中无此玩家。此玩家并非在游戏，可能阵亡、可能退出了。
         */
        public void OfferDraw(OfSide side)
        {
            if (!Enum.IsDefined(typeof(OfSide), side))//参数检测：side
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if ( !_PlayersInfo.ContainsKey(side) )//此游戏中无此玩家
                throw new GameManagerException("请求和棋", "此游戏中无此玩家");
            if (_PlayersInfo[side].Status!=PlayerStatus.Alive)//此玩家并非在游戏，可能阵亡、可能退出了。
                throw new GameManagerException("请求和棋", "此玩家并非在游戏，可能阵亡、可能退出了。");
            //至此，参数合法，游戏中有此玩家，并在游戏中提出和棋
            if (_drawTimer!=null)//有和棋请求正在进行
                throw new GameManagerException("请求和棋", "此玩家或其他玩家正在请求和棋");
            //至此，无其他正在进行的和棋请求
            if (_PlayersInfo[side].OfferDrawNum>=OfferDrwaMaxNum)//若和棋请求次数用尽
                throw new GameManagerException("请求和棋", "此玩家的和棋请求次数已用尽");

            //进行和棋请求
            _PlayersInfo[side].OfferDrawNum += 1;
            _playerDrawRecord = new DrawRecord(Steps, side);
            //获取和设置投票人名单
            foreach (var theSide in _PlayersInfo.Keys)
            {
                if (_PlayersInfo[theSide].Status==PlayerStatus.Alive &&
                    theSide!= _playerDrawRecord.OfferSide)//游戏中活动的，排除提出和棋的玩家
                {
                    _playerDrawRecord.RigthSideAgree.Add(theSide, false);
                    _playerDrawRecord.IsDoDraw.Add(theSide, false);
                }
            }
            //创建定时器并设置超时时关闭定时器，记录和棋，超时后记录和棋行棋并关闭定时器
            _drawTimer = new System.Timers.Timer(DrawInterval) { AutoReset = false };
            _drawTimer.Elapsed += (sender, elapsedEventArgs) => {
                _drawTimer.Close();
                _drawTimer = null;
                _drawRecords.Enqueue(_playerDrawRecord);
                ExpireDraw?.Invoke(this, _playerDrawRecord);
            };
            _drawTimer.Start();
            PlayerOfferDraw?.Invoke(this, side);
        }
        /*9.同意和棋
         *异常：
         *  ArgumentOutOfRangeException:参数side超出枚举
         *  GameManagerException:此游戏中无此玩家。此玩家并非在游戏，可能阵亡、可能退出了。
         *                       无请求和棋方。同意和棋方与请求方一致或重复提交同意请求
         */
        public void AgreeDraw(OfSide side)
        {
            if (!Enum.IsDefined(typeof(OfSide), side))//参数检测：side
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if (_drawTimer==null)//不在和棋中
                throw new GameManagerException("同意和棋", "无正在进行的和棋进程");

            //至此，参数合法，此玩家想参与正在进行的和棋请求
            if (_playerDrawRecord.IsDoDraw.TryGetValue(side, out bool isDoDraw))
            {
                if (isDoDraw ==true)
                    throw new GameManagerException("请求和棋", "已回应和棋请求，无法同意或再次同意和棋");  
            }
            else //该玩家无权参加和棋投票
                throw new GameManagerException("同意和棋", "该玩家无权参加和棋投票");

            //至此，此玩家可以同意和棋
            _playerDrawRecord.IsDoDraw[side] = true;
            _playerDrawRecord.RigthSideAgree[side] = true;
            PlayerAgrreDraw?.Invoke(this, side);
            foreach (var theside in _playerDrawRecord.IsDoDraw.Keys)
            {
                if (_playerDrawRecord.IsDoDraw[theside] == false)//若有一方还未回应和棋
                    return;
                else if (_playerDrawRecord.RigthSideAgree[theside] == false)//若玩家回应了，但拒绝了
                    throw new GameManagerCodeException("GameManager.AgreeDraw()","有玩家拒绝了和棋，但和棋进程还未结束，此为代码逻辑错误");
            }
            
            //至此，所有玩家都同意和棋
            _drawTimer.Close();
            _drawTimer = null;
            //获取胜利方、强退者
            List<OfSide> winners=new List<OfSide>(4), forceExist=new List<OfSide>();
            foreach (var theside in _PlayersInfo.Keys)
            {
                switch (_PlayersInfo[theside].Status)
                {
                    case PlayerStatus.Alive:
                    case PlayerStatus.Death:
                    case PlayerStatus.Surrendered:
                    case PlayerStatus.Eixted://正常完成游戏
                    case PlayerStatus.LostConnection://失去连接不久，超时将变为强退
                        winners.Add(theside);
                        break;
                    case PlayerStatus.ForceEixted:
                        forceExist.Add(theside);
                        break;
                    case PlayerStatus.UnReady:
                    case PlayerStatus.Ready:
                        throw new GameManagerCodeException("GameManager.AgreeDraw()", "玩家状态错误");
                    default:
                        throw new GameManagerCodeException("GameManager.AgreeDraw()", "_PlayersInfo[key].Status的值出错");
                }
            }
            _gameResult = new GameResult(true, winners.ToArray(), null, forceExist.ToArray(),_playerDrawRecord);
            Status = GameStatus.Over;
        }
        //10.拒绝和棋
        public void RefuseDraw(OfSide side)
        {
            if (!Enum.IsDefined(typeof(OfSide), side))//参数检测：side
                throw new ArgumentOutOfRangeException("side", "非法的势力参数");
            if (_drawTimer == null)//不在和棋中
                throw new GameManagerException("拒绝和棋", "无正在进行的和棋进程");

            //至此，参数合法，此玩家想参与正在进行的和棋请求
            if (!_playerDrawRecord.RigthSideAgree.ContainsKey(side))//若不在投票名单内
                throw new GameManagerException("拒绝和棋", "此玩家无权参与和棋");

            //至此，开始拒绝和棋
            _drawTimer.Close();
            _drawTimer = null;
            _playerDrawRecord.IsDoDraw[side] = true;
            _playerDrawRecord.RigthSideAgree[side] = false;
            _drawRecords.Enqueue(_playerDrawRecord);
            PlayerRefuseDraw?.Invoke(this, new PlayerRefuseDrawEventArgs(side, _playerDrawRecord));
        }
        //11.玩家掉线
        //12.玩家重连
        //13.玩家想获取此盘游戏所有信息

        //14.重载：旁观者获取棋盘信息


        //15.
        public ChessInfo[] IWantToWatchAllChesses(OfSide side)
        {
            return default(ChessInfo[]);
        }

        /// <summary>
        /// 根据当前游戏的信息，根据棋子对此方的可见性，模糊化布阵图。
        /// 默认参数都合法
        /// </summary>
        /// <param name="ci"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="mode"></param>
        public static int[,] FuzzifyLayout(int[,] ci, OfSide from,OfSide to, GameMode mode)
        {
            //拷贝二维数组
            int[,] layout = new int[ci.GetLength(0), ci.GetLength(1)];
            for (int i = 0; i < layout.GetLength(0); i++)
            {
                for (int j = 0; j < layout.GetLength(1); j++)
                {
                    layout[i, j] = ci[i,j];
                }
            }
            //转化
            switch (mode)
            {
                case GameMode.SiMing:
                    break;
                case GameMode.SiAn:
                case GameMode.Solo:
                    if (from!= to)
                    {
                        for (int i = 0; i < layout.GetLength(0); i++)
                        {
                            for (int j = 0; j < layout.GetLength(1); j++)
                            {
                                if(layout[i, j]!=-2)
                                    layout[i, j] =  -1 ;
                            }
                        }
                    }
                    break;
                case GameMode.ShuangMing:
                    if (from != to || Math.Abs(from-to)!=2)
                    {
                        for (int i = 0; i < layout.GetLength(0); i++)
                        {
                            for (int j = 0; j < layout.GetLength(1); j++)
                            {
                                if (layout[i, j] != -2)
                                    layout[i, j] = -1;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            return layout;
        }

        /// <summary>
        /// 根据当前游戏的信息，根据棋子对此方的可见性，模糊化棋盘上的棋子信息（送给客户端）。
        /// 默认参数都合法
        /// </summary>
        /// <param name="ci"></param>
        /// <param name="toside"></param>
        /// <param name="mode"></param>
        public static ChessInfo[] FuzzifyChessInfo(ChessInfo[] ci, OfSide toside, GameMode mode)
        {
            ChessInfo[] chessInfos=new ChessInfo[ci.Length];
            for (int i = 0; i < chessInfos.Length; i++)
            {
                chessInfos[i] = ci[i];
            }
            switch (mode)
            {
                case GameMode.SiMing:
                    break;
                case GameMode.SiAn:
                case GameMode.Solo://除自己以外所有都隐藏掉
                    {
                        for (int i = 0; i < chessInfos.Length; i++)
                        {
                            if (chessInfos[i].chess.Side != toside)
                            {
                                chessInfos[i].chess.Type = ChessType.UnKnow;
                            }
                        }
                    }
                    break;
                case GameMode.ShuangMing:
                    {
                        for (int i = 0; i < chessInfos.Length; i++)
                        {
                            int abs = Math.Abs(chessInfos[i].chess.Side - toside);
                            if (abs != 2||abs!=0)//非同盟
                            {
                                chessInfos[i].chess.Type = ChessType.UnKnow;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            return chessInfos;
        }

        public void Close()
        {
            Status = GameStatus.Closed;
            //do something to dispose
        }

        #endregion
        //对类的某些字段和属性进行检测，更新游戏状态。具体的：Steps、_playersInfo、Mode、Status。
        //为了提高运行效率，有关和棋、跳过回合的，不进行检测。
        //对游戏状态进行设置，若游戏结束、设置相应的游戏结果。
        private void _updateGameStatus()
        {
            switch (Status)
            {
                #region 游戏开始前
                case GameStatus.Layouting://如果游戏还在布阵准备中
                    switch (Mode)//根据游戏模式
                    {
                        case GameMode.SiMing:
                        case GameMode.SiAn:
                        case GameMode.ShuangMing:
                            if (_PlayersInfo.Count != 4)//若非4名玩家在座位上
                                return;
                            break;
                        case GameMode.Solo:
                            if (_PlayersInfo.Count != 2)//若非2名玩家在座位上
                                return;
                            break;
                    }
                    //至此，游戏在布阵之中，且玩家人数满足开始游戏的条件
                    foreach (var playerInfo in _PlayersInfo.Values)
                    {
                        if (playerInfo.Status != PlayerStatus.Ready)//若有玩家未准备
                            return;
                    }
                    //至此，都准备了
                    Status = GameStatus.Doing;

                    break;
                #endregion 


                case GameStatus.Doing://游戏进行中
                    {
                        switch (Mode){
                            case GameMode.SiMing:
                            case GameMode.ShuangMing:
                            case GameMode.SiAn://四人游戏：
                                {
                                    //结束条件1：场上存活的玩家都是己方的
                                    List<OfSide> alivePlayers = new List<OfSide>(4);
                                    foreach (var theside in _PlayersInfo.Keys)
                                    {
                                        if (_PlayersInfo[theside].Status == PlayerStatus.Alive)
                                            alivePlayers.Add(theside);
                                    }
                                    OfSide winner= alivePlayers[0];//假设胜利者是第一个元素
                                    int abs;bool over = true;
                                    foreach (var theside in alivePlayers)//对每一存活玩家
                                    {
                                        abs = Math.Abs(winner - theside);
                                        if (abs!=0 && abs!=2)//若有非己方的
                                        {
                                            over = false;
                                            break;
                                        }
                                    }
                                    if (over)
                                    {
                                        _setNotDrawGameResult();
                                        Status = GameStatus.Over;
                                        return;
                                    }

                                    //结束条件2：步数超额
                                    if (Steps>=FourPlayerMaxSteps)
                                    {
                                        _setDrawGameResult();
                                        Status = GameStatus.Over;
                                        return;
                                    }
                                }
                                break;
                                
                            case GameMode.Solo://两人游戏
                                {
                                    //结束条件1:只有一个玩家存活
                                    List<OfSide> alivePlayers = new List<OfSide>(2);
                                    foreach (var theside in _PlayersInfo.Keys)
                                    {
                                        if (_PlayersInfo[theside].Status == PlayerStatus.Alive)
                                            alivePlayers.Add(theside);
                                    }
                                    if (alivePlayers.Count==1)
                                    {
                                        _setNotDrawGameResult();
                                        Status = GameStatus.Over;
                                        return;
                                    }
                                    if (Steps>=TwoPlayerMaxSteps)
                                    {
                                        _setDrawGameResult();
                                        Status = GameStatus.Over;
                                        return;
                                    }
                                    
                                }
                                break;
                        }
                    }
                    break;
                //case GameStatus.Over:
                //    break;
                default:
                    break;
            }
            
        }
        //游戏结束时，设置非平局的游戏结果。//一定有存活的玩家
        private void _setNotDrawGameResult()
        {
            
            List<OfSide> winers = new List<OfSide>(2),
                        losers = new List<OfSide>(2),
                        force = new List<OfSide>(2);

            //根据游戏模式
            switch (Mode)
            {
                case GameMode.SiMing:
                case GameMode.SiAn:
                case GameMode.ShuangMing://四人游戏
                    {
                        //获取其中一个存活的玩家
                        OfSide aliveSide = OfSide.First;
                        foreach (var theside in _PlayersInfo.Keys)
                        {
                            if (_PlayersInfo[theside].Status == PlayerStatus.Alive)
                            {
                                aliveSide = theside;
                                break;
                            }
                        }
                        //设置结果
                        foreach (var theside in _PlayersInfo.Keys)
                        {
                            if (_PlayersInfo[theside].Status == PlayerStatus.ForceEixted)
                            {
                                force.Add(theside);
                                continue;
                            }
                            int abs = Math.Abs(aliveSide - theside);
                            if (abs == 0 || abs == 2)//与存活的玩家是同盟
                                winers.Add(theside);
                            else losers.Add(theside);
                        }
                    }
                    break;
                case GameMode.Solo://两人游戏
                    {
                        foreach (var theside in _PlayersInfo.Keys)
                        {
                            if (_PlayersInfo[theside].Status == PlayerStatus.ForceEixted)
                                force.Add(theside);
                            else if (_PlayersInfo[theside].Status == PlayerStatus.Alive)
                                winers.Add(theside);
                            else
                                losers.Add(theside);
                        }
                    }
                    break;
            }
            _gameResult = new GameResult(false, winers.ToArray(), losers.ToArray(), force.ToArray());
        }

        //游戏结束时，设置平局的游戏结果。//一定有存活的玩家
        private void _setDrawGameResult()
        {
            List<OfSide> winers = new List<OfSide>(2),
                       force = new List<OfSide>(2);
            foreach (var theside in _PlayersInfo.Keys)
            {
                if (_PlayersInfo[theside].Status == PlayerStatus.ForceEixted
                    || _PlayersInfo[theside].Status == PlayerStatus.LostConnection)//游戏结束时，玩家处于掉线状态，视为强退
                    force.Add(theside);
                else winers.Add(theside);
            }
            _gameResult = new GameResult(true, winers.ToArray(), new OfSide[0], force.ToArray(),_playerDrawRecord);
        }
        //根据行棋顺序,设置下一个该行棋的势力,包括游戏开始时第一次设置
        private void _updateNextSide()
        {
            if (Steps == 0) NowCanMoveSide = _playerMoveOrder[0];//游戏刚开始的第一步行棋方
            else
            {
                int index=0;
                for (; index < _playerMoveOrder.Length; index++)
                {
                    if (_playerMoveOrder[index] == NowCanMoveSide)//找到当前行棋方所在的顺序索引
                        break;
                }
                //从索引往后“轮回”式查找
                OfSide nextside;
                int nextsideIndex;
                if (index == _playerMoveOrder.Length-1)//找到的是尾部的索引
                    nextsideIndex = 0;
                else nextsideIndex = index + 1;
                for (int i=0; i< _playerMoveOrder.Length - 1; i++)
                {
                    nextside = _playerMoveOrder[nextsideIndex];
                    if (_PlayersInfo.TryGetValue(nextside,out SideInfo playerInfo))
                    {
                        if (playerInfo.Status == PlayerStatus.Alive)//下一玩家可行棋
                        {
                            NowCanMoveSide = nextside;
                            return;
                        }
                    }
                    if (nextsideIndex == _playerMoveOrder.Length - 1)//走到头了吗
                        nextsideIndex = 0;
                    else nextsideIndex++;
                }
                throw new GameManagerCodeException("GameManager._updateNextSide()","找不到下一应行棋方");
            }
        }




        /* 14.游戏结束后，可获取游戏复盘
         */
        public GameReplay GetTheReplay()
        {
            return default(GameReplay);
        }
        public class GameReplay
        {
            public Queue<ChessMoveRecord> ChessMoveRecords;//行棋记录
            public Queue<DrawRecord> DrawRecords;//和棋记录
            public Queue<SkipMoveRecord> SkipMoveRecord;//跳过行棋的记录
            public GameReplay(Queue<ChessMoveRecord> chessMoveRecords, Queue<DrawRecord> drawRecords)
            {
                ChessMoveRecords = chessMoveRecords;
                DrawRecords = drawRecords;
            }
            //输出到文件,over表示是否覆盖，是则为true,不覆盖，为true
            //异常：
            public static void PrintTOFile(GameReplay replay, string directory, bool over)
            {
                //if (!over)
                //{
                //    if (File.Exists(path))
                //        throw new GameManagerException("将复盘输出到文件","文件已存在");
                //}
                //string s =Newtonsoft.Json.JsonConvert.SerializeObject(replay);
                //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(s);
                return;
            }
            //从文件读取到对象
            //异常：格式错误、文件不存在
            public static GameReplay ReadByFile(string path)
            {
                //Newtonsoft.Json.JsonConvert.DeserializeObject();
                return default(GameReplay);
            }
        }
    }
}
