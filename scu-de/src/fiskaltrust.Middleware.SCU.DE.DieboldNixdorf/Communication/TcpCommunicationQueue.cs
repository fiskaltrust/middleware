using System;
using System.Linq;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication
{
    public class TcpCommunicationQueue : AbstractCommunicationQueue
    {
        private bool _disposed = false;

        private readonly TcpClient _client;
        private readonly ILogger<TcpCommunicationQueue> _logger;
        private readonly string _hostname;
        private readonly int _port;

        public override bool DeviceConnected
        {
            get
            {
                try
                {
                    PerformWithLock(() => Open());
                    return _client.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        protected override bool ReadyToRead => _client.GetStream().DataAvailable;

        public TcpCommunicationQueue(ILogger<TcpCommunicationQueue> logger, string hostname, int port) : base(logger)
        {
            _logger = logger;
            _hostname = hostname;
            _port = port;
            _client = new TcpClient();
        }

        protected override byte[] ReadAdditionalDataFromBuffer()
        {
            var innerReadBuffer = new byte[_client.ReceiveBufferSize];
            var readCount = _client.GetStream().Read(innerReadBuffer, 0, innerReadBuffer.Length);
            return innerReadBuffer.Take(readCount).ToArray();
        }

        protected override void Write(byte[] buffer, int offset, int length) => _client.GetStream().Write(buffer, 0, buffer.Length);

        protected override void Open()
        {
            if (!_client.Connected)
            {
                try
                {
                    _client.Client.Disconnect(true);
                    _client.Connect(_hostname, _port);
                    _logger.LogDebug("Succeeded connecting to TCP at: {0}:{1}", _hostname, _port);
                }
                catch (Exception x)
                {
                    _logger.LogDebug(x, "Not able to open TCP connection ({0}:{1})", _hostname, _port);
                    SerialPortDeviceUnavailable = true;
                    throw;
                }
                SerialPortDeviceUnavailable = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
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
                    }
                }
                _disposed = true;
            }
        }
    }
}