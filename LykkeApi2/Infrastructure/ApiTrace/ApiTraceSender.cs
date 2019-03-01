using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace LykkeApi2.Infrastructure.ApiTrace
{
    public interface IApiTraceSender
    {
        Task LogMethodCall(object item);
    }

    public class ApiTraceSender : IApiTraceSender, IDisposable
    {
        private readonly bool _useApiTrace;
        private readonly string _host;
        private readonly int _port;
        private readonly ConcurrentQueue<object> _logs = new ConcurrentQueue<object>();
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private Thread _worker;

        private bool _isConnect = false;
        private TcpClient _client;
        private StreamWriter _writer;

        private bool _isWork = true;

        public ApiTraceSender(bool useApiTrace, string host, int port)
        {
            _useApiTrace = useApiTrace;
            _host = host;
            _port = port;
        }

        public async Task LogMethodCall(object item)
        {
            if (!_useApiTrace)
            {
                return;
            }

            if (_logs.Count > 100)
            {
                return;
            }

            _logs.Enqueue(item);

            await CheckWorker();
        }

        private async Task CheckWorker()
        {
            if (_worker != null)
            {
                return;
            }

            await _gate.WaitAsync();
            try
            {
                if (_worker == null)
                {
                    _worker = new Thread(Worker)
                    {
                        IsBackground = true
                    };
                    _worker.Start();
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private void Worker()
        {
            while (_isWork)
            {
                while (_logs.TryDequeue(out var item))
                {
                    SendData(item.ToJson());
                }
                Thread.Sleep(10);
            }
        }

        private void SendData(string json)
        {
            while (true)
            {
                try
                {
                    if (!_isConnect || _writer == null)
                    {
                        _writer?.Dispose();
                        _client?.Dispose();

                        _client = new TcpClient(_host, _port);
                        _writer = new StreamWriter(_client.GetStream());
                        _isConnect = true;
                    }

                    _writer.WriteLine(json);
                    return;
                }
                catch (Exception)
                {
                    _isConnect = false;
                }
            }
        }

        public void Dispose()
        {
            _isWork = false;
            _worker?.Join();
            _worker = null;

            _writer?.Dispose();
            _writer = null;

            _client?.Dispose();
            _client = null;

            _isConnect = false;
        }
    }
}