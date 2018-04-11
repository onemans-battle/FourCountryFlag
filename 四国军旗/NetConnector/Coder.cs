using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Reflection;
using NetConnector.MsgType;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;


namespace NetConnector
{



    /*消息的编码方式：
     * 把消息和其元信息封装成NetMsg对象，
     * 再Json化NetMsg对象为string，
     * 最后使用UTF-8方式编码为二进制序列
     */
    public enum Role:byte
    {
        Server=0,
        Client=1
    }
    

    public struct NetMsg
    {
        public Role Role;//发送方
        public String MsgTypeName;//网络消息类型
        public object Data;//消息内容
    }
    public class Coder//网络信息编解码器
    {
        public readonly Role Role;
        public readonly string NSOFMsg;
        public readonly Type[] MsgTypeList;//支持编解码的消息的类型信息
        //NSOFMsg为需编解码的消息结构体定义所在的命名空间,此命名空间中只能消息结构体和其枚举值（排除的消息）
        public Coder(Role role, string nSOFMsg = "NetConnector.MsgType")
        {
            Role = role;
            NSOFMsg = nSOFMsg;
            MsgTypeList = getAllMsgTypes(NSOFMsg);
        }
        //编码消息
        public byte[] Encode(object data)
        {
            //检测是否是已定义的消息类型
            Type dTType = data.GetType();
            bool exist = false;
            foreach (var type in MsgTypeList)
            {
                if (type == dTType)
                {
                    exist = true;
                    break;
                }
            }
            if (!exist) throw new NetCoderException("编码错误：不支持此种消息的编码");

            //给消息内容加上元信息封装成NetMsg:
            NetMsg netMsg = new NetMsg() { Data=data,MsgTypeName=dTType.Name,Role=this.Role};

            //json化NetMsg对象
            string json = JsonConvert.SerializeObject(netMsg);
            return Encoding.UTF8.GetBytes(json);
        }


        public object Decode(byte[] buffer)
        {
            //解码为Json对象
            string json=Encoding.UTF8.GetString(buffer);
            JObject jObject = JObject.Parse(json);

            //解析消息的元信息
            Role role = (Role)(byte)jObject["Role"];
            string msgTypeName = (string)jObject["MsgTypeName"];
            //根据元信息设置消息的类型
            Type dataType;
            switch (role)
            {
                case Role.Server:
                    
                case Role.Client:
                    {
                        bool flag = false;
                        foreach (var item in MsgTypeList) //检索MsgTypeList看看是否支持此类型消息的解码
                        {
                            if (item.Name == msgTypeName) flag = true;
                        }
                        if(flag==false) throw new NetCoderException("解码错误：无法其中的识别MsgTypeName信息");
                    }
                    break;
                default:throw new NetCoderException("解码错误：无法其中的识别Role信息");
                    
            }
            dataType = Type.GetType(NSOFMsg + '.' + msgTypeName);
            return JsonConvert.DeserializeObject(jObject["Data"].ToString(), dataType);
        }


        //获取需要编解码的结构体类型全体
        private Type[] getAllMsgTypes(string nSOFMsg)
        {

            List<Type> tem = new List<Type>(30);
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.Namespace == nSOFMsg && !type.IsEnum)
                    tem.Add(type);
            }
            return tem.ToArray();
        }

        
    }
   
}
