using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 服务器端的游戏逻辑展现者。
    /// </summary>
    /// <remarks>
    /// 为UI隐藏网络的存在、接收用户游戏请求、呈现游戏服务器的响应、游戏变化
    /// 导致游戏进程变化的逻辑由游戏管理者完成并对它“发起调用”，
    /// 它仅是对管理者的呈现、尽可能地给玩家提供辅助功能。
    /// 它只有少量的不影响游戏进程的逻辑：为玩家寻路、。
    /// </remarks>
    public class GameAgent
    {
        public GameStatus Status;
        public OfSide NowMoveSide;
        public CheckBroad CheckBroad;


        #region 游戏管理者的“调用”
        //private void _onMsgRecv(object sender, NetServerMsg netServerMsg)
        //{
        //    switch (netServerMsg.MsgType.Name)
        //    {
        //        case "LoginInfo":
        //            {
        //                LoginInfo loginInfo = (LoginInfo)netServerMsg.Data;
        //                if (loginInfo.IsLogin)
        //                {
        //                    myuid = loginInfo.PlayerID;
        //                    Login.Visibility = Visibility.Hidden;
        //                    MatchButton.IsEnabled = true;
        //                }
        //                else
        //                {
        //                    LoginInfoLable.Content = loginInfo.Info;
        //                }
        //                NetMsgTextBlock.Text += loginInfo.Info;
        //            }
        //            break;
        //        case "PReady":
        //            {
        //                PReady pr = (PReady)netServerMsg.Data;
        //                gameInfo.CB.Layout(CheckBroad.ConvertFromLayoutToCheInfo(pr.CheLayout, pr.PlayerInfo.Side));
        //            };
        //            break;
        //        case "MatchInfo":
        //            {
        //                MatchInfo matchInfo = (MatchInfo)netServerMsg.Data;
        //                if (matchInfo.HasAGame)
        //                {
        //                    MatchButton.IsEnabled = false;
        //                    roomid = matchInfo.RoomID;
        //                    foreach (var info in matchInfo.PlayerInfo)
        //                    {
        //                        if (info.UID == myuid)
        //                        {
        //                            myside = info.Side;
        //                            break;
        //                        }
        //                    }
        //                    gameInfo.Mysideinfo = new SideInfo(myside);
        //                    gameInfo.CB = new CheckBroad(matchInfo.GameMode);
        //                    gameStatus = GameStatus.Layouting;
        //                }
        //            }; break;
        //        case "GameLayouting":
        //            {
        //                GameLayouting gameLayouting = (GameLayouting)netServerMsg.Data;
        //                gameStatus = GameStatus.Layouting;
        //            }
        //            break;
        //        case "GetquestError":
        //            {
        //                GetquestError getquestError = (GetquestError)netServerMsg.Data;
        //                if (getquestError.Code == 101)
        //                    Login.Visibility = Visibility.Visible;
        //            }; break;
        //        case "GameStart":
        //            gameStatus = GameStatus.Doing;
        //            break;
        //        case "GameOver":
        //            GameOver gameOver = (GameOver)netServerMsg.Data;
        //            gameInfo.CB.ClearAllChesses();
        //            GameInfo.Content += "  游戏结束：胜利者是：" + gameOver.GResult.Winers + "\n";
        //            gameStatus = GameStatus.Over;
        //            break;
        //        case "SideNext":
        //            {
        //                SideNext sideNext = (SideNext)netServerMsg.Data;
        //                _onSideNext(sideNext);
        //            }; break;
        //        case "PMove":
        //            {
        //                PMove pmove = (PMove)netServerMsg.Data;
        //                gameInfo.CB.Move(pmove.SMInfo);
        //            }; break;
        //        default:
        //            break;
        //    }
        //    NetMsgTextBlock.Text += "  " + netServerMsg.MsgType.Name;
        //}

        public void OnGameManagerEvent()
        {

        }
        #endregion
    }
}
