using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
namespace NetConnector
{
    /* 网络组件
     * 负责编码发送和接收解码在MsgType.cs中定义的结构体消息，将网络二进制数据打包成消息结构体通过激发事件通知给上层。
     * 网络发生错误也将激发网络错误事件。
     * 
     * 只对服务器和客户端进行身份验证来确保连接不被伪装，不关心由上层应用的逻辑所决定的身份，如玩家身份验证等（？？？）
     * 
     * 总之，它只负责××编解码××以下定义为结构体的数据，（并有限地确保连接不被窃听、伪装？？？：未实现）
     * 作用是让上层忽略网络流的编码复杂性，忽略网络连接的细节
     * 
     */

    //负责管控连接，激发网络事件通知订阅者
    //每个数据的开头32位（4字节byte）表示其承载的消息长度

    public class NetServerMsg : System.EventArgs
    {
        public readonly Role Role;//发送方
        public readonly Type MsgType;//网络消息类型
        public readonly object Data;//消息内容

        public NetServerMsg(Role role, Type type, object data)
        {
            Role = role;
            MsgType = type;
            Data = data;
        }
    }
    public class GameTCPClient
    {
        public event EventHandler<NetServerMsg> NetMsgRev;
        public event EventHandler<Exception> ConnectClosed;
        public TcpClient TcpClient { get; set; }//获取或设置基础支持的TcpClient


        static Coder coder=new Coder(Role.Client);
        byte[] buffer = new byte[1024*100];//10kb缓存
        Int32 bufferDataLength = 0;//缓存中有效数据长度,以字节byte为单位
        static readonly Int32 mataDataLength = 4;//每个数据的开头32位（4字节byte）表示其承载的消息长度


        public GameTCPClient()
        {
            TcpClient = new TcpClient()
            {
                NoDelay = true,
                ReceiveBufferSize = buffer.Length,
                //SendTimeout = 1500,
                //ReceiveTimeout=3000,
                
            };
            //TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

        }
        public void Close()
        {
            TcpClient.Close();
        }
        public void StartUp(IPAddress ip, int port=8080)
        {
            TcpClient.ConnectAsync(ip, port).Wait();
            StartReadMsgAsync();
        }
        //包装TCPClient的异步发送任务。
        public void SendAsync(object data)
        {
            byte[] dataBuffer = coder.Encode(data);
            byte[] length = BitConverter.GetBytes((Int32)dataBuffer.Length);
            byte[] buffer = length.Concat(dataBuffer).ToArray();
            try
            {
                NetworkStream networkStream = TcpClient.GetStream();
                networkStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception)
            {
                Close();
            }
        }

        //不断地异步读取网络数据，解析成NetServerMsg对象通过事件通知。直到连接关闭，退出此任务。
        //解码错误清空缓冲区并继续读取
        private async void StartReadMsgAsync()
        {
            try
            {
                while (TcpClient.Connected)
                {
                    NetworkStream networkStream = TcpClient.GetStream();
                    
                    //此次从网络数据流中读取到的数据长度
                    int length = await networkStream.ReadAsync(buffer, bufferDataLength, buffer.Length - bufferDataLength);
                    //若读到了流的末尾（关闭了连接？？）
                    if (length == 0)
                    {
                        ConnectClosed?.Invoke(this, new Exception("从网络中读取信息错误：连接已关闭"));
                        Close();
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
                        NetMsgRev?.Invoke(this, new NetServerMsg(Role.Server, data.GetType(), data));

                        pointer += theMsgLength;
                        if (bufferDataLength == pointer) break;
                    }
                    //清理缓存中已处理的数据
                    if (pointer== bufferDataLength)
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
            catch (Newtonsoft.Json.JsonException )//信息解码错误
            {
                bufferDataLength = 0;//"清空"缓冲区
                StartReadMsgAsync();
                return;
            }
            catch (NetCoderException)//自定义的解码错误
            {
                bufferDataLength = 0;//"清空"缓冲区
                StartReadMsgAsync();
                return;
            }
            catch (System.ObjectDisposedException e)//已关闭连接
            {
                bufferDataLength = 0;//"清空"缓冲区
                ConnectClosed?.Invoke(this, e);
                Close();
            }
            catch (System.IO.IOException e)
            {
                if (e.InnerException is SocketException)
                {
                    bufferDataLength = 0;//"清空"缓冲区
                    ConnectClosed?.Invoke(this, e);
                    Close();
                }
                else throw e;
            }
            catch(InvalidOperationException e)
            {
                ConnectClosed?.Invoke(this, e);
                Close();
                return;
            }
           
           
        }
    }
}
