//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Linq;
//namespace Net
//{
//    /* 网络组件
//     * 负责编码发送和接收解码在MsgType.cs中定义的结构体消息，将网络二进制数据打包成消息结构体通过激发事件通知给上层。
//     * 网络发生错误也将激发网络错误事件。
//     * 
//     * 只对服务器和客户端进行身份验证来确保连接不被伪装，不关心由上层应用的逻辑所决定的身份，如玩家身份验证等（？？？）
//     * 
//     * 总之，它只负责××编解码××以下定义为结构体的数据，（并有限地确保连接不被窃听、伪装？？？：未实现）
//     * 作用是让上层忽略网络流的编码复杂性，忽略网络连接的细节
//     * 
//     */

//    //负责管控连接，激发网络事件通知订阅者
//    //每个数据的开头32位（4字节byte）表示其承载的消息长度

//    public class NetMsgEventArgs : System.EventArgs
//    {
//        public readonly Role Role;//发送方
//        public readonly Type MsgType;//网络消息类型
//        public readonly object Data;//消息内容

//        public NetMsgEventArgs(Role role, Type type, object data)
//        {
//            Role = role;
//            MsgType = type;
//            Data = data;
//        }
//    }
//    public class GameClient
//    {
//        public event EventHandler<NetMsgEventArgs> MsgReceived;//激发事件： MsgReceive?.Invoke(this, e);
//        public event EventHandler<Exception> ServerClosed;
//        public event EventHandler<Exception> ConnectError;
//        public event EventHandler<Exception> NetErrorHappen;
//        public TcpClient TcpClient { get; set; }//获取或设置基础支持的TcpClient
//        Coder coder;
//        byte[] buffer = new byte[1024*100];//10kb缓存
//        Int32 bufferDataLength = 0;//缓存中有效数据长度,以字节byte为单位
//        static readonly Int32 lengthBytes = 4;//每个数据的开头32位（4字节byte）表示其承载的消息长度
//        public GameClient()
//        {
            
//        }
//        //连接到服务器
//        public void ConnectTo(IPAddress iP, int port)
//        {
//            TcpClient?.Close();
//            TcpClient = new TcpClient() { NoDelay = true, ReceiveBufferSize = buffer.Length };
//            coder = new Coder(Role.Client);
//            var result= TcpClient.BeginConnect(iP, port,
//                   (ar) =>
//                   {
//                       try
//                       {
//                           TcpClient.EndConnect(ar);
//                           NetworkStream netStream= TcpClient.GetStream();
//                           netStream.BeginRead(buffer, 0, buffer.Length, OnRceiveData, netStream);
//                       }
//                       catch (Exception e)
//                       {
//                           ConnectError?.Invoke(this, e);
//                       }
//                   }, TcpClient).AsyncWaitHandle.WaitOne(1000);
//            if (!result) throw new Exception("连接超时");

//        }
//        //异步发送数据，成功发送回调asyncCallback委托
//        public void AsyncSendData(object data,AsyncCallback asyncCallback)
//        {

//            byte[] dataBuffer = coder.Encode(data);
//            byte[] length = BitConverter.GetBytes((Int32)dataBuffer.Length);
//            byte[] buffer = length.Concat(dataBuffer).ToArray();
//            NetworkStream networkStream= TcpClient.GetStream();
//            var result = networkStream.BeginWrite(buffer, 0, buffer.Length,
//                (ar) => {
//                    try
//                    {
//                        ((NetworkStream)ar.AsyncState).EndWrite(ar);
//                        asyncCallback?.Invoke(ar);
//                    }
//                    catch (Exception e)
//                    {
//                        throw e;
//                    }
//                }, networkStream).AsyncWaitHandle.WaitOne(1000);
//            if (!result) throw new Exception("发送数据连接超时");
//        }
//        public void Close()
//        {
//            TcpClient.Close();
//        }


//        //解析buffer的数据：是否构成至少一条消息？是的话是哪种类型的消息？解析成功激发事件
//        private void OnRceiveData(IAsyncResult ar)
//        {
//            NetworkStream networkStream = (NetworkStream)ar.AsyncState;
//            Int32 pointer = 0; byte[] tem;//pointer表示当前解析完的数据长度值
//            int length = 0;//此次从网络数据流中读取到的数据长度
//            try {
//                length= networkStream.EndRead(ar);
//            }catch(Exception e)
//            {
//                NetErrorHappen?.Invoke(this, e);
//                //继续接收数据
//                networkStream.BeginRead(
//                    buffer, bufferDataLength, buffer.Length - bufferDataLength,
//                    OnRceiveData,
//                    networkStream);
//                return;
//            }

//            //成功接收到数据
//            bufferDataLength += length;//更新缓冲区中表示有效数据长度的变量
//            //开始解析
//            while (true)
//            {
//                Int32 theMsgLength= BitConverter.ToInt32(buffer, pointer);
//                //判断buffer里的数据是否可以形成至少一条消息
//                if ((bufferDataLength- pointer)>= theMsgLength+ lengthBytes)
//                {
//                    pointer += lengthBytes;
//                    Array.ConstrainedCopy(buffer, pointer, tem = new byte[theMsgLength], 0, theMsgLength);
//                    //根据长度信息尝试解码消息，失败清空缓冲区，并继续接收数据
//                    try {
//                        object data = coder.Decode(tem);
//                        MsgReceived?.Invoke(this, new NetMsgEventArgs(Role.Server, data.GetType(), data));
//                        pointer += theMsgLength;
//                    }
//                    catch (Exception e){
//                        Array.Clear(buffer,0, bufferDataLength);
//                        bufferDataLength = 0;
//                        NetErrorHappen?.Invoke(this, e);
//                        //继续接收数据
//                        networkStream.BeginRead(
//                            buffer, bufferDataLength, buffer.Length - bufferDataLength,
//                            OnRceiveData,
//                            networkStream);
//                        return;
//                    }
//                }
//                else { break; }
//            }
//            //清理缓存中已处理的数据
//            if (pointer > 0)
//            {
//                Array.Copy(buffer, pointer, buffer, 0, pointer);
//                bufferDataLength -= pointer ;
//            }
                
//            //继续接收数据
//            networkStream.BeginRead(
//                buffer,bufferDataLength,buffer.Length- bufferDataLength,
//                OnRceiveData, 
//                networkStream);

//        }
//    }
//}
