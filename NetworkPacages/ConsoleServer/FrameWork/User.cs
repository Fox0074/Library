
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Net;

    public class User : IDisposable
    {
        public Action<string> OnSendChatMessage = delegate { };

        public const int PING_TIME = 7000;

        private readonly Timer _pingTimer;
        public EndPoint EndPoint { get; private set; }
        private readonly object syncLock = new object();
        private Unit _syncResult;
        private readonly ManualResetEventSlim _OnResponce = new ManualResetEventSlim(false);


        public void SyncResult(Unit msg)
        {  // получен результат выполнения процедуры

            _syncResult = msg;
            _syncResult.IsEmpty = false;

            _OnResponce.Set();  // разблокируем поток
        }

        public UserType UserType = UserType.UnAuthorized;

        public byte[] HeaderLength = BitConverter.GetBytes((int)0);

        public readonly TcpClient _socket;
        public readonly ConqurentNetworkStream nStream;

        private readonly object _disposeLock = new object();
        private bool _IsDisposed = false;

        public User(TcpClient Socket)
        {
            this._socket = Socket;
            Socket.ReceiveTimeout = PING_TIME * 4;
            Socket.SendTimeout = PING_TIME * 4;
            Socket.ReceiveBufferSize = 9999999;
            Socket.ReceiveBufferSize = 9999999;
            nStream = new ConqurentNetworkStream(Socket.GetStream());
            _pingTimer = new Timer(OnPing, null, PING_TIME, PING_TIME);
            EndPoint = Socket.Client.RemoteEndPoint;

        }

        private void OnPing(object state)
        {
            ServerNet.SendMessage(nStream, new Unit("OnPing", null));
        }
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (!_IsDisposed)
                {
                    _IsDisposed = true;
                    nStream.Dispose();
                    _pingTimer.Dispose();
                    _socket.Close();
                }
            }
        }

        public Unit Execute(string MethodName, object[] parameters, bool IsWaitAnswer)
        {
            lock (syncLock)
            {
                _syncResult = new Unit(MethodName, parameters);
                _syncResult.IsSync = IsWaitAnswer;

                if (IsWaitAnswer)
                {
                    _OnResponce.Reset();
                    ServerNet.SendMessage(nStream, _syncResult);
                    _OnResponce.Wait();  // ожидаем ответ сервера
                }
                else
                {
                    ServerNet.SendMessage(nStream, _syncResult);
                }

                if (_syncResult.IsEmpty && IsWaitAnswer)
                {// произошел дисконект, результат не получен
                    throw new Exception(string.Concat("Ошибка при получении результата на команду \"", MethodName, "\""));
                }

                if (_syncResult.Exception != null)
                    throw _syncResult.Exception;  // исключение переданное клиентом          
                return _syncResult;
            }
        }
    }
