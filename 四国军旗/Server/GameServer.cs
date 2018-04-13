using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using NetConnector;
using NetConnector.MsgType;
using DataBase;
using GameLogic;
using System.Security.Cryptography;
/*
 * 游戏服务器:
 * 开启网络监听，与客户端建立连接后，开始
 * 处理玩家的请求：
 * 1.账号登录（、注册、找回）;
 * 2.玩家信息；
 * 3.匹配；
 * 4.游戏请求。
 * 为每一个登录的玩家生成token，用以标识玩家身份；
 * 每一次匹配成功后，创建、操控游戏管理者；
 */
namespace Server
{
    public delegate void Next(Session session,object data,Next next);//保留
    /// <summary>
    /// 游戏房间的抽象，它不对玩家进行任何广播
    /// </summary>
    public class Room
    {
        public static UInt64 Counter { get { return ++counter; } }//自增器
        static UInt64 counter = 0;//自增器的字段

        /// <summary>
        /// 房间唯一ID,采用自增。
        /// </summary>
        public readonly UInt64 ID;
        /// <summary>
        /// 游戏玩家列表,包含位置信息OfSide。
        /// </summary>
        public readonly List<PlayerInfo> PlayersInfo;
        /// <summary>
        /// 广播频道。
        /// </summary>
        public readonly Channel Channel;
        /// <summary>
        /// 游戏逻辑，游戏管理者。
        /// </summary>
        public readonly GameManager Gamemanager;
        /// <summary>
        /// 为玩家创建房间,开启游戏,并为玩家分配座位。默认uid都是不相等的。
        /// </summary>
        /// <param name="uids">玩家的UID表</param>
        /// <param name="mode">游戏模式</param>
        public Room(ulong[] uids, GameMode mode=GameMode.SiAn)
        {
            ID = Counter;
            Channel = new Channel(uids);
            Gamemanager = new GameManager(mode);
            Gamemanager.StartUp();
            PlayersInfo = new List<PlayerInfo>(4);
            foreach (var uid in uids)
            {
                PlayersInfo.Add(new PlayerInfo(uid, Gamemanager.Enter()));
            }
        }
        /// <summary>
        /// 进入游戏房间，随机分配座位,更新广播频道、玩家列表。游戏进入并不会导致游戏进程产生变化
        /// </summary>
        /// <exception cref="RoomException"></exception>
        /// <exception cref="GameManagerException"></exception>
        public event EventHandler<Room.PlayerInfo> PEnter;
        public void PlayerEnter(ulong uid)
        {
            int index = PlayersInfo.FindIndex((info) => { return info.UID == uid; });
            if (index != -1)//游戏列表中有此玩家
                throw new RoomException("请退出后再进入房间");
            OfSide side = Gamemanager.Enter();
            Channel.UIDs.Add(uid);
            PlayerInfo playerInfo = new PlayerInfo(uid, side);
            PlayersInfo.Add(playerInfo);
            PEnter?.Invoke(this, playerInfo);
        }
        /// <summary>
        /// 退出游戏房间，不要调用游戏管理者的退出方法，更新广播频道、玩家列表(游戏进行中不更新)。
        /// </summary>
        /// <exception cref="RoomException"></exception>
        /// <exception cref="GameManagerCodeException"></exception>
        public void PlayerExit(ulong uid)
        {
            int index= PlayersInfo.FindIndex((info) => { return info.UID == uid; });
            if (index == -1)
                throw new RoomException("玩家不在房间中，退出房间失败");

             //至此玩家在房间中
            try
            {
                Channel.UIDs.Remove(uid);//假设成功退出
                OfSide side = PlayersInfo[index].Side;
                Gamemanager.Exit(side);//成功会引发事件，而服务器会广播，那么上面的频道是有必要先行更新的
            }
            catch(GameManagerCodeException e)
            {
                throw e;
            }
            catch (Exception e)//退出失败
            {
                Channel.UIDs.Add(uid);//撤销
                throw new  RoomException(e.Message);
            }
            if (Gamemanager.Status != GameStatus.Doing)//游戏不在进行，玩家可以“真正”离开座位
                PlayersInfo.RemoveAt(index);

        }
        /// <summary>
        /// 根据游戏角色查找玩家信息。供给SubscriptGameEvent()使用。找不到返回默认值
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public PlayerInfo SearchInfoBySide(OfSide side)
        {
            return PlayersInfo.Find((info) =>
            {
                return info.Side == side;
            });
        }
        /// <summary>
        /// 找不到返回默认值
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public PlayerInfo SearchInfoByUID(UInt64 uid)
        {
            return PlayersInfo.Find((info) =>
            {
                return info.UID == uid;
            });
        }
        public class PlayerInfo
        {
            public UInt64 UID;//玩家唯一ID
            public OfSide Side;//在游戏中所掌管的势力方
            public PlayerInfo(UInt64 uid, OfSide side)
            {
                UID = uid;
                Side = side;
            }
        }
    }
    public struct MatchingInfo
    {
        public UInt64 UID;
        public GameMode Mode;
        public long StartTick;
    }

    public partial class GameServer
    {
        /// <summary>
        /// 设置为公开，用于服务器控制台测试
        /// </summary>
        public Connector _connector;     //管理连接的模块,

        private DBAcess _dataBaseAccess;//数据库访问模块

        private List<Room> _gameRooms;//游戏房间列表


        public GameServer(IPAddress iPAddress,int port=8080)
        {
            _connector = new Connector(iPAddress, port);
            _dataBaseAccess = new DBAcess();
            _matchingList = new List<MatchingInfo>();
            _gameRooms = new List<Room>();
            _connector.SessionClose += _onSessionClose;
            _connector.MsgReceived += _onMsgReceived;
            timer = new System.Timers.Timer(timeoutInterval);
            timer.Elapsed += (sender,e)=>_checkTimeOutMatch();
        }
        public void StartUp()
        {
            _dataBaseAccess.Open();
            _connector.RunServerAsync();
            timer.Start();
        }
        public void Close()
        {
            _connector.Close();
            _dataBaseAccess.Close();
            _matchingList.Clear();
            _gameRooms.Clear();
        }
        private void _onSessionClose(object sender,Session session)
        {
            //清理匹配、房间中的
            if (session.IsLogin)
            {
                _matchingList.RemoveAll((item)=>item.UID==session.UID);
                if (session.RoomID!=0)//session中有房间标记
                {
                    Room room = _searchRoomByID(session.RoomID);
                    try
                    {
                        room.PlayerExit(session.UID);
                        if (room.Channel.UIDs.Count==0)
                        {
                            _gameRooms.Remove(room);
                        }
                    }
                    catch (RoomException exc)//房间不存在
                    {
                        //log....
                        return;
                    }
                    
                }
               
            }
            
        }
        /// <summary>
        /// 将玩家的请求路由到对应的处理方法。由它发起对服务器各模块的调用。
        /// </summary>
        /// <remarks>它默认接收到的数据e.Data都是在MsgType中定义的结构体</remarks>
        /// <param name="sender">Connector</param>
        /// <param name="e"></param>
        private void _onMsgReceived(object sender,NetClientMsgEventArgs e)
        {
            
            switch (e.MsgType.Name)
            {
                //account
                case "LoginIn":
                    _onLogin(e.Session, (LoginIn)e.Data);
                    break;
                //
                case "GetPlayerInfo":
                    _onGetPlayerInfo(e.Session, (GetPlayerInfo)e.Data);
                    break;
                //match
                case "Match":
                    _onMatch(e.Session, (Match)e.Data);
                    break;
                case "CancelMatch":
                    _onCancelMatch(e.Session, (CancelMatch)e.Data);
                    break;
                //Room
                case "Ready":
                    _onReady(e.Session, (Ready)e.Data);
                    break;
                case "Move":
                    _onMove(e.Session, (Move)e.Data);
                    break;
                case "GetGInfo":
                    _onGetGInfo(e.Session, (GetGInfo)e.Data);
                    break;
                default:
                    //

                    break;
            }
        }
       
        
        #region 玩家信息获取模块
        /// <summary>
        /// 获取玩家信息的处理方法
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data"></param>
        private void _onGetPlayerInfo(Session session, GetPlayerInfo data)
        {
            if (!LoginFilter(session,"获取玩家信息"))
                return;
            if(_dataBaseAccess.TryQueryPlayerInfo(data.PlayerID,out DBAcess.PNGDBRecord record))
            {
                NetConnector.MsgType.PlayerInfo playerInfo = new NetConnector.MsgType.PlayerInfo
                {
                    Escape = record.Escape,
                    Fail = record.Fail,
                    Grade = record.Grade,
                    NickName = record.Nickname,
                    PlayerID = record.ID,
                    Score = record.Score,
                    Win = record.Win
                };
                _connector.SendDataAsync(session, playerInfo);
            }
            else
                _connector.SendDataAsync(session, new GetquestError() { ClientMsgType = "GetPlayerInfo", ErrorInfo = "找不到玩家信息" });


        }
        #endregion

        #region 匹配模块
        public List<MatchingInfo> _matchingList;
        public static long timeoutInterval = TimeSpan.TicksPerMinute;//匹配请求的超时时间
        private object thislock;
        public System.Timers.Timer timer; //
        /// <summary>
        /// 处理匹配请求，成功则创建房间并广播给玩家。并没有处理玩家是否处于其他游戏中
        /// </summary>
        /// <param name="session"></param>
        /// <param name="match"></param>
        private void _onMatch(Session session,Match match)
        {
            if (!LoginFilter(session,"匹配"))
                return;
            if (session.RoomID != 0)
            {
                _connector.SendDataAsync(session, new GetquestError() {
                    Code = 102 ,
                    ClientMsgType="匹配请求",
                    ErrorInfo="玩家在游戏中，不能进行匹配"});
                return;
            }

            //至此开始匹配
            MatchingInfo matchingInfo = new MatchingInfo()
            {
                Mode = match.GameMode,
                UID = session.UID,
                StartTick = DateTime.Now.Ticks
            };
            _matchingList.RemoveAll((info)=>info.UID == session.UID);//总是剔除该玩家已有的匹配请求
            _matchingList.Add(matchingInfo);
            MatchInfo matchInfo;
            if (_tryMatchGame(out UInt64[] uids, out Room room))//若匹配到房间
            {
                matchInfo = new MatchInfo()
                {
                    GameMode = room.Gamemanager.Mode,
                    HasAGame = true,
                    PlayerInfo = room.PlayersInfo.ToArray(),
                    RoomID = room.ID,
                };
                _gameRooms.Add(room);
                //清理匹配列表
                _matchingList.RemoveAll((macthing) => {//对于每个在匹配表中的玩家
                    foreach (var uid in uids)
                    {
                        if (uid == macthing.UID)//是已经匹配房间的玩家
                            return true;
                    }
                    return false;
                });
                //设置会话中的RoomID
                _connector.Sessions.ForEach((se) => {
                    foreach (var uid in uids)
                    {
                        if (uid == se.UID)
                        {
                            se.RoomID = room.ID;
                            return;
                        }
                    }
                });
                //广播匹配结果
                _connector.PushMsgByChannel(room.Channel, matchInfo);
                //通告游戏开始
                _connector.PushMsgByChannel(room.Channel, new GameLayouting() { RoomID=room.ID});
                //开启游戏房间内的事件广播
                SubscriptGameEvent(room);
            }
            else //未匹配到游戏
            {
                //matchInfo = new MatchInfo() { HasAGame = false };
                //_connector.SendDataAsync(session, matchInfo);
            }
            
        }
        /// <summary>
        /// 匹配算法,匹配到则返回true并创建房间，否则为false。它最多只匹配到一个房间，成功后不再进行匹配。
        /// </summary>
        /// <remarks>只按模式对列表头几个元素进行简单匹配</remarks>
        /// <exception cref="NotImplementedException"></exception>
        private bool _tryMatchGame(out UInt64[] uids, out Room room)
        {
            List<UInt64>[] alluidsOfMode=new List<ulong>[4];//使用二维列表记录各模式下的匹配人数
            int siming = 0, sian = 1, shuangming = 2, solo = 3;//各模式的索引
            for (int i = 0; i < alluidsOfMode.Length; i++)
            {
                alluidsOfMode[i] = new List<ulong>(4);
            }
            
            foreach (var item in _matchingList)
            {
                switch (item.Mode)
                {
                    case GameMode.SiMing:
                        alluidsOfMode[siming].Add(item.UID);
                        break;
                    case GameMode.SiAn:
                        alluidsOfMode[sian].Add(item.UID);
                        break;
                    case GameMode.ShuangMing:
                        alluidsOfMode[shuangming].Add(item.UID);
                        break;
                    case GameMode.Solo:
                        alluidsOfMode[solo].Add(item.UID);
                        break;
                    default:
                        throw new  NotImplementedException();
                }
            }
            
            for (int i = 0; i < alluidsOfMode.Length; i++)
            {
                if (i==solo)//两人模式
                {
                    if (alluidsOfMode[i].Count >= 2)
                    {
                        uids = new ulong[2];
                        alluidsOfMode[i].CopyTo(0,uids,0,2);
                        room = new Room(uids, (GameMode)i);
                        return true;
                    }
                }
                else if (alluidsOfMode[i].Count >= 4)//私人模式下，满足匹配条件
                {
                    uids = new ulong[4];
                    alluidsOfMode[i].CopyTo(0, uids, 0, 4);
                    room = new Room(uids,(GameMode)i);
                    return true;
                }
            }
            uids = default(ulong[]);
            room = null;
            return false;
        }

        private void _onCancelMatch(Session session, CancelMatch match)
        {
            if (!LoginFilter(session,"取消匹配"))
                return;
            int num=_matchingList.RemoveAll((info)=>  session.UID ==info.UID);
            if (num==0)//该玩家没有匹配请求
            {
                _connector.SendDataAsync(session, new GetquestError() { Code=105,ErrorInfo= "该玩家没有匹配请求或已进入游戏" });
            }
            else
                _connector.SendDataAsync(session, new MatchInfo() { HasCancel = true, Info = "已取消" });
        }
        /// <summary>
        /// 检测匹配表中超时(1分钟)的匹配请求，通告该玩家
        /// </summary>
        private void _checkTimeOutMatch()
        {
            lock (_matchingList)//可以想象成把此线程加入到当前主线程之中？？？
            {
                MatchingInfo[] m = _matchingList.ToArray();
                long nowtick = DateTime.Now.Ticks;
                MatchInfo netdata = new MatchInfo()
                {
                    HasCancel = true,
                    Info = "匹配超时"
                };
                foreach (var item in m)
                {
                    if (nowtick - item.StartTick > timeoutInterval)//若该匹配请求超时
                    {
                        _connector.PushMsgByUID(item.UID, netdata);
                        _matchingList.Remove(item);
                    }
                    else //在表头的匹配请求是等待最久的，因此前后的没有超时，后面的自然也没有
                        break;
                }
            }
           
        }
        #endregion


        #region 游戏对局模块
        /// <summary>
        /// 获取玩家所在房间的那盘游戏的所有信息
        /// </summary>
        /// <param name="session"></param>
        /// <param name="req"></param>
        private void _onGetGInfo(Session session, GetGInfo req)
        {
            if (!_roomFilter(session, req.RoomID,out Room room, out Room.PlayerInfo playerInfo))
                return;

            //至此请求合法
            OfSide side = playerInfo.Side;
            ChessInfo[] chessInfos = room.Gamemanager._checkbroad.GetCurrentChesses();
            ChessInfo[] endchessInfos= GameManager.FuzzifyChessInfo(chessInfos,side,room.Gamemanager.Mode);
            RGInfo rGInfo = new RGInfo()
            {
                RoomID = req.RoomID,
                GMode = room.Gamemanager.Mode,
                Step = room.Gamemanager.Steps,
                GStatus = room.Gamemanager.Status,
                PlayerInfo = room.PlayersInfo,
                CChessesInfo = endchessInfos
            };
            _connector.SendDataAsync(session, rGInfo);
        }
        /// <summary>
        /// 订阅游戏管理者的事件（游戏对局中除玩家进出房间的所有事件），广播给处在频道中的所有玩家。
        /// </summary>
        /// <remark>
        /// 匹配到房间、开启游戏进入布阵态后，最后才开始广播游戏事件
        /// </remark>
        private void SubscriptGameEvent(Room room)
        {
            //玩家进出游戏
            room.PEnter += (sender, info) =>
            {
                _connector.PushMsgByChannel(room.Channel, new PEnter() { RoomID = room.ID, PlayerInfo = info });
            };
            room.Gamemanager.PlayerExit += (sender, side) => {
                _connector.PushMsgByChannel(room.Channel, new PExit()
                {
                    RoomID = room.ID,
                    PlayerInfo = room.SearchInfoBySide(side)
                });
            };
            room.Gamemanager.PlayerForceExit += (sender, info) =>
            {
                PForceExit pForceExit = new PForceExit()
                {
                    RoomID = room.ID,
                    PlayerInfo=room.SearchInfoBySide(info.Side)
                };
                _connector.PushMsgByChannel(room.Channel,pForceExit);
            };
           
            //游戏变化
            room.Gamemanager.PlayerReady += (sender, readyInfo) =>
            {
                foreach (var uid in room.Channel.UIDs)
                {
                    Room.PlayerInfo toInfo = room.SearchInfoByUID(uid);
                    Room.PlayerInfo fromInfo = room.SearchInfoBySide(readyInfo.Side);
                    int[,] layout = GameManager.FuzzifyLayout(readyInfo.CheLayout, fromInfo.Side, toInfo.Side, room.Gamemanager.Mode);
                    PReady pReady = new PReady()
                    {
                        RoomID = room.ID,
                        CheLayout = layout,
                        PlayerInfo = fromInfo
                    };
                    _connector.PushMsgByUID(uid, pReady);
                }
            };
            room.Gamemanager.PlayerCancelReady += (sender, side) => {
                _connector.PushMsgByChannel(room.Channel, new PCReady() {
                    RoomID = room.ID, PlayerInfo = room.SearchInfoBySide(side)
                });
            };
            room.Gamemanager.PlayerChessMove += (sender, simplemove) =>
            {
                PMove pMove=new PMove()
                {
                    RoomID = room.ID,
                    SMInfo= simplemove,
                    PlayerInfo = room.SearchInfoBySide(simplemove.Side),
                };
                _connector.PushMsgByChannel(room.Channel, pMove);
            };
            room.Gamemanager.SideNext += (sender, side) =>
            {
                SideNext sideNext = new SideNext()
                {
                    RoomID = room.ID,
                    Steps = room.Gamemanager.Steps,
                    PlayerInfo = room.SearchInfoBySide(side)
                };
                _connector.PushMsgByChannel(room.Channel, sideNext);
            };
            room.Gamemanager.PlayerSiLingDied += (sender, chelist) =>
            {
                PSiLingDied pSiLingDied = new PSiLingDied()
                {
                    RoomID = room.ID,
                    PlayerInfo = room.SearchInfoBySide(chelist[0].chess.Side),
                    chessInfo = chelist
                };
                _connector.PushMsgByChannel(room.Channel, pSiLingDied);
            };
            room.Gamemanager.PlayerDie += (sender, side) =>
            {
                PDie pDie = new PDie()
                {
                    RoomID = room.ID,
                    Player = room.SearchInfoBySide(side),
                };
                _connector.PushMsgByChannel(room.Channel, pDie);
            };
            room.Gamemanager.PlayerMoveSkip+=(sender, side)=>{
                _connector.PushMsgByChannel(room.Channel, new PSkip()
                {
                    RoomID =room.ID,
                    PlayerInfo =room.SearchInfoBySide(side)
                });
            };

            //扩展功能
            room.Gamemanager.ExpireDraw += (sender, record) =>
            {
                ExpireDraw expireDraw = new ExpireDraw()
                {
                    RoomID = room.ID,
                    DRecord = record
                };
                _connector.PushMsgByChannel(room.Channel, expireDraw);
            };

            //游戏状态
            room.Gamemanager.GameLayouting += (sender, e) => {
                _connector.PushMsgByChannel(room.Channel, new GameLayouting() { RoomID = room.ID });
            };
            room.Gamemanager.GameStart += (sender, e) =>
            {
                _connector.PushMsgByChannel(room.Channel, new GameStart() { RoomID = room.ID, LayoutDic = e });
            };
            room.Gamemanager.GameOver += (sender, e) =>
            {
                _connector.PushMsgByChannel(room.Channel, new GameOver() { RoomID = room.ID, GResult = e });
                //更新房间中的玩家列表
                foreach (var item in room.Gamemanager.PlayersInfo)
                {
                    if (item.Status==PlayerStatus.Eixted||
                    item.Status == PlayerStatus.LostConnection||
                    item.Status == PlayerStatus.ForceEixted)
                    {
                        room.PlayersInfo.RemoveAll((info) => info.Side == item.Side);
                    }
                }

            };
            room.Gamemanager.GameClose += (sender, e) =>
            {
                //游戏关闭则时删除游戏、房间等资源
                //更新回话状态
                _connector.Sessions.ForEach((session) =>
                {
                    if (session.RoomID == room.ID)
                    {
                        session.RoomID = 0;
                    }
                });
                //移除房间
                _gameRooms.Remove(room);
            };

        }
        /// <summary>
        /// 根据房间ID搜索房间，找不到返回Room类型的默认值(null)
        /// </summary>
        /// <param name="RoomID"></param>
        /// <returns></returns>
        private Room _searchRoomByID (UInt64 RoomID)
        {
            return _gameRooms.Find((room) => room.ID == RoomID);
        }
        /// <summary>
        /// 处理玩家的准备请求，成功则通过广播推送给全部玩家，失败则单独响应GetquestError消息
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data"></param>
        private void _onReady(Session session,Ready data)
        {
            if (!_roomFilter(session, data.RoomID, out Room room,out Room.PlayerInfo playerInfo))
                return;
            //至此，玩家在指定的房间中
            try
            {
                room.Gamemanager.Ready(playerInfo.Side, data.CheLayout);
            }
            catch (GameManagerCodeException)
            {
                //log....
                throw;
            }
            catch (Exception e)
            {
                _connector.SendDataAsync(session, new GetquestError() { ClientMsgType = "Ready", ErrorInfo = e.Message });
                //log....
            }

        }
        private void _onCReady(Session session, CReady data)
        {

        }

        private void _onMove(Session session,Move data)
        {
            if (!_roomFilter(session, data.RoomID,out Room room, out Room.PlayerInfo playerInfo))//玩家不在此房间中
                return;
            try
            {
                room.Gamemanager.Move(playerInfo.Side, data.ChessCoord, data.TargetCoord);
                return;
            }
            catch (Exception e)
            {
                _connector.SendDataAsync(session, new GetquestError() { ClientMsgType = "行棋", ErrorInfo = e.Message });
                //log....
            }
        }

        private void _onExit(Session session, Exit data)
        {
            if (!_roomFilter(session, data.RoomID, out Room room, out Room.PlayerInfo playerInfo))//玩家不在此房间中
                return;
            try
            {
                room.PlayerExit(session.UID);
            }
            catch (Exception e)
            {
                _connector.SendDataAsync(session, new GetquestError() { ClientMsgType = "退出", ErrorInfo = e.Message });
                //log....
            }
        }

        /// <summary>
        /// 玩家是否在(包括强退、游戏中正常离开)指定房间中的过滤器。若玩家不在房间中，发送信息给客户端后返回false。若存在返回true
        /// </summary>
        /// <param name="session"></param>
        /// <param name="roomid">请求消息中的roomid</param>
        private bool _roomFilter(Session session,UInt64 roomid,out Room room,out Room.PlayerInfo playerInfo)
        {
            room = default(Room);
            playerInfo= default(Room.PlayerInfo);
            if (!LoginFilter(session, "游戏准备"))//登录过滤
                return false;
            //房间过滤
            if (session.RoomID != roomid)
            {
                _connector.SendDataAsync(session, new GetquestError()
                {
                    Code = 104,
                });
                return false;
            }
            room = _searchRoomByID(roomid);
            if (room == default(Room))
            {
                _connector.SendDataAsync(session, new GetquestError() { Code=103, ErrorInfo = "找不到该房间" });
                return false;
            }
            playerInfo = room.SearchInfoByUID(session.UID);
            if (playerInfo == default(Room.PlayerInfo))
            {
                _connector.SendDataAsync(session, new GetquestError() { Code=104, ErrorInfo = "该玩家不再此房间中" });
                return false;
            }
            return true;
        }
        #endregion

        
    }
}
