using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Collections;
using Newtonsoft.Json;
using System.Xml;
using MySql.Data;
using MySql.Data.MySqlClient;
namespace learning
{
    class Program
    {
        static void Main(string[] args)
        {
            //person对象json化后，解码为object类型后，还能否再恢复为person？
            Person person = new Person("ki", "23");
            string p =JsonConvert.SerializeObject(person);
            object obj = JsonConvert.DeserializeObject(p);

            Person pp = default(Person);
           


            Console.ReadKey();
        }
        
        public class Person
        {
            public string Name;
            public string Age;
            public Person(string name,string age)
            {
                Name = name;
                Age = age;
            }
        }

    }
}
