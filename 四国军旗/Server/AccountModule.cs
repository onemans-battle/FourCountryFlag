using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetConnector;
using NetConnector.MsgType;
namespace Server
{
    public partial class GameServer
    {
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
                Session presession = _connector.Sessions.Find((s) => { return s.UID == UID; });
                if (presession != default(Session))
                    _connector.SendDataAsync(session, new ForceOffline() {Info="有玩家现在登陆了此帐号，密码可能已泄露，请尽快修改" });

                _connector.SessionBind(session, UID);
                _connector.SendDataAsync(session, new LoginInfo() { IsLogin = true, PlayerID = UID, Info = "账号登录成功！" });
            }
            else _connector.SendDataAsync(session, new LoginInfo() { IsLogin = false, Info = "账号不存在或密码错误!" });
        }
        private void _onLoginOut(Session session, LoginOut data)
        {
            if (session.IsLogin)
            {
                _connector.SessionUnBind(session);
                _connector.SendDataAsync(session,new LoginOutInfo() { IsLoginOut=true, Info ="注销成功"});
            }
            else
            {
                _connector.SendDataAsync(session, new LoginOutInfo() { IsLoginOut = false, Info = "注销失败，还未登录" });
            }
        }

        /// <summary>
        /// 登录检测，未登录会发送信息给客户端
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private bool LoginFilter(Session session, string msgType="")
        {
            bool b = session.IsLogin;
            if (!b)
            {
                _connector.SendDataAsync(session, new GetquestError() { Code = 101, ClientMsgType = msgType, ErrorInfo = "请先登录" });
            }
            return b;
        }
        #endregion
    }
}
