using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 服务器端的游戏逻辑展现者。它有太多与游戏管理者的重复代码了？？还是暂时不写了
    /// </summary>
    /// <remarks>
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
        
        public void OnGameManagerEvent()
        {

        }
        #endregion
    }
}
