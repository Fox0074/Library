using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NetworkPakage
{
    class Program
    {
        private static readonly ManualResetEventSlim _OnResponce = new ManualResetEventSlim(false);

        static void Main()
        {
            //Запуск Соеденения
            new Client();

            Console.WriteLine("Запуск");
            _OnResponce.Wait();
            Console.WriteLine("Сработал _OnResponce.wait()");
        }
    }
}
