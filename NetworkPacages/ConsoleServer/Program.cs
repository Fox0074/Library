using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConsoleServer
{
    class Program
    {
        private static readonly ManualResetEventSlim _OnResponce = new ManualResetEventSlim(false);

        static void Main(string[] args)
        {
            ServerNet server = new ServerNet(7776);
            server.Events.OnConnected += () => { Console.WriteLine("Подключен клиент"); };
            server.Events.OnDisconnect += () => { Console.WriteLine("Отключен клиент"); };
            server.Events.OnGetMessage += (Unit unit, User user) => { Console.WriteLine(DateTime.Now.ToLongTimeString() + "  " + user.EndPoint.ToString() + " : {0}({1})", unit.Command, unit.Command); };
            server.Start();
            Console.WriteLine("Сервер Запущен");

            _OnResponce.Wait();
        }
    }
}
