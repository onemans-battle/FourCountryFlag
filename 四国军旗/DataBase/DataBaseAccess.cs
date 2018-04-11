using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
namespace DataBase
{
    /*掌管数据库的访问：查询、修改、删除数据
     * 所有对数据库的操作都封装于此，使得调用者（服务器）可以忽略数据库访问与数据转换的复杂性
     * 目前要实现的最基本的操作有：账号认证、玩家信息查询、匹配权值查询
     * 
     * 
     */
    public class DBAcess
    {
        private MySqlConnection _conn;
        
        //创建连接、并打开
        //连接错误抛出 MySqlException
        public DBAcess(string connStr = "server=localhost;user=root;database=game;port=3306;password=dcxnjkw3DCKSOL3xmjkw")
        {
            _conn = new MySqlConnection(connStr);
        }
        public void Open()
        {
            _conn.Open();
        }
        /// <summary>
        /// 账号认证：防注入地在数据库中对账号密码检查。认证成功，返回true，将玩家ID赋值给ID。错误则返回false，ID为其类型的默认值
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <param name="ID"></param>
        /// <exception cref="MySqlException">内部的sql语句错误，MySqlDataReader未关闭，数据库连接已关闭</exception>
        /// <returns></returns>
        public bool AuthenAccount(string account,string password,out UInt64 ID)
        {
            string sql = "SELECT 玩家ID  FROM 帐号认证 where 账号=@account and 密码=@password";
            MySqlCommand cmd=new MySqlCommand(sql,_conn); 
            cmd.Parameters.AddWithValue("account",account);
            cmd.Parameters.AddWithValue("password", password);
            using (MySqlDataReader rdr = cmd.ExecuteReader()) 
            {
                if (rdr.Read())
                {
                    ID = rdr.GetUInt64("玩家ID");
                    return true;
                }
                else
                {
                    ID = default(UInt64);
                    return false;
                }
            }  

               
        }

        /// <summary>
        /// (PlayerNormalGameDatabaseRecord):玩家ID、昵称、头像相对路径、胜利次数、失败次数、逃跑和掉线次数、分数、游戏等级
        /// </summary>
        //
        public struct PNGDBRecord
        {
            public UInt64 ID;
            public string Nickname;
            public string Path;
            public ushort Win;
            public ushort Fail;
            public ushort Escape;
            public ushort Score;
            public string Grade;
        }

        //玩家信息查询：防注入地根据玩家ID，查询其昵称、头像相对路径、胜利次数、失败次数、逃跑和掉线次数、分数、游戏等级
        //异常：MySqlException：内部的sql语句错误，MySqlDataReader未关闭，数据库连接已关闭
        public bool TryQueryPlayerInfo(UInt64 ID,out PNGDBRecord playerInfo)
        {
            string sql = "SELECT *  FROM 玩家信息 where 玩家ID=@ID";
            MySqlCommand cmd = new MySqlCommand(sql, _conn);
            cmd.Parameters.AddWithValue("ID", ID);
            using (MySqlDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    playerInfo = new PNGDBRecord()
                    {
                        ID = ID,
                        Nickname = rdr.GetString(1),
                        Path = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
                        Win = rdr.GetUInt16(3),
                        Fail = rdr.GetUInt16(4),
                        Escape = rdr.GetUInt16(5),
                        Score = rdr.GetUInt16(6),
                        Grade = rdr.GetString(7)
                    };
                    return true;
                }
                else
                {
                    playerInfo = default(PNGDBRecord);
                    return false;
                }
            }
            
        }



        //查询普通游戏匹配权值：防注入地根据玩家ID和游戏模式，查询匹配权值
        public bool TryQueryNormalWeight(UInt64 ID,string mode,out ushort weight)
        {
            string sql = "SELECT *  FROM 玩家匹配权值 where 玩家ID=@ID and 游戏模式=@mode" ;
            MySqlCommand cmd = new MySqlCommand(sql, _conn);
            cmd.Parameters.AddWithValue("ID",ID);
            cmd.Parameters.AddWithValue("mode", mode);
            using (MySqlDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    weight = rdr.GetUInt16(2);
                    return true;
                }
                else
                {
                    weight = default(ushort);
                    return false;
                }
            }

        }


        //关闭与数据库的连接
        public void Close()
        {
            _conn.Close();
        }
    }

}
