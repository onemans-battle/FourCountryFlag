using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GameLogic;
using System.Globalization;
using NetConnector.MsgType;
using NetConnector;
using System.Net;
using Server;
using DataStruct;
namespace UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GameWindow : Window
    {
        Task<IPAddress[]> iPAddresses;
        IPAddress ipAddress;
        string domainName = "junqi.online"; 
        static int port;
        public class Game
        {
            public GameStatus GameStatus;
            public GameMode Mode;
            public ushort Step;
            public SideInfo Mysideinfo;
            public CheckBroad CB;
        }
        GameStatus gameStatus {//根据游戏状态改变按钮是否可见、开启
            set
            {
                Coord viewC;
                gameInfo.GameStatus = value;
                switch (gameInfo.GameStatus)
                {
                    case GameStatus.Layouting:
                        
                        ReadyButton.IsEnabled = true;
                        EixtButton.IsEnabled = true;
                        for (int i = 0; i < chessButton.GetLength(0); i++)
                        {
                            for (int j = 0; j < chessButton.GetLength(1); j++)
                            {
                                if(chessButton[i, j]!=null)
                                chessButton[i, j].IsEnabled = false;
                            }
                        }
                        ChessInfo[] chessInfos = gameInfo.CB.GetCurrentChesses(myside);
                        foreach (var item in chessInfos)
                        {
                            viewC = _reverseToViewC(item.coord, myside);
                            chessButton[viewC.i, viewC.j].IsEnabled = true;
                        }

                        break;
                    case GameStatus.Doing:
                        clickedB = null;
                        HasSecondTick_lable.Visibility = Visibility.Visible;
                        ReadyButton.IsEnabled = false;
                        SkipMoveButton.IsEnabled = true;
                        break;
                    case GameStatus.Over:
                        SurrenderButton.IsEnabled = false;
                        OfferDrawButton.IsEnabled = false;
                        ReadyButton.IsEnabled = false;
                        SkipMoveButton.IsEnabled = false;
                        HasSencondTimer.Stop();
                        HasSecondTick_lable.Visibility = Visibility.Hidden;
                        break;
                    case GameStatus.Closed:
                        SurrenderButton.IsEnabled = false;
                        OfferDrawButton.IsEnabled = false;
                        ReadyButton.IsEnabled = false;
                        SkipMoveButton.IsEnabled = false;
                        EixtButton.IsEnabled = false;
                        MatchButton.IsEnabled = true;
                        HasSencondTimer.Stop();
                        HasSecondTick_lable.Visibility = Visibility.Hidden;
                        break;
                    default:
                        break;
                }
            }
            get { return gameInfo.GameStatus; }
        }
        Game gameInfo = new Game();
        ushort step
        {
            set
            {
                if (gameInfo.Step != value)
                {
                    gameInfo.Step = value;
                    if (gameInfo.Step >= 50 && gameInfo.Mysideinfo.Status == PlayerStatus.Alive)
                    {
                        SurrenderButton.IsEnabled = true;
                        OfferDrawButton.IsEnabled = true;
                    }
                }
            }
            get
            {
                return gameInfo.Step;
            }
        }
        Coord startC {
            set
            { _startC = _exhangeToRealC(value, myside);}
            get
            { return _startC; }
        } //自动将ui坐标转为游戏棋盘坐标
        Coord endC{
            set
            { _endC = _exhangeToRealC(value, myside); }
            get { return _endC; }
        }//自动将ui坐标转为游戏棋盘坐标
        Coord _startC, _endC;
        Button clickedB=null;
        #region 测试代码
        GameTCPClient GameTCP;
        UInt64 roomid;
        UInt64 myuid;
        OfSide myside { set {
                _myside = value;
                CheckboardGrid_SetChessBingding();
            }
            get { return _myside; }
        }//我方势力、我方视角点
        OfSide _myside;
        Button[,] chessButton;

        System.Timers.Timer HasSencondTimer = new System.Timers.Timer(1000) { AutoReset=true};
        System.Timers.Timer ElapsedSencondTimer = new System.Timers.Timer(1000) { AutoReset = true };
        Action actionHasSencond;
        Action actionElapsed;
        int hasSencond;
        int elapsedSencond;
        #endregion
        public GameWindow()
        {
            InitializeComponent();
            CheckboardGrid_CreateChess();
            MatchButton.Tag = true;

            iPAddresses = Dns.GetHostAddressesAsync(domainName);

            actionHasSencond = () => {
                if (hasSencond != 0)
                {
                    hasSencond -= 1;
                    HasSecondTick_lable.Content = hasSencond;
                }
                else HasSencondTimer.Stop();
            };
            actionElapsed = () => {
                elapsedSencond += 1;
                int mi = elapsedSencond / 60, se = elapsedSencond % 60;
                if (mi > 0)
                {
                    HasSecondTick_lable.Content = mi + ":";
                    if (se < 10) HasSecondTick_lable.Content += "0" + se.ToString();
                    else HasSecondTick_lable.Content += se.ToString();
                }
                else HasSecondTick_lable.Content = se.ToString();
            };
            HasSencondTimer.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(actionHasSencond);
            };
           
            ElapsedSencondTimer.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(actionElapsed);
            };

        }
        private void _onMsgRecv(object sender, NetServerMsg netServerMsg)
        {
            switch (netServerMsg.MsgType.Name)
            {
                case "LoginInfo":
                    {
                        LoginInfo loginInfo=(LoginInfo)netServerMsg.Data;
                        if (loginInfo.IsLogin)
                        {
                            myuid = loginInfo.PlayerID;
                            LoginGrid.Visibility = Visibility.Hidden;
                            MatchButton.IsEnabled = true;
                        }
                        else
                        {
                            LoginInfoLable.Content = loginInfo.Info;
                        }
                    }
                    break;
                case "ForceOffline":
                    {
                        ForceOffline forceOffline = (ForceOffline)netServerMsg.Data;
                        LoginInfoLable.Content = forceOffline.Info;
                        gameStatus = GameStatus.Closed;
                        LoginGrid.Visibility = Visibility.Visible;
                        SurrenderButton.IsEnabled = false;
                        OfferDrawButton.IsEnabled = false;
                        ReadyButton.IsEnabled = false;
                        SkipMoveButton.IsEnabled = false;
                        EixtButton.IsEnabled = false;
                        MatchButton.Tag = true;
                        MatchButton.Content = "匹配";
                        gameInfo?.CB?.ClearAllChesses();
                        HasSencondTimer.Stop();
                        ElapsedSencondTimer.Stop();
                    }
                    break;
                case "PReady":
                    {
                        PReady pr = (PReady)netServerMsg.Data;
                        if (pr.layout!=default(int[,]))
                        {
                            gameInfo.CB.Layout(CheckBroad.ConvertFromLayoutToCheInfo(pr.layout, myside));
                        }
                        else
                            gameInfo.CB.Layout(CheckBroad.ConvertFromLayoutToCheInfo(CheckBroad.GetUnknowLayout(), pr.PlayerInfo.Side));
                        if (myside == pr.PlayerInfo.Side)
                            ReadyButton.IsEnabled = false;
                    };
                    break;
                case "MatchInfo":
                    {
                        //记录匹配的信息
                        MatchInfo matchInfo = (MatchInfo)netServerMsg.Data;
                        if (matchInfo.HasAGame)
                        {
                            HasSecondTick_lable.Visibility = Visibility.Hidden;
                            ElapsedSencondTimer.Stop();
                            MatchButton.IsEnabled = false;
                            MatchButton.Content = "匹配";
                            MatchButton.Tag = true;
                            roomid = matchInfo.RoomID;
                            gameInfo.Mode = matchInfo.GameMode;
                            gameInfo.CB = new CheckBroad(gameInfo.Mode);
                            foreach (var info in matchInfo.PlayerInfo)
                            {
                                if (info.UID==myuid)
                                {
                                    gameInfo.Mysideinfo = new SideInfo(info.Side);
                                    myside = info.Side;
                                    break;
                                }
                            }
                        }
                        else if (matchInfo.HasCancel)//匹配被服务器取消
                        {
                            HasSecondTick_lable.Content = string.Empty;
                            MatchButton.Tag = true;
                            MatchButton.Content = "匹配";
                            ElapsedSencondTimer.Stop();
                        }
                    }; break;
                case "GameLayouting":
                    {
                        GameLayouting gameLayouting=(GameLayouting)netServerMsg.Data;
                        ChessInfo[] chessInfos = CheckBroad.ConvertFromLayoutToCheInfo(CheckBroad.GetDefaultLayout(), myside);
                        gameInfo.CB.Layout(chessInfos);
                        gameStatus = GameStatus.Layouting;
                    }
                    break;
                case "GetquestError":
                    {
                        GetquestError getquestError = (GetquestError)netServerMsg.Data;
                        if(getquestError.Code==101)
                            LoginGrid.Visibility = Visibility.Visible;
                    };break;
                case "GameStart":
                    GameStart gameStart= (GameStart)netServerMsg.Data;
                    foreach (var layout in gameStart.LayoutDic)
                    {
                        gameInfo.CB.Layout(CheckBroad.ConvertFromLayoutToCheInfo(layout.Value, layout.Key));
                    }
                    gameStatus = GameStatus.Doing;
                    break;
                case "GameOver":
                    GameOver gameOver = (GameOver)netServerMsg.Data;
                    gameInfo.CB.ClearAllChesses();
                    try
                    {
                        GameInfo.Content = "游戏结束：胜利者是:";
                        foreach (var item in gameOver.GResult.Winers)
                        {
                            switch (item)
                            {
                                case OfSide.First:
                                    GameInfo.Content += "\n红色方";
                                    break;
                                case OfSide.Second:
                                    GameInfo.Content += "\n蓝色方";
                                    break;
                                case OfSide.Third:
                                    GameInfo.Content += "\n绿色方";
                                    break;
                                case OfSide.Fourth:
                                    GameInfo.Content += "\n色色方";
                                    break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        GameInfo.Content += "\n???";
                    }
                    gameStatus = GameStatus.Over;
                    break;
                case "GameClose":
                    gameInfo.CB.ClearAllChesses();
                    gameStatus = GameStatus.Closed;
                    break;
                case "SideNext":
                    {
                        SideNext sideNext = (SideNext)netServerMsg.Data;
                        _onSideNext(sideNext);
                    };
                    break;
                case "PMove":
                    {
                        PMove pmove = (PMove)netServerMsg.Data;
                        gameInfo.CB.Move(pmove.SMInfo);
                    };
                    break;
                case "PSiLingDied":
                    PSiLingDied pSiLingDied=(PSiLingDied)netServerMsg.Data;
                    foreach (var cheinfo in pSiLingDied.chessInfo)
                    {
                        if (gameInfo.CB.VertexDataM[cheinfo.coord.i, cheinfo.coord.j].ExistChess())
                        {
                            gameInfo.CB.VertexDataM[cheinfo.coord.i, cheinfo.coord.j].Chess = cheinfo.chess;
                        }
                    }
                    break;
                case "PDie":
                    {
                        PDie pDie = (PDie)netServerMsg.Data;
                        gameInfo.CB.ClearChessOf(pDie.Player.Side);
                    }
                    break;
                case "PForceExit":
                    {
                        PForceExit pForceExit= (PForceExit)netServerMsg.Data;
                        gameInfo.CB.ClearChessOf(pForceExit.PlayerInfo.Side);
                    }
                    break;
                case "PSurr":
                    {
                        PSurr pSurr = (PSurr)netServerMsg.Data;
                        gameInfo.CB.ClearChessOf(pSurr.PlayerInfo.Side);
                    }
                    break;
                default:
                    break;
            }
            NetMsgTextBlock.Text += "  "+netServerMsg.MsgType.Name;
            NetMsgScrollViewer.ScrollToEnd();
        }

        private void _onSideNext(SideNext sideNext)
        {
            ChessInfo[] chessInfos = gameInfo.CB.GetCurrentChesses(myside);
            if (sideNext.PlayerInfo.Side == myside)
            {
                clickedB = null;
                //开启我方棋子
                foreach (var cheinfo in chessInfos)
                {
                    Coord viewC = _reverseToViewC(cheinfo.coord,myside);
                    chessButton[viewC.i, viewC.j].IsEnabled = true;
                }
            }
            else//禁用我方棋子
            {
                foreach (var cheinfo in chessInfos)
                {
                    Coord viewC = _reverseToViewC(cheinfo.coord,myside);
                    chessButton[viewC.i, viewC.j].IsEnabled = false;
                }
            }
            int x=0, y=0;
            switch (sideNext.PlayerInfo.Side)//(15,12-1)(4,15)  ( 1-1,4 )(12-1,1-1)
            {
                case OfSide.First:
                    x = 15;y = 12;
                    break;
                case OfSide.Second:
                    x = 4; y = 15;
                    break;
                case OfSide.Third:
                    x = 1; y =4;
                    break;
                case OfSide.Fourth:
                    x = 12; y = 1;
                    break;
            }
            Coord viewCticklable = _reverseToViewC(new Coord(x, y),myside);
            if (viewCticklable.i == 15) viewCticklable.j-=1;
            else if (viewCticklable.i == 1) viewCticklable.i -=1;
            else if (viewCticklable.i == 12) { viewCticklable.i-=1; viewCticklable.j-=1; }
            Grid.SetRow(HasSecondTick_lable, viewCticklable.i);
            Grid.SetColumn(HasSecondTick_lable, viewCticklable.j);
            HasSecondTick_lable.Content = 30;
            hasSencond = 30;
            HasSencondTimer.Start();
            //激活当前窗口
            if (sideNext.PlayerInfo.Side==myside&&!IsActive)
            {
                Activate();
            }
        }
        private void CheckboardGrid_Loaded(object sender, RoutedEventArgs e)
        {
           
            
            
        }
        /// <summary>
        /// 将棋盘坐标顺时针旋转为UI坐标。
        /// </summary>
        /// <param name="c">游戏逻辑中的棋盘坐标</param>
        /// <param name="viewSide">当前所用视角</param>
        /// <returns></returns>
        /// <remarks>
        /// (ii,jj)为UI棋盘网格上的坐标,(i,j)为以棋盘坐标(8,8)为原点建立的坐标系中的点坐标
        /// 顺时针旋转90度: (i,j)-> (x,y)=(j,-i).转换到棋盘坐标系中为(jj,-ii+16)
        /// 顺时针旋转180即两次90旋转度: (i,j)-> (j,-i)-> (x,y)=(-i,-j) .最后(-ii+16,-jj+16)
        /// 同上，270度为(-jj+16,ii)
        /// </remarks>
        private static Coord _reverseToViewC(Coord c, OfSide viewSide)
        {
            Coord viewC = new Coord();
            switch (viewSide)
            {
                case OfSide.First:
                    return c;
                case OfSide.Second://转90度
                    viewC.i = c.j;
                    viewC.j = -c.i+16;
                    break;
                case OfSide.Third://
                    viewC.i = -c.i + 16;
                    viewC.j = -c.j + 16;
                    break;
                case OfSide.Fourth:
                    viewC.i = -c.j+16;
                    viewC.j = c.i;
                    break;
            }
            return viewC;
        }
        /// <summary>
        /// 将UI坐标逆时针转换为游戏逻辑中的棋盘坐标。从UI上对棋子有关的操作都需经过此层转换.
        /// </summary>
        /// <param name="c">UI坐标</param>
        /// <param name="viewSide">当前所用视角</param>
        /// <returns></returns>
        /// <remark>
        /// 它是转为UI坐标的逆变(顺时针)。
        /// 逆时针90度即顺时针270度为: (i,j)-> (x,y)=(-j,i),(-jj+16,ii)
        /// 同理，180度:(-i,-j),最后(-ii+16,-jj+16)
        /// 270度 (j,-i),最后(jj,-ii+16)
        /// </remark>
        private static Coord _exhangeToRealC(Coord c, OfSide viewSide)
        {
            Coord realC = new Coord();
            switch (viewSide)
            {
                case OfSide.First:
                    return c;
                case OfSide.Second://逆时针转90度
                    realC.i = -c.j+16;
                    realC.j = c.i;
                    break;
                case OfSide.Third://
                    realC.i = -c.i + 16;
                    realC.j = -c.j + 16;
                    break;
                case OfSide.Fourth:
                    realC.i = c.j;
                    realC.j = -c.i+16;
                    break;
            }
            return realC;
        }
        /// <summary>
        /// 匹配到游戏后，开始预先放置棋子按钮、
        /// </summary>
        private void CheckboardGrid_CreateChess()
        {
            chessButton = new Button[17, 17];
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 17; j++)//遍历UI棋盘上的每一顶点
                {
                    if (CheckBroad.VertexInfoM[i, j].Type == VertexType.None)
                        continue;
                    Button bt;
                    //预置棋子
                    bt = new Button()
                    { Style = (Style)FindResource("chessButtonTemplate"),
                        Margin = new Thickness(5),
                        FontWeight = FontWeights.Bold,
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                        Opacity = 0.0,
                        IsEnabled = false,
                    };
                    bt.Click += ChessButton_Click;//订阅事件
                    CheckboardGrid.Children.Add(bt);

                    Grid.SetRow(bt, i);
                    Grid.SetColumn(bt, j);
                    chessButton[i, j] = bt;
                }
            }

        }
        /// <summary>
        /// 以“我方的视角”创建与游戏逻辑中的棋盘数据关联
        /// </summary>
        private void CheckboardGrid_SetChessBingding()
        {
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 17; j++)
                {
                    if (chessButton[i, j]==null)
                    {
                        continue;
                    }
                    //该UI坐标对应的实际坐标
                    Coord realC = _exhangeToRealC(new Coord(i, j), myside);
                    //建立数据关联
                    Binding bChessName = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[realC.i, realC.j],
                        Converter = new ChessTypeTOStringConverter(),
                        Mode = BindingMode.OneWay
                    };
                    Binding bChessSide = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[realC.i, realC.j],
                        Converter = new ChessSideTOBoolColor(),
                        Mode = BindingMode.OneWay
                    };
                    Binding bChessExist = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[realC.i, realC.j],
                        Converter = new ChessExitTDoubleConverter(),
                        Mode = BindingMode.OneWay
                    }; 
                    chessButton[i,j].SetBinding(Button.ContentProperty, bChessName);
                    chessButton[i, j].SetBinding(Button.OpacityProperty, bChessExist);
                    chessButton[i, j].SetBinding(Button.BackgroundProperty, bChessSide);
                }
            }
        }
        //使用代码创建棋盘底座：为grid批量添加图形元素
        private void CheckboardGrid_Initialized(object sender, EventArgs e)
        {

            /*根据顶点类型信息创建棋盘顶点图形*/
            Shape shape;
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 17; j++)//遍历棋盘上的每一顶点
                {
                    switch ((int)CheckBroad.VertexInfoM[i,j].Type)//根据其类型
                    {
                        case 1:
                            shape = new Rectangle
                            {
                                Margin = new Thickness(6),
                                Style = (Style)FindResource("兵站")//应用样式
                            };
                            break;
                        case 2:
                            shape = new Rectangle
                            {
                                Margin = new Thickness(6),
                                Style = (Style)FindResource("行营")
                            };
                            break;
                        case 3:
                            shape = new Rectangle
                            {
                                Margin = new Thickness(10),
                                Style = (Style)FindResource("星宫")
                            };
                            break;
                        case 4:
                            shape = new Rectangle
                            {
                                Margin = new Thickness(6),
                                Style = (Style)FindResource("大本营")
                            };
                            break;
                        default: continue;
                    }
                    CheckboardGrid.Children.Add(shape);
                    Grid.SetRow(shape, i);
                    Grid.SetColumn(shape, j);
                }
            }

        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginInfoLable.Content = string.Empty;
            if (GameTCP==null|| !GameTCP.TcpClient.Connected)
            {
                GameTCP?.Close();
                GameTCP = new GameTCPClient();
                try
                {
                    if (ipAddress==default(IPAddress)&&port==default(int))
                    {
                        ipAddress = iPAddresses.Result[0];
                        port = 8080;
                    }
                    GameTCP.StartUp(ipAddress, port);
                    GameTCP.NetMsgRev += _onMsgRecv;
                    Action action = ()=>{
                        LoginGrid.Visibility = Visibility.Visible;
                    };
                    GameTCP.ConnectClosed += (ssdenr, ee) => {
                        Dispatcher.Invoke(action);
                    };
                }
                catch (Exception exc)
                {
                    LoginInfoLable.Content = "无法连接服务器，请重试";
                    return;
                }
            }
            LoginIn loginIn = new LoginIn()
            {
                UserName = UserNameComboBox.Text,
                Password = PasswordTextB.Password
            };
            GameTCP.SendAsync(loginIn);
        }

        private void MatchButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((bool)MatchButton.Tag)
            {
                case true://按的是“开始”匹配按钮
                    {
                        //设为取消匹配按钮
                        MatchButton.Tag = false;
                        MatchButton.Content = "取消匹配";
                        //发送匹配请求
                        GameMode mode = (GameMode)((ComboBoxItem)GameModeComBox.SelectedItem).Tag;
                        Match match = new Match() { GameMode = mode };
                        GameTCP.SendAsync(match);
                        //定时器
                        HasSecondTick_lable.Visibility = Visibility.Visible;
                        HasSecondTick_lable.Content = 0;
                        elapsedSencond = 0;
                        ElapsedSencondTimer.Start();
                        Grid.SetRow(HasSecondTick_lable, 0);
                        Grid.SetColumn(HasSecondTick_lable, 11);
                    }
                    break;
                case false://按的是取消匹配
                    {
                        //设为匹配按钮
                        MatchButton.Tag = true;
                        MatchButton.Content = "匹配";
                        //发送取消匹配请求
                        CancelMatch cancel = new CancelMatch() { };
                        GameTCP.SendAsync(cancel);
                    }
                    break;
            }
            
        }

        private void ReadyButton_Click(object sender, RoutedEventArgs e)
        {
            Ready ready = new Ready()
            {
                RoomID = roomid,
                CheLayout = CheckBroad.ConvertFromCheInfoToLayout(gameInfo.CB.GetCurrentChesses(myside))
            };
            GameTCP.SendAsync(ready);
        }

        private void EixtButton_Click(object sender, RoutedEventArgs e)
        {
            Exit exit = new Exit()
            {
                RoomID = roomid,
                
            };
            GameTCP.SendAsync(exit);
            gameInfo.CB.ClearAllChesses();
            gameStatus = GameStatus.Closed;
        }

        private void SkipMoveButton_Click(object sender, RoutedEventArgs e)
        {
            Skip skip = new Skip()
            {
                RoomID = roomid,
            };
            GameTCP.SendAsync(skip);
        }

        private void LoginGrid_Initialized(object sender, EventArgs e)
        {
            UserNameComboBox.SelectionChanged +=(ssender,ee)=>{
                if (ee.AddedItems.Count == 0)
                {
                    PasswordTextB.Password = string.Empty;
                }
                else
                {
                    ComboBoxItem item = (ComboBoxItem)(ee.AddedItems[0]);
                    PasswordTextB.Password = (string)item.Tag;
                }
            };
        }

        private void ChessButton_Click(object sender, RoutedEventArgs e)
        {

            Button b = (Button)sender;
            switch (gameStatus)
            {
                case GameStatus.Layouting:
                    {
                        if (clickedB == null)//当前无选中的棋子
                        {
                            startC = new Coord(Grid.GetRow(b), Grid.GetColumn(b));
                            if (!gameInfo.CB.VertexDataM[startC.i, startC.j].ExistChess())
                                return;
                            clickedB = b;
                        }
                        else 
                        {
                            if (clickedB != b)//选中的棋子不是上一次选中的
                            {
                                endC = new Coord(Grid.GetRow(b), Grid.GetColumn(b));
                                gameInfo.CB.ExchangeChess(startC,endC);
                            }
                            clickedB = null;
                        }
                    }
                    break;
                case GameStatus.Doing:
                    {
                        if (clickedB == null)//无选中的棋子
                        {
                            startC = new Coord(Grid.GetRow(b), Grid.GetColumn(b));
                            if (!gameInfo.CB.VertexDataM[startC.i, startC.j].ExistChess())
                                return;
                            //判断此棋子是否可行走，并查找路径
                            Coord[] allPathsC;
                            try { allPathsC = gameInfo.CB.FindAllPaths(startC).ToArray(); }
                            catch (GameRuleException) { return; }
                            //禁用除选中的我方棋子，开启可选路径
                            ChessInfo[] chessInfos = gameInfo.CB.GetCurrentChesses(myside); 
                            foreach (var cheinfo in chessInfos)
                            {
                                Coord viewC = _reverseToViewC( cheinfo.coord,myside);
                                chessButton[viewC.i, viewC.j].IsEnabled = false;
                            }
                            foreach (var coord in allPathsC)
                            {
                                Coord viewC = _reverseToViewC(coord, myside);
                                chessButton[viewC.i, viewC.j].IsEnabled = true;
                            }
                            clickedB = b;//记录点选的棋子
                        }
                        else//有选中的棋子
                        {
                            if (clickedB != b)//选中的是其他棋子或顶点
                            {
                                endC = new Coord(Grid.GetRow(b), Grid.GetColumn(b));
                                GameTCP.SendAsync(new Move()
                                {
                                    RoomID = roomid,
                                    ChessCoord = startC,
                                    TargetCoord = endC
                                });
                                foreach (var item in chessButton)
                                {
                                   if(item!=null) item.IsEnabled = false;
                                }
                            }
                            else
                            {
                                //关闭路径、开启我方棋子
                                foreach (var item in chessButton)
                                {
                                    if(item!=null)item.IsEnabled = false;
                                }
                                ChessInfo[] chessInfos = gameInfo.CB.GetCurrentChesses(myside);
                                foreach (var cheinfo in chessInfos)
                                {
                                    Coord viewC = _reverseToViewC( cheinfo.coord,myside);
                                    chessButton[viewC.i, viewC.j].IsEnabled = true;
                                }
                            }
                            clickedB = null;
                           
                        }
                    }
                    break;
                case GameStatus.Over:
                    break;
                case GameStatus.Closed:
                    break;
                default:
                    break;
            }


        }
    }
    [ValueConversion(typeof(ChessType), typeof(String))]
    public class ChessTypeTOStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            ChessType ct = ((Chess)value).Type;

            switch (ct)
            {
                case ChessType.SiLing:
                    return "司令";
                case ChessType.JunZhang:
                    return "军长";
                case ChessType.ShiZhang:
                    return "师长";
                case ChessType.LvZhang:
                    return "旅长";
                case ChessType.TuanZhang:
                    return "团长";
                case ChessType.YingZhang:
                    return "营长";
                case ChessType.LianZhang:
                    return "连长";
                case ChessType.PaiZhang:
                    return "排长";
                case ChessType.GongBing:
                    return "工兵";
                case ChessType.JunQi:
                    return "军旗";
                case ChessType.DiLei:
                    return "地雷";
                case ChessType.Bomb:
                    return "炸弹";
                case ChessType.UnKnow:
                default:
                    return string.Empty;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class ChessExitTDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0.0;
            else return 1.0; 
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ChessSideTOBoolColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value==null)
            {
                return Brushes.WhiteSmoke;
            }
            OfSide side = ((Chess)value).Side;
            switch (side)
            {
                case OfSide.First:return new SolidColorBrush(Color.FromRgb(0xFF, 0x74, 0x74));//红色

                case OfSide.Second: return new SolidColorBrush(Color.FromRgb(0x74, 0xAF, 0xFF));//蓝色

                case OfSide.Third: return Brushes.MediumSeaGreen;

                case OfSide.Fourth: return new SolidColorBrush(Color.FromRgb(0xBC, 0x74, 0xFF));//紫色BC74FF

            }
            return Brushes.WhiteSmoke;

        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
