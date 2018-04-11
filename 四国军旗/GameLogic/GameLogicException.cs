using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 由不符合游戏规则的操作所引发的异常。
    /// </summary>
    /// <remarks>如：非法的布阵、非法的行棋等</remarks>
    public class GameRuleException : Exception
    {
        public override string  Message{get;}
        public GameRuleException(string message) {
            Message = message;
        }
    }
    /// <summary>
    /// 由不符合游戏进程的操作所引发的异常。
    /// </summary>
    /// <remarks>如：正在进行游戏时，玩家请求布阵等</remarks>
    public class GameManagerException : Exception
    {
        public string Progress { get; }//游戏在哪个进程出错
        public override string Message { get; }
        public GameManagerException(string progress,string message )
        {
            Progress = progress;
            Message = message;
        }
    }
    /// <summary>
    /// 在游戏管理者类中单纯由代码所引起的逻辑错误，此种错误不应发送给客户端
    /// </summary>
    public class GameManagerCodeException : CodeException
    {
        public override string Path { get; }//在哪个路径出错
        public override string CodeMessage { get; }
        public GameManagerCodeException(string typeName, string codeMessage)
        {
            Path = typeName;
            CodeMessage = codeMessage;
        }
    }
    /// <summary>
    /// 各种代码错误类的基类
    /// </summary>
    public class CodeException : Exception
    {
        public virtual string Path { get; }
        public virtual string CodeMessage { get; }
        public CodeException()
        {
        }
        public CodeException(string typeName, string codeMessage)
        {
            Path = typeName;
            CodeMessage = codeMessage;
        }
    }
}
