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
        /// 进入游戏房间，随机分配座位,更新广播频道、玩家列表
        /// </summary>
        /// <exception cref="RoomException"></exception>
        /// <exception cref="GameManagerException"></exception>
        public OfSide PlayerEnter(ulong uid)
        {
            int index = PlayersInfo.FindIndex((info) => { return info.UID == uid; });
            if (index != -1)//游戏列表中有此玩家
                throw new RoomException("请退出后再进入房间");
            OfSide side = Gamemanager.Enter();
            PlayersInfo.Add(new PlayerInfo(uid, side));
            Channel.UIDs.Add(uid);
            return side;
        }
        /// <summary>
        /// 退出游戏房间，更新广播频道、玩家列表(游戏进行中不更新)
        /// </summary>
        /// <exception cref="RoomException"></exception>
        /// <exception cref="GameManagerCodeException"></exception>
        public GameManager.ExitWay PlayerExit(ulong uid)
        {
            int index= PlayersInfo.FindIndex((info) => { return info.UID == uid; });
            
            if (index!=-1)
            {
                try
                {
                    OfSide side = PlayersInfo[index].Side;
                    GameManager.ExitWay exit = Gamemanager.Exit(side);
                    Channel.UIDs.Remove(uid);
                    if (Gamemanager.Status != GameStatus.Doing)
                        PlayersInfo.RemoveAt(index);
                    return exit;
                }
                catch(GameManagerCodeException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw new  RoomException(e.Message);
                }
                
            }
            else throw new RoomException("玩家不在房间中，退出房间失败");
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
    }
    public class GameServer
    {
        private Connector _connector;     //管理连接的模块

        private DBAcess _dataBaseAccess;//数据库访问模块

        public List<MatchingInfo> _matchingList;

        private List<Room> _gameRooms;//游戏房间列表


        public GameServer(IPAddress iPAddress,int port=8080)
        {
            _connector = new Connector(iPAddress, port);
            _dataBaseAccess = new DBAcess();
            _matchingList = new List<MatchingInfo>();
            _gameRooms = new List<Room>();
            _connector.SessionClose += _onSessionClose;
            _connector.MsgReceived += _onMsgReceived;
        }
        public void StartUp()
        {
            _dataBaseAccess.Open();
            _connector.RunServerAsync();

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
                if (session.RoomID!=0)//玩家在房间内
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
                    catch (RoomException exc )//不在房间中
                    {
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
        #region 账号登录、注册、注销模块
        /// <summary>
        /// 玩家登录请求的处理方法
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data"></param>
        private void _onLogin(Session session, LoginIn data)
        {
            if (_dataBaseAccess.AuthenAccount(data.UserName, data.Password, out ulong UID))
            {
                _connector.SessionBind(session,UID);
                _connector.SendDataAsync(session, new LoginInfo() { IsLogin = true,PlayerID=UID, Info = "账号登录成功！" });
            }
            else _connector.SendDataAsync(session,new LoginInfo() { IsLogin=false,Info="账号不存在或密码错误!"});
        }
        /// <summary>
        /// 连接器主动关闭或被动关闭会话时的处理
        /// </summary>
        /// <param name="session"></param>
        private void _onSessionClose(Session session)
        {

        }
        #endregion
        
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
                
            MatchingInfo matchingInfo = new MatchingInfo() { Mode = match.GameMode, UID = session.UID };
            _matchingList.Remove(matchingInfo);//总是剔除该玩家已有的匹配请求
            _matchingList.Add(matchingInfo);
            MatchInfo matchInfo;
            if (_tryMatchGame(out UInt64[] uids, out Room room))
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
                _matchingList.RemoveAll((macthing)=> {//对于每个在匹配表中的玩家
                    foreach (var uid in uids)
                    {
                        if (uid== macthing.UID)//是已经匹配房间的玩家
                            return true;
                    }
                    return false;
                });
                //设置会话中的RoomID
                _connector.Sessions.ForEach((se)=> {
                    foreach (var uid in uids)
                    {
                        if (uid==se.UID)
                        {
                            se.RoomID = room.ID;
                            return;
                        }
                    }
                });
                //广播匹配结果
                _connector.PushMsgByChannel(room.Channel, matchInfo);
                //开启游戏房间内的事件广播
                SubscriptGameEvent(room);
                return;
            }
            else //未匹配到游戏
            {
                matchInfo = new MatchInfo() { HasAGame = false };
                _connector.SendDataAsync(session, matchInfo);
                return;
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
                _connector.SendDataAsync(session, new CancelMatchInfo() { IsCancel=false,Info= "该玩家没有匹配请求或已进入游戏" });
            }
            else
                _connector.SendDataAsync(session, new CancelMatchInfo() { IsCancel = true, Info = "已取消" });
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
            if (!LoginFilter(session, "获取游戏信息"))
                return;
            if (!_roomFilter(session, req.RoomID))
                return;
            //搜索游戏房间
            Room room= _searchRoomByID(req.RoomID);
            if (room==default(Room))
            {
                _connector.SendDataAsync(session, new GetquestError()
                {Code=103,ClientMsgType="获取游戏信息",ErrorInfo="游戏房间不存在"
                });
                return;
            }
            OfSide side = room.SearchInfoByUID(session.UID).Side;
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
        /// 订阅游戏管理者的事件（调用其方法时无法确定会发生的），广播给处在频道中的所有玩家。玩家进出游戏除外，此由房间类来广播
        /// </summary>
        /// <remark>这样的事件有：玩家死亡、和棋超时、玩家司令阵亡、游戏开始、游戏结束、轮到下一方行棋</remark>
        private void SubscriptGameEvent(Room room)
        {
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
            room.Gamemanager.PlayerDie += (sender, side) =>
            {
                PDie pDie = new PDie()
                {
                    RoomID = room.ID,
                    Player = room.SearchInfoBySide(side),
                };
                _connector.PushMsgByChannel(room.Channel, pDie);
            };

            room.Gamemanager.ExpireDraw += (sender, record) =>
            {
                ExpireDraw expireDraw = new ExpireDraw()
                {
                    RoomID = room.ID,
                    DRecord = record
                };
                _connector.PushMsgByChannel(room.Channel, expireDraw);
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
            room.Gamemanager.GameStart += (sender, e) =>
            {
                _connector.PushMsgByChannel(room.Channel, new GameStart() { RoomID = room.ID, LayoutDic = e });
            };
            room.Gamemanager.GameOver += (sender, e) =>
            {
                _connector.PushMsgByChannel(room.Channel, new GameOver() { RoomID = room.ID, GResult = e });
                //游戏结束则删除游戏、房间等资源
                _gameRooms.Remove(room);
                _connector.Sessions.ForEach((session) =>
                {
                    foreach (var info in room.PlayersInfo)
                    {
                        if(session.UID==info.UID)
                        {
                            session.RoomID = 0;
                            return;
                        }
                    }
                });
                room = null;

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
            if(!LoginFilter(session,"游戏准备"))
                return;
            if (!_roomFilter(session,data.RoomID))
                return;
            try
            {
                Room room = _searchRoomByID(data.RoomID);
                if (room!=default(Room))
                {
                    Room.PlayerInfo fromInfo=  room.SearchInfoByUID(session.UID);
                    if (fromInfo != default(Room.PlayerInfo))
                    {
                        room.Gamemanager.Ready(fromInfo.Side, data.CheLayout);
                        foreach (var uid in room.Channel.UIDs)
                        {
                            Room.PlayerInfo toInfo= room.SearchInfoByUID(uid);
                            int[,] layout= GameManager.FuzzifyLayout(data.CheLayout, fromInfo.Side, toInfo.Side, room.Gamemanager.Mode);
                            PReady pReady = new PReady()
                            {
                                RoomID = room.ID,
                                CheLayout = layout,
                                PlayerInfo = fromInfo
                            };
                           _connector.PushMsgByUID(uid, pReady);
                        }
                    }
                    
                }
                _connector.SendDataAsync(session, new GetquestError() { ClientMsgType = "Ready", ErrorInfo = "找不到该房间" });
            }
            catch(GameManagerException e)
            {
                _connector.SendDataAsync(session, new GetquestError() { ClientMsgType = "Ready", ErrorInfo = e.Message });
                //log....
            }
        }

        private void _onMove(Session session,Move data)
        {
            if (!LoginFilter(session, "行棋"))
                return;
            if (!_roomFilter(session, data.RoomID))//玩家不在此房间中
                return;
            Room room = _searchRoomByID(session.RoomID);
            Room.PlayerInfo playerInfo = room.SearchInfoByUID(session.UID);
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
        /// <summary>
        /// 玩家在房间中的过滤器。若玩家不在房间中，发送信息给客户端后返回false。若存在返回true
        /// </summary>
        /// <param name="session"></param>
        private bool _roomFilter(Session session,UInt64 roomid)
        {
            if (session.RoomID != roomid)
            {
                _connector.SendDataAsync(session, new GetquestError()
                {
                    Code = 104,
                });
                return false;
            }
            return true;
        }
        #endregion

        /// <summary>
        /// 登录检测，未登录会发送信息给客户端
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private bool LoginFilter(Session session,string msgType)
        {
            bool b = session.IsLogin;
            if (!b)
            {
                _connector.SendDataAsync(session, new GetquestError() {Code=101, ClientMsgType = msgType, ErrorInfo = "请先登录" });
            }
            return b;
        }
    }
}
