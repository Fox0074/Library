using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

    public class ConqurentNetworkStream : IDisposable
    {
        private readonly BlockingCollection<byte[]> _fifo = new BlockingCollection<byte[]>();
        private readonly NetworkStream _nstream;
        private readonly CancellationTokenSource _token = new CancellationTokenSource();
        private readonly ManualResetEventSlim _disposeEvent = new ManualResetEventSlim(false);

        public ConqurentNetworkStream(NetworkStream nstream)
        {
            this._nstream = nstream;
            ThreadPool.QueueUserWorkItem(_thread);
        }

        private void _thread(object state)
        {
            try
            {
                while (true)
                {
                    try
                    {
                        byte[] data = _fifo.Take(_token.Token);
                        _nstream.BeginWrite(data, 0, data.Length, null, null);
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                _disposeEvent.Set();
                return;
            }
        }

        public void Add(byte[] data)
        {
            _fifo.Add(data);
        }

        public int Read(byte[] data)
        {
            return _nstream.Read(data, 0, data.Length);
        }

        public int EndRead(IAsyncResult asyncResult)
        {
            return _nstream.EndRead(asyncResult);
        }

        public IAsyncResult BeginRead(byte[] data, AsyncCallback callback, object state)
        {
            return _nstream.BeginRead(data, 0, data.Length, callback, state);
        }

        private readonly object _disposeLock = new object();
        private bool _IsDisposed;
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (!_IsDisposed)
                {
                    _IsDisposed = true;
                    _token.Cancel();
                    _disposeEvent.Wait();
                    _token.Dispose();
                    _disposeEvent.Dispose();
                    _nstream.Dispose();
                }
            }
        }
    }
