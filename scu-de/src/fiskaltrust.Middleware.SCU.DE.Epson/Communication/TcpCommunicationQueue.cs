using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.Epson.Exceptions;
using fiskaltrust.Middleware.SCU.DE.Epson.Helpers;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Communication
{
    public class TcpCommunicationQueue : IDisposable
    {
        private readonly SemaphoreSlim _hwSemaphore = new SemaphoreSlim(1, 1);
        private const int maxHwSemaphoreWaitTimeout = 120 * 1000;

        private bool _disposed = false;

        private const int CONNECT_RETRY = 5;
        private TcpClient _client;
        private readonly ILogger<TcpCommunicationQueue> _logger;
        private readonly string _hostname;
        private readonly int _port;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        protected bool ReadyToRead => _client.GetStream().DataAvailable;

        public bool TcpEndpointUnavailable { get; private set; }

        public bool IsConnected => _client?.Connected ?? false;

        public TcpCommunicationQueue(ILogger<TcpCommunicationQueue> logger, EpsonConfiguration epsonConfiguration)
        {
            _logger = logger;
            _hostname = epsonConfiguration.Host;
            _port = epsonConfiguration.Port;
            _client = new TcpClient();
        }

        protected byte[] ReadAdditionalDataFromBuffer()
        {
            var innerReadBuffer = new byte[_client.ReceiveBufferSize];
            var readCount = _client.GetStream().Read(innerReadBuffer, 0, innerReadBuffer.Length);
            return innerReadBuffer.Take(readCount).ToArray();
        }

        public async Task<string> SendCommandWithResultAsync(byte[] command, double timeOutInMilliSeconds = 60000)
        {
            return await LockingHelper.PerformWithLock(_hwSemaphore, async () =>
            {
                await OpenAsync();
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(timeOutInMilliSeconds));
                _client.GetStream().Write(command, 0, command.Length);
                return await ReceiveDataAsync(_cancellationTokenSource.Token);
            }, maxHwSemaphoreWaitTimeout);
        }

        private async Task<string> ReceiveDataAsync(CancellationToken token = default)
        {
            while (!ReadyToRead)
            {
                if (token.IsCancellationRequested)
                {
                    throw new NoResponseException();
                }
            }
            var result = new List<byte>();
            do
            {
                var buffer = new byte[_client.ReceiveBufferSize];
                var readCount = await _client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                result.AddRange(buffer.Take(readCount));
                if (buffer[readCount - 1] == 0x00)
                {
                    break;
                }
            } while (true);
            return Encoding.UTF8.GetString(result.ToArray());
        }

        protected async Task OpenAsync()
        {
            if (!_client.Connected)
            {
                try
                {
                    var connectRetry = 0;
                    while (connectRetry < CONNECT_RETRY && !_client.Connected)
                    {
                        try
                        {
                            try
                            {
                                _client.Client.Disconnect(true);
                            }
                            catch { }
                            await _client.ConnectAsync(_hostname, _port);
                            await ReceiveDataAsync();
                            _logger.LogDebug("Succeeded connecting to TCP at: {Host}:{Port}", _hostname, _port);
                        }
                        catch(SocketException socketEx)
                        {
                            throw new EpsonException($"The TSE is not available. Unable to connect to endpoint at {_hostname}:{_port}. {socketEx.Message}");
                        }
                        catch (Exception ex) when (connectRetry + 1 < CONNECT_RETRY)
                        {
                            _logger.LogDebug(ex, "Succeeded connecting to TCP at: {Host}:{Port}", _hostname, _port);
                        }
                        finally
                        {
                            connectRetry++;
                        }
                    }
                }
                catch (Exception x)
                {
                    _logger.LogDebug(x, "Not able to open TCP connection ({Host}:{Port})", _hostname, _port);
                    TcpEndpointUnavailable = true;
                    throw;
                }
                TcpEndpointUnavailable = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_client != null)
                    {
                        if (_client.Connected)
                        {
                            _client.Close();
                        }
                        _client.Dispose();
                        _client = null;
                    }
                }
                _disposed = true;
            }
        }
    }
}