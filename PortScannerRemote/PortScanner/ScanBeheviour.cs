using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortScanner
{
    public class ScanBeheviour
    {
        public static Action<string, int> OnFindOpenPort = delegate { };
        public static Action<string, int> OnPortClose = delegate { };

        public static void Scan(string ip,int Port)
        {
            IPAddress IpAddr = IPAddress.Parse(ip);
            IPEndPoint IpEndP = new IPEndPoint(IpAddr, Port);
            Socket MySoc = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);

            try
            {
                MySoc.Connect(IpEndP);
                OnFindOpenPort.Invoke(ip, Port);
            }
            catch(Exception ex)
            {
                OnPortClose.Invoke(ip, Port);
            }
            finally
            {
                try { MySoc.Close(); } catch { }
            }
        }
    }
}
