using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using NetConnector.MsgType;
using System.Net.Sockets;
using System.Collections;
namespace NetConnector
{
    public class NetClientMsgEventArgs
    {
        public readonly Session Session;
        public readonly Role Role;//发送方
        public readonly Type MsgType;//网络消息类型
        public readonly object Data;//消息内容

        public NetClientMsgEventArgs(Session session, Role role, Type type, object data)
        {
            Session = session;
            Role = role;
            MsgType = type;
            Data = data;
        }
    }
    /// <summary>
    /// 客户端连接的抽象
    /// </summary>
    public class Session
    {
        private static int counter = 0;
        public readonly int ID;
        public UInt64 UID;//未登录为0
        public ulong RoomID;//玩家当前所在的房间，强退等都会置为0
        /// <summary>
        /// 暂时设置为公开，用于服务器控制台测试
        /// </summary>
        public TcpClient TcpClient;

        public bool IsLogin
        {
            get
            {
                return UID != 0;
            }
        }

        public Session(TcpClient tcpClient,UInt64 uid=0)
        {
            ID = ++counter;
            TcpClient = tcpClient;
            UID = uid;
            RoomID = 0;
        }
        internal void Close()
        {
            TcpClient.Close();
        }


    }
    public class Channel
    {
        public readonly List<UInt64> UIDs;
        public Channel()
        {
            UIDs = new List<ulong>(4);
        }
        public Channel(UInt64[] uids)
        {
            UIDs = new List<ulong>(uids);
        }
    }
    public class Connector
    {
        public event EventHandler<NetClientMsgEventArgs> MsgReceived;
        public event EventHandler<Session> SessiontAccept;//暂时保留
        public event EventHandler<Session> SessionClose;
        public event EventHandler ConnectorClose;
        public List<Session> Sessions;

        private readonly int MaxClient;
        readonly int mataDataLength=4;//用以分包的数据的长度：表示一个包长度的字节数
        static Coder coder;
        TcpListener TcpListener;
        
        public Connector(IPAddress iP,int port,int maxClient=500)
        {
            coder = new Coder(Role.Server);
            TcpListener = new TcpListener(iP,port);
            Sessions = new List<Session>(100);
            MaxClient = maxClient;
        }
        public async void RunServerAsync(int backlog = 100)
        {
            TcpListener.Start(backlog);
            try
            {
                while (true)
                {
                    Accept(await TcpListener.AcceptTcpClientAsync());
                }  
            }
            catch(Exception e)
            {
                return;
            }
            finally
            {
                TcpListener.Stop();
            }
        }
        public void Close()
        {
            foreach (var s in Sessions)
            {
                s.Close();
            }
            Sessions.Clear();
            TcpListener.Stop();
        }

        //满载则发送消息并断开，空闲则保持此连接
        private void Accept(TcpClient client)
        {
            Session session = new Session( client);
            try
            {
                if (Sessions.Count <= MaxClient)
                {

                    Sessions.Add(session);
                    SessiontAccept?.Invoke(this, session);
                    //client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    StartReadMsgAsync(session);
                }
                else
                {
                    //发送满载消息..
                    SendDataAsync(session, new ServerError() { ErrorInfo = "服务器满载；请稍后再试" });
                    //关闭
                    CloseSeesion(session);
                }
            }
            catch (InvalidOperationException )
            {
                CloseSeesion(session);
            }
            
        }
        /// <summary>
        /// 开启某个会话中的数据接收，直到会话关闭。
        /// 解码错误释放该TcpClient资源
        /// </summary>
        /// <param name="session"></param>
        private async void StartReadMsgAsync(Session session)
        {
            int bufferDataLength = 0;
            byte[] buffer = new byte[1024 * 5];
            try
            {
                while (true)
                {
                    NetworkStream networkStream = session.TcpClient.GetStream();
                    //此次从网络数据流中读取到的数据长度
                    int length = await networkStream.ReadAsync(buffer, bufferDataLength, buffer.Length - bufferDataLength);
                    //若读到了流的末尾（被动关闭了连接？？）
                    if (length == 0)
                    {
                        CloseSeesion(session);
                        return;
                    }

                    bufferDataLength += length;//更新缓冲区中表示有效数据长度的变量
                    //开始解析
                    Int32 pointer = 0; byte[] tem;//pointer表示当前解析完的数据长度值
                    for (Int32 theMsgLength = BitConverter.ToInt32(buffer, pointer);
                    (bufferDataLength - pointer) >= theMsgLength + mataDataLength;//当buffer里未解析的数据还可以形成一条消息
                    theMsgLength = BitConverter.ToInt32(buffer, pointer))
                    {
                        pointer += mataDataLength;
                        Array.ConstrainedCopy(buffer, pointer, tem = new byte[theMsgLength], 0, theMsgLength);
                        //根据长度信息尝试解码消息，失败清空缓冲区，并继续接收数据
                        object data = coder.Decode(tem);
                        MsgReceived?.Invoke(this, new NetClientMsgEventArgs(session, Role.Server, data.GetType(), data));

                        pointer += theMsgLength;
                        if (bufferDataLength == pointer) break;
                    }
                    //清理缓存中已处理的数据
                    if (pointer == bufferDataLength)
                    {
                        bufferDataLength = 0;
                    }
                    else if (pointer !=0)
                    {
                        Array.Copy(buffer, pointer, buffer, 0, pointer);
                        bufferDataLength -= pointer;
                    }
                    
                }
            }
            catch (Newtonsoft.Json.JsonException)//信息解码错误
            {
                CloseSeesion(session);
                return;
            }
            catch (NetCoderException)
            {
                CloseSeesion(session);
                return;
            }
            catch (System.ObjectDisposedException e)//已关闭连接
            {
                CloseSeesion(session);
            }
            catch (System.IO.IOException e)//被动关闭连接？？
            {
                if (e.InnerException is SocketException)
                {
                    CloseSeesion(session);
                }
                else throw e;
            }
            catch (System.InvalidOperationException)
            {
                CloseSeesion(session);
                //throw;
            }

        }

        public void CloseSeesion(Session session) {
            
            if (Sessions.Remove(session))
            {
                SessionClose?.Invoke(this, session);
                session.Close();
            }
        }
        /// <summary>
        /// 根据频道中的玩家ID进行广播
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="data"></param>
        public void PushMsgByChannel(Channel channel, object data)
        {
            ulong[] uids=  channel.UIDs.ToArray();
            //给在频道中的客户端发送消息
            foreach (var session in Sessions)
            {
                foreach (var uid in uids)
                {
                    if (session.UID==uid)
                    {
                        SendDataAsync(session, data);
                        break;
                    }
                }
            }
        }

        public void PushMsgByUID(UInt64 uid,object data)
        {
            foreach (var session in Sessions)
            {
                if (session.UID == uid)
                {
                    SendDataAsync(session, data);
                    return;
                }
            }
        }
        /// <summary>
        /// 对回话进行响应
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data"></param>
        public void SendDataAsync(Session session, object data)
        {
            byte[] dataBuffer = coder.Encode(data);
            byte[] length = BitConverter.GetBytes((Int32)dataBuffer.Length);
            byte[] buffer = length.Concat(dataBuffer).ToArray();
            try
            {
                NetworkStream networkStream = session.TcpClient.GetStream();
                networkStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch(Exception)
            {
                CloseSeesion(session);
            }
        }
        /// <summary>
        /// 为会话绑定玩家，只允许同一玩家绑定一个会话，此绑定会先剔除已登录的玩家
        /// </summary>
        /// <param name="uid">玩家唯一标识码</param>
        /// <param name="data">默认值则不发送</param>
        public void SessionBind(Session session, UInt64 uid)
        {
            int index = Sessions.FindIndex((s) => { return s.UID == uid; });
            if (index != -1)
            {
                CloseSeesion(Sessions[index]);
            }
            session.UID = uid;
        }
        /// <summary>
        ///解除绑定。
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data"></param>
        public void SessionUnBind(Session session)
        {
            session.UID = 0;
        }
        public Session SearchSessionByUID(ulong uid)
        {
            return Sessions.Find(item => item.UID == uid);
        }
 
    }
}
