//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Threading;
//using System.Net;
//using System.Net.Sockets;
//using System.Collections;
//namespace Net
//{
//    public class ServerMsgEventArgs
//    {
//        public readonly int ID;//表示客户端的ID，发送数据时指定ID
//        public readonly Role Role;//发送方
//        public readonly Type MsgType;//网络消息类型
//        public readonly object Data;//消息内容

//        public ServerMsgEventArgs(int id, Role role, Type type, object data)
//        {
//            ID = id;
//            Role = role;
//            MsgType = type;
//            Data = data;
//        }
//    }
//    public class Server
//    {
//        public event EventHandler<Exception> NetErrorHappen;
//        public event EventHandler<ServerMsgEventArgs> MsgReceived;

//        List<TcpClient> ClientList;
//        Coder coder;
//        TcpListener TcpListener;
//        static readonly Int32 lengthBytes = 4;//每个数据的开头32位（4字节byte）表示其承载的消息长度
//        Array[] bufferList;
//        Int32[] bufferDataLengthList;//缓存中有效数据长度,以字节byte为单位
        
        
//        public Server(IPAddress iP,int port,int backlog=100,int maxClient=500)
//        {
//            coder = new Coder(Role.Server);
//            bufferList = new Array[maxClient];
//            bufferDataLengthList = new Int32[maxClient];
//            for (int i = 0; i < maxClient; i++)
//            {
//                bufferList[i] = new byte[1024 * 10];
//            }
//            TcpListener = new TcpListener(iP,port);
//            TcpListener.Start(backlog);
//            ClientList = new List<TcpClient>(maxClient);
//            TcpListener.BeginAcceptTcpClient((ar) =>
//            {
//                OnConnectRequest(ar);
//            }, TcpListener);

//        }
//        private void OnConnectRequest(IAsyncResult ar)
//        {

//            TcpListener tcpListener = (TcpListener)ar.AsyncState;
//            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
//            if (ClientList.Count < 501)
//            {
//                int id = ClientList.Count;
//                ClientList.Add(tcpClient);
//                NetworkStream networkStream = tcpClient.GetStream();
//                networkStream.BeginRead((byte[])bufferList[id], 0, bufferList[id].Length, OnRceiveData, id);
//            }
//            else
//            {
//                tcpClient.Close();
//            }
//            tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnConnectRequest), tcpListener);
//        }

//        public void AsyncSendData(int id,object data, AsyncCallback asyncCallback)
//        {
//            byte[] dataBuffer = coder.Encode(data);
//            byte[] length = BitConverter.GetBytes((Int32)dataBuffer.Length);
//            byte[] buffer = length.Concat(dataBuffer).ToArray();
//            NetworkStream networkStream = ClientList[id].GetStream();
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
//                }, ClientList[0].GetStream()).AsyncWaitHandle.WaitOne(1000);
//            //发送超时
//            if (!result) throw new Exception("发送超时");
//        }

//        //解析buffer的数据：是否构成至少一条消息？是的话是哪种类型的消息？解析成功激发事件
//        private void OnRceiveData(IAsyncResult ar)
//        {
//            int id = (int)ar.AsyncState;
//            NetworkStream networkStream = ClientList[id].GetStream();
//            byte[] buffer = (byte[])bufferList[id];
//            int bufferDataLength = bufferDataLengthList[id];
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
//                    id);
//                return;
//            }

//            //成功接收到数据
//            bufferDataLength += length;//更新缓冲区中表示有效数据长度的变量
//            //开始解析
//            while (true)
//            {
//                Int32 theMsgLength= BitConverter.ToInt32(buffer, pointer);
//                //判断buffer里的数据是否可以形成至少一条消息
//                if ((bufferDataLength - pointer)>= theMsgLength+ lengthBytes)
//                {
//                    pointer += lengthBytes;
//                    Array.ConstrainedCopy(buffer, pointer, tem = new byte[theMsgLength], 0, theMsgLength);
//                    //根据长度信息尝试解码消息，失败清空缓冲区，并继续接收数据
//                    try {
//                        object data = coder.Decode(tem);
//                        MsgReceived?.Invoke(this, new ServerMsgEventArgs(id, Role.Client, data.GetType(), data));
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
//                            id);
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
//                buffer, bufferDataLength, buffer.Length- bufferDataLength,
//                OnRceiveData, 
//                id);

//        }
//    }
//}
