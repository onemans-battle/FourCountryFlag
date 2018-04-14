using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetConnector.MsgType;
using NetConnector;
using GameLogic;
using System.Net;
using DataStruct;
namespace GameClient
{
    /// <summary>
    /// 游戏客户端的逻辑。UI只是它的外表呈现，以及对它相应的请求。
    /// </summary>
    /// <remarks>它底层使用游戏逻辑中的棋盘、客户端连接器</remarks>
    public class GameClient
    {
        public class MyRoom
        {
            public UInt64 ID;
            public readonly CheckBroad Cb;
            public readonly Dictionary<OfSide,PlayerInfo> PlayersInfo;
            public MyRoom()
            {
                Cb = new CheckBroad(GameMode.SiAn);
                PlayersInfo =new Dictionary<OfSide, PlayerInfo>(4);
            }
        }
        
        private GameTCPClient GameTCPClient;

        public PlayerInfo MyInfo;
        public bool IsLogin { get {return MyInfo.PlayerID != default(UInt64); } }
        public MyRoom Room;
        public GameClient()
        {
            GameTCPClient = new GameTCPClient();
            GameTCPClient.NetMsgRev += (sender, netmsg) => _onMsgRecv(netmsg);
            MyInfo = new PlayerInfo();
            Room = new MyRoom();
        }
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void StartUp(IPAddress ip, int port,GameMode mode)
        {
            GameTCPClient.StartUp(ip, port);
            Room.Cb.ChangeMode(mode);
        }
        //public Task Login(LoginIn loginIn)
        //{
        //    return GameTCPClient.SendAsync(loginIn);
        //}
        //public Task Match(Match match)
        //{
        //    return GameTCPClient.SendAsync(match);
        //}
        //public Task Move(Move move)
        //{
        //    return GameTCPClient.SendAsync(move);
        //}
        /// <summary>
        /// 处理服务器的响应或广播消息
        /// </summary>
        /// <param name="netmsg"></param>
        private void _onMsgRecv(NetServerMsg netmsg)
        {
            switch (netmsg.MsgType.Name)
            {
                case "LoginInfo":
                    _onLogin((LoginInfo)netmsg.Data);
                    break;
                case "MatchInfo":
                    _onMatchInfo((MatchInfo)netmsg.Data);
                    break;
                case "PlayerInfo":
                    _onPlayerInfo((PlayerInfo)netmsg.Data);
                    break;
                default:
                    break;
            }
        }
        public event EventHandler SuccessLogin;
        public event EventHandler FailLogin;
        private void _onLogin(LoginInfo loginInfo)
        {
            if (loginInfo.IsLogin)
            {
                MyInfo.PlayerID = loginInfo.PlayerID;
                SuccessLogin?.Invoke(this,EventArgs.Empty);
            }
            FailLogin?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler HasGame;
        public event EventHandler MatchCancel;
        private void _onMatchInfo(MatchInfo matchInfo)
        {
            if (matchInfo.HasAGame)
            {
                foreach (var item in matchInfo.PlayerInfo)
                {
                    Room.PlayersInfo.Add(item.Side, new PlayerInfo() { PlayerID = item.UID });
                    if (item.UID!=MyInfo.PlayerID)
                    {
                        GameTCPClient.SendAsync(new GetPlayerInfo() { PlayerID = item.UID });
                    }
                    
                }
                HasGame?.Invoke(this, EventArgs.Empty);

            }
            else if (matchInfo.HasCancel)
            {
                MatchCancel?.Invoke(this, EventArgs.Empty);
            }
        }

        private void _onPlayerInfo(PlayerInfo playerInfo)
        {
            if (playerInfo.PlayerID== MyInfo.PlayerID)
            {
                MyInfo = playerInfo;
            }
            else
            {
                Room.PlayersInfo.Map((side, dic)=>{
                   if(dic[side].PlayerID== playerInfo.PlayerID)
                    {
                        dic[side] = playerInfo;
                        return;
                    }
                });

            }
        }

        private void _onMoveChess(MoveInfo moveInfo)
        {

        }
    }
}
