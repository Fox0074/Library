using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PortScanner
{
    class RouterConnection
    {
        /// <summary>
        /// Подключение к админке роутера, требуется открытый 80 порт(Но это не точно)
        /// </summary>
        public static bool TryConnect(string ip, string login, string pass)
        {
            try
            {
                string URL = "http://" + ip + "/";
                NetworkCredential nc = new NetworkCredential(login, pass);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                //request.Timeout = 7;
                request.Credentials = nc;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                WebHeaderCollection whc = response.Headers;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                response.Close();
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
