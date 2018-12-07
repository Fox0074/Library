using Injector;
using System;
using System.IO;
using System.Reflection;

namespace alphabotcsharp
{

    public class Program
    {
        

        public static void Main()
        {
            string rawDLL = String.Empty;
            if (Injection.is64BitOperatingSystem)
            {
                rawDLL = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "DLLInject_64.dll");
            }
            else
            {
                rawDLL = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "DLLInject_64.dll");
            }
            DllInjectionResult result = Injection.Inject("mspaint", rawDLL);
            Console.WriteLine(result);         
            Console.ReadLine();
        }
    }

    public enum DllInjectionResult
    {
        DllNotFound,
        GameProcessNotFound,
        InjectionFailed,
        Success
    }

}