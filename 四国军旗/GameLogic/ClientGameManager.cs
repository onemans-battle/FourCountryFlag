using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStruct;
namespace GameLogic
{

    public class ClientGameManager
    {

        public OfSide NowCanMoveSide;  //当前轮到的行棋方,每当改变为不同值时，激发SideNext事件
        public GameStatus Status {
            set
            {
                if (_status!=value)
                {
                    _status = value;
                    switch (_status)
                    {
                        case GameStatus.Layouting:
                            GameLayouting?.Invoke(this, EventArgs.Empty);
                            break;
                        case GameStatus.Doing:
                            GameStart?.Invoke(this, EventArgs.Empty);
                            break;
                        case GameStatus.Over:
                            GameOver?.Invoke(this, GameResult);
                            break;
                        case GameStatus.Closed:
                            GameClose?.Invoke(this, EventArgs.Empty);
                            break;
                    }
                }
            }
        }
        private GameStatus _status;
        System.Timers.Timer _moveTimer;//行棋定时器

        public const ushort FourPlayerMaxSteps = 500;//四人游戏中最大步数，大于则和棋
        public const ushort TwoPlayerMaxSteps = 250;//二人游戏中最大步数，大于则和棋
        public const ushort ChessMoveInterval = 30 * 1000;//每方的行棋时间，单位为毫秒
        public const ushort DrawInterval = 30 * 1000;//和棋最长持续时间，单位为毫秒
        public const int LostInterval = 5 * 60 * 1000;//掉线最长持续时间，单位为毫秒，超过自动投降
        public const ushort OfferDrwaMaxNum = 3;//每个玩家最大的请求和棋次数
        public const ushort SkipMaxNum = 5;//每个玩家跳过自己行棋的次数，超时也算在内

        //20个事件
        
        public event EventHandler<OfSide> PlayerEnter;                  //玩家进场
        public event EventHandler<OfSide> PlayerExit;                   //玩家退出
        public event EventHandler<PlayerForceExitEventArgs> PlayerForceExit;//玩家强制退出
        public event EventHandler<PlayerReadyEventArgs> PlayerReady;                  //玩家准备
        public event EventHandler<OfSide> PlayerCancelReady;            //玩家取消准备
        public event EventHandler<SimpleMoveInfo> PlayerChessMove;      //某个棋子移动了
        public event EventHandler<OfSide> PlayerMoveSkip;               //玩家跳过行棋
        public event EventHandler<OfSide> PlayerDie;                    //玩家死亡
        public event EventHandler<SurrenderEventArgs> PlayerSurrender;  //玩家投降
        public event EventHandler<OfSide> PlayerOfferDraw;              //玩家请求和棋
        public event EventHandler<OfSide> PlayerAgrreDraw;              //某玩家同意和棋
        public event EventHandler<PlayerRefuseDrawEventArgs> PlayerRefuseDraw;         //某玩家拒绝和棋
        public event EventHandler<DrawRecord> ExpireDraw;               //和棋超时
        public event EventHandler<ChessInfo[]> PlayerSiLingDied;        //某方的司令死亡
        public event EventHandler<OfSide> PlayerLost;                   //玩家掉线
        public event EventHandler<OfSide> PlayerReconnect;              //玩家重连
        public event EventHandler GameClose;                            //游戏关闭:释放此游戏管理者时激发
        //通过属性设置器间接激发的事件：
        public event EventHandler GameLayouting;
        public event EventHandler GameStart;            //开始游戏了，事件数据为布阵字典
        public event EventHandler<GameResult> GameOver; //游戏结束了
        public event EventHandler<OfSide> SideNext;     //轮到某方行棋了
        public GameResult GameResult;
    }


}
