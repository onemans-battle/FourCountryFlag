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
                        
                        CheckboardGrid_CreateChessAndSetBingding();
                        ChessInfo[] chessInfos = CheckBroad.ConvertFromLayoutToCheInfo(CheckBroad.GetDefaultLayout(), myside);
                        gameInfo.CB.Layout(chessInfos);
                        ReadyButton.IsEnabled = true;
                        for (int i = 0; i < chessButton.GetLength(0); i++)
                        {
                            for (int j = 0; j < chessButton.GetLength(1); j++)
                            {
                                chessButton[i, j].IsEnabled = false;
                            }
                        }
                        foreach (var item in chessInfos)
                        {
                            viewC = _reverseToViewC(item.coord, myside);
                            chessButton[viewC.i, viewC.j].IsEnabled = true;
                        }

                        break;
                    case GameStatus.Doing:
                        ChessInfo[] chessInfo = CheckBroad.ConvertFromLayoutToCheInfo(CheckBroad.GetDefaultLayout(), myside);
                        HasSecondTick_lable.Visibility = Visibility.Visible;
                        ReadyButton.IsEnabled = false;
                        foreach (var item in chessInfo)
                        {
                            viewC = _reverseToViewC( item.coord,myside);
                            chessButton[viewC.i, viewC.j].IsEnabled = true;
                        }
                        break;
                    case GameStatus.Over:
                        SurrenderButton.IsEnabled = false;
                        OfferDrawButton.IsEnabled = false;
                        ReadyButton.IsEnabled = false;
                        Timer.Stop();
                        HasSecondTick_lable.Visibility = Visibility.Hidden;
                        break;
                    case GameStatus.Closed:
                        SurrenderButton.IsEnabled = false;
                        OfferDrawButton.IsEnabled = false;
                        ReadyButton.IsEnabled = false;
                        MatchButton.IsEnabled = true;
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
        OfSide myside;//我方势力、我方视角点
        Button[,] chessButton =new Button[17,17];
        System.Timers.Timer Timer = new System.Timers.Timer(1000) { AutoReset=true};
        int hasSencond;
        #endregion
        public GameWindow()
        {
            InitializeComponent();

            iPAddresses = Dns.GetHostAddressesAsync(domainName);

            Action action = () => {
                if(hasSencond!=0) hasSencond -= 1;
                HasSecondTick_lable.Content = hasSencond;
                };
            Timer.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(action);
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
                            Login.Visibility = Visibility.Hidden;
                            MatchButton.IsEnabled = true;
                        }
                        else
                        {
                            LoginInfoLable.Content = loginInfo.Info;
                        }
                        NetMsgTextBlock.Text += loginInfo.Info;
                    }
                    break;
                case "PReady":
                    {
                        PReady pr = (PReady)netServerMsg.Data;
                        gameInfo.CB.Layout(CheckBroad.ConvertFromLayoutToCheInfo(pr.CheLayout, pr.PlayerInfo.Side));
                    };
                    break;
                case "MatchInfo":
                    {
                        MatchInfo matchInfo = (MatchInfo)netServerMsg.Data;
                        if (matchInfo.HasAGame)
                        {
                            MatchButton.IsEnabled = false;
                            roomid = matchInfo.RoomID;
                            foreach (var info in matchInfo.PlayerInfo)
                            {
                                if (info.UID==myuid)
                                {
                                    myside = info.Side;
                                    break;
                                }
                            }
                            gameInfo.Mysideinfo = new SideInfo(myside);
                            gameInfo.CB = new CheckBroad(matchInfo.GameMode);
                        }
                    }; break;
                case "GameLayouting":
                    {
                        GameLayouting gameLayouting=(GameLayouting)netServerMsg.Data;
                        gameStatus = GameStatus.Layouting;
                    }
                    break;
                case "GetquestError":
                    {
                        GetquestError getquestError = (GetquestError)netServerMsg.Data;
                        if(getquestError.Code==101)
                        Login.Visibility = Visibility.Visible;
                    };break;
                case "GameStart":
                    gameStatus = GameStatus.Doing;
                    break;
                case "GameOver":
                    GameOver gameOver = (GameOver)netServerMsg.Data;
                    gameInfo.CB.ClearAllChesses();
                    GameInfo.Content += "  游戏结束：胜利者是：" + gameOver.GResult.Winers+"\n";
                    gameStatus = GameStatus.Over;
                    break;
                case "GameClose":
                    gameStatus = GameStatus.Closed;
                    break;
                case "SideNext":
                    {
                        SideNext sideNext = (SideNext)netServerMsg.Data;
                        _onSideNext(sideNext);
                    };break;
                case "PMove":
                    {
                        PMove pmove = (PMove)netServerMsg.Data;
                        gameInfo.CB.Move(pmove.SMInfo);
                    };break;
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
                default:
                    break;
            }
            NetMsgTextBlock.Text += "  "+netServerMsg.MsgType.Name;
        }

        private void _onSideNext(SideNext sideNext)
        {
            ChessInfo[] chessInfos = gameInfo.CB.GetCurrentChesses(myside);
            if (sideNext.PlayerInfo.Side == myside)
            {
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
            Timer.Start();
        }
        private void CheckboardGrid_Loaded(object sender, RoutedEventArgs e)
        {
           
            
            
        }
        /// <summary>
        /// 从UI坐标转换为游戏逻辑中的棋盘坐标。从UI上对棋子有关的操作都需经过此层转换.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="viewSide"></param>
        /// <returns></returns>
        /// <remarks>
        /// (ii,jj)为UI棋盘网格上的坐标,(i,j)为以棋盘坐标(8,8)为原点建立的坐标系中的点坐标
        /// 逆时针旋转90度: (i,j)-> (x,y)=(j,-i).转换到棋盘坐标系中为(jj,-ii+16)
        /// 逆时针旋转180即两次90旋转度: (i,j)-> (j,-i)-> (x,y)=(-i,-j) .最后(-ii+16,-jj+16)
        /// 同上，270度为(jj,ii)
        /// </remarks>
        private static Coord _exhangeToRealC(Coord c, OfSide viewSide)
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
                    viewC.i = c.j;
                    viewC.j = c.i;
                    break;
            }
            return viewC;
        }
        /// <summary>
        /// 从棋盘坐标转为UI坐标。
        /// </summary>
        /// <param name="c"></param>
        /// <param name="viewSide"></param>
        /// <returns></returns>
        /// <remark>
        /// 与转为视角坐标唯一的不同在于它是顺时针的。
        /// 同理,90度为: (i,j)-> (x,y)=(-j,i),(-jj+16,ii)
        /// 180度:(-i,-j),最后(-ii+16,-jj+16)
        /// 270度 (j,-i),最后(jj,-ii+16)
        /// </remark>
        private static Coord _reverseToViewC(Coord c, OfSide viewSide)
        {
            Coord realC = new Coord();
            switch (viewSide)
            {
                case OfSide.First:
                    return c;
                case OfSide.Second://转90度
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
        /// 匹配到游戏后，开始预先放置棋子按钮、以“我方的视角”创建与游戏逻辑中的棋盘数据关联
        /// </summary>
        private void CheckboardGrid_CreateChessAndSetBingding()
        {
            Coord realC;//视角坐标
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 17; j++)//遍历棋盘上的每一顶点
                {
                    //该UI坐标
                    realC = _exhangeToRealC(new Coord(i, j), myside);
                    Button bt;
                    //预置棋子
                    bt = new Button()
                    { Style = (Style)FindResource("UnableChessButton"),
                        Margin = new Thickness(5),
                        FontWeight = FontWeights.Bold,
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                    };
                    bt.Click += ChessButton_Click;//订阅事件
                    CheckboardGrid.Children.Add(bt);

                    Grid.SetRow(bt, i);
                    Grid.SetColumn(bt, j);
                    //建立数据关联
                    Binding bChessName = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[realC.i, realC.j],
                        Converter = new ChessTypeTOStringConverter(),
                        Mode = BindingMode.OneWay
                    };
                    Binding bChessExist = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[realC.i, realC.j],
                        Converter = new ChessExitTODoubleConverter(),
                        Mode = BindingMode.OneWay
                    };
                    Binding bChessSide = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[realC.i, realC.j],
                        Converter = new ChessSideTOBoolColor(),
                        Mode = BindingMode.OneWay
                    };
                    Binding bChessIsMySide = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[realC.i, realC.j],
                        Converter = new ChessIsMySideTOBool(gameInfo.Mysideinfo.Side),
                        Mode = BindingMode.OneWay
                    };
                    bt.SetBinding(Button.ContentProperty, bChessName);
                    bt.SetBinding(Button.OpacityProperty, bChessExist);
                    bt.SetBinding(Button.BackgroundProperty, bChessSide);
                    chessButton[i, j] = bt;
                    //bt.SetBinding(Button.IsEnabledProperty, bChessIsMySide);
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
                GameTCP = new GameTCPClient();
                try
                {
                    //string ipandport = IPAndPortTextB.Text;
                    //string[] s = ipandport.Split(':');
                    //IPAddress ip = IPAddress.Parse(s[0]);
                    //int port = Convert.ToInt32(s[1]);
                    if (ipAddress==default(IPAddress)&&port==default(int))
                    {
                        ipAddress = iPAddresses.Result[0];
                        port = 8080;
                    }
                    GameTCP.StartUp(ipAddress, port);
                    GameTCP.NetMsgRev += _onMsgRecv;
                }
                catch (Exception exc)
                {
                    LoginInfoLable.Content = "无法连接服务器，请重试";
                    return;
                }
            }
            LoginIn loginIn = new LoginIn()
            {
                UserName = UsernameTextB.Text,
                Password = PasswordTextB.Password
            };
            GameTCP.SendAsync(loginIn);
        }

        private void MatchButton_Click(object sender, RoutedEventArgs e)
        {
            Match match = new Match() { GameMode = GameMode.Solo };
            GameTCP.SendAsync(match);
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
                            clickedB = b;
                            //禁用除选中的我方棋子，开启可选路径
                            ChessInfo[] chessInfos = gameInfo.CB.GetCurrentChesses(myside);
                            foreach (var cheinfo in chessInfos)
                            {
                                Coord viewC = _reverseToViewC( cheinfo.coord,myside);
                                chessButton[viewC.i, viewC.j].IsEnabled = false;
                            }
                            Coord[] coords= gameInfo.CB.FindAllPaths(startC).ToArray();
                            foreach (var coord in coords)
                            {
                                Coord viewC = _reverseToViewC(coord, myside);
                                chessButton[viewC.i, viewC.j].IsEnabled = true;
                            }
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
                                    item.IsEnabled = false;
                                }
                            }
                            else
                            {
                                //关闭路径、开启我方棋子
                                foreach (var item in chessButton)
                                {
                                    item.IsEnabled = false;
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
    public class ChessIsMySideTOBool : IValueConverter
    {
        OfSide _side;
        public ChessIsMySideTOBool(OfSide side)
        {
            _side = side;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            OfSide s = ((Chess)value).Side;

            if (s==_side)
            {
                return true;
            }
            else return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
    
    public class ChessExitTODoubleConverter : IValueConverter
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
                case OfSide.First:return Brushes.MediumVioletRed;

                case OfSide.Second: return Brushes.BlueViolet;

                case OfSide.Third: return Brushes.MediumSeaGreen;

                case OfSide.Fourth: return Brushes.WhiteSmoke;
                    
            }
            return Brushes.WhiteSmoke;

        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
