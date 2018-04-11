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
        public class Game
        {
            public GameStatus GameStatus;
            public ushort Step;
            public SideInfo Mysideinfo;
            public CheckBroad CB;
        }
        GameStatus gameStatus {set
            {

                gameInfo.GameStatus = value;
                switch (gameInfo.GameStatus)
                {
                    case GameStatus.Layouting:
                        MatchButton.IsEnabled = false;
                        ReadyButton.IsEnabled = true;
                        gameInfo.CB.Layout(CheckBroad.ConvertFromLayoutToCheInfo(CheckBroad.GetDefaultLayout(),myside));
                        break;
                    case GameStatus.Doing:
                        ReadyButton.IsEnabled = false;
                        break;
                    case GameStatus.Over:
                        SurrenderButton.IsEnabled = false;
                        OfferDrawButton.IsEnabled = false;
                        ReadyButton.IsEnabled = false;
                        break;
                    case GameStatus.Closed:
                        MatchButton.IsEnabled = true;
                        break;
                    default:
                        break;
                }
            }
            get { return gameInfo.GameStatus; }
        }
        Game gameInfo=new Game();
        ushort step
        {
            set
            {
                if (gameInfo.Step!= value)
                {
                    gameInfo.Step = value;
                    if (gameInfo.Step>= 50 && gameInfo.Mysideinfo.Status== PlayerStatus.Alive)
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

        Coord startC=new Coord(0,0),endC;
        Button clickedB=null;
        #region 测试代码
        GameTCPClient GameTCP;
        UInt64 roomid;
        UInt64 myuid;
        OfSide myside;
        #endregion
        public GameWindow()
        {
            InitializeComponent();
            GameTCP = new GameTCPClient();
            //LoginIn[] loginIn = new LoginIn[4]
            //{
            //    new LoginIn(){UserName="123456",Password="123456" },
            //    new LoginIn(){UserName="Student",Password="Student" },
            //    new LoginIn(){UserName="HelloWorld",Password="HelloWorld" },
            //    new LoginIn(){UserName="Teacher",Password="Teacher" },
            //};
            //Match match = new Match() { GameMode = GameMode.SiAn };
            //for (int i = 1; i < GameTCP.Count + 1; i++)
            //{

            //    GameTCP[(OfSide)i].StartUp(IPAddress.Loopback);
            //    GameTCP[(OfSide)i].SendAsync(loginIn[i - 1]).Wait();
            //    GameTCP[(OfSide)i].SendAsync(match).Wait();
            //}
            GameTCP.NetMsgRev += _onMsgRecv;
            try
            {
                GameTCP.StartUp(IPAddress.Loopback);
            }
            catch (Exception e)
            {
                GameInfo.Content = "无法连接服务器，请重试";
            }
            

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
                        else LoginInfoLable.Content = loginInfo.Info;
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
                            roomid = matchInfo.RoomID;
                            foreach (var info in matchInfo.PlayerInfo)
                            {
                                if (info.UID==myuid)
                                {
                                    myside = info.Side;
                                    return;
                                }
                            }
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
                default:
                    break;
            }
            NetMsgTextBlock.Text += netServerMsg.MsgType.Name;
        }

        private void CheckboardGrid_Loaded(object sender, RoutedEventArgs e)
        {
           
            
            
        }
        /// <summary>
        /// 匹配到游戏后，开始预先放置棋子按钮、创建与游戏逻辑中的棋盘数据关联
        /// </summary>
        private void CheckboardGrid_SetBingding()
        {
            for (int i = 0; i < 17; i++)
            {
                for (int j = 0; j < 17; j++)//遍历棋盘上的每一顶点
                {
                    Button bt;
                    //预置棋子
                    bt = new Button()
                    { //Style=
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
                        Source = gameInfo.CB.VertexDataM[i, j],
                        Converter = new ChessTypeTOStringConverter(),
                        Mode = BindingMode.OneWay
                    };
                    Binding bChessExist = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[i, j],
                        Converter = new ChessExitTODoubleConverter(),
                        Mode = BindingMode.OneWay
                    };
                    Binding bChessSide = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[i, j],
                        Converter = new ChessSideTOBoolColor(),
                        Mode = BindingMode.OneWay
                    };
                    Binding bChessIsMySide = new Binding("Chess")
                    {
                        Source = gameInfo.CB.VertexDataM[i, j],
                        Converter = new ChessIsMySideTOBool(gameInfo.Mysideinfo.Side),
                        Mode = BindingMode.OneWay
                    };
                    bt.SetBinding(Button.ContentProperty, bChessName);
                    bt.SetBinding(Button.IsVisibleProperty, bChessExist);
                    bt.SetBinding(Button.BackgroundProperty, bChessSide);
                    bt.SetBinding(Button.IsEnabledProperty, bChessIsMySide);
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
            if (!GameTCP.TcpClient.Connected)
            {
                try
                {
                    GameTCP.StartUp(IPAddress.Loopback);
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
                CheLayout = CheckBroad.ConvertFromCheInfoToLayout(gameInfo.CB.GetCurrentChesses())
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
                            Coord c = new Coord(Grid.GetRow(b), Grid.GetColumn(b));
                            if (!gameInfo.CB.VertexDataM[c.i, c.j].ExistChess())
                                return;
                            startC = c;
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
                        if (clickedB == null)//当前无选中的棋子
                        {
                            Coord c = new Coord(Grid.GetRow(b), Grid.GetColumn(b));
                            if (!gameInfo.CB.VertexDataM[c.i, c.j].ExistChess())
                                return;
                            startC = c;
                            clickedB = b;
                        }
                        else
                        {
                            if (clickedB != b)
                            {
                                endC = new Coord(Grid.GetRow(b), Grid.GetColumn(b));
                                GameTCP.SendAsync(new Move()
                                {
                                    RoomID = roomid,
                                    ChessCoord = startC,
                                    TargetCoord = endC
                                });
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
