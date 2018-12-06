using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.CodeDom.Compiler;

namespace PortScanner
{
    public class PortScanner
    {

        public static List<PortInfo> GetOpenPort()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();
            TcpConnectionInformation[] tcpConnections = properties.GetActiveTcpConnections();
            return tcpConnections.Select(p =>
            {
                return new PortInfo(
                    i: p.LocalEndPoint.Port,
                    local: String.Format("{0}:{1}", p.LocalEndPoint.Address, p.LocalEndPoint.Port),
                    remote: String.Format("{0}:{1}", p.RemoteEndPoint.Address, p.RemoteEndPoint.Port),
                    state: p.State.ToString(),
                    endPort: p.RemoteEndPoint.Port);
            }).ToList();
            

        }
    }

    public class PortInfo
    {
        public int PortNumber { get; set; }
        public string Local { get; set; }
        public string Remote { get; set; }
        public string State { get; set; }
        public int PortEndPoint { get; set; }

        public PortInfo(int i, string local, string remote, string state, int endPort)
        {
            PortNumber = i;
            Local = local;
            Remote = remote;
            State = state;
            PortEndPoint = endPort;
        }
    }
}
