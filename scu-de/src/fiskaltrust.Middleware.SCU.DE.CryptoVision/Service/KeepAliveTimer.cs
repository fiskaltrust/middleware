using System;
using System.IO;
using System.Linq;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Service
{
    public class KeepAliveTimer : IDisposable
    {
        private readonly string _fileName = "TSE-GUARD.bin";
        private readonly int _byteArraySize = 512;
        private Timer _timer = null;
        private string _filePath;
        private FileStream _fileStream = null;
        private bool _disposed = false;
        private readonly ILogger _logger;

        public KeepAliveTimer(ILogger logger)
        {
            _logger = logger;
        }

        public void CreateTimer(string devicePath, int keepAliveInterval)
        {
            _filePath = Path.Combine(devicePath, _fileName);
            if (!File.Exists(_filePath))
            {
                var writeBytes = Enumerable.Repeat((byte) 0x0, _byteArraySize).ToArray();
                File.WriteAllBytes(_filePath, writeBytes);
            }
            _timer = new Timer(keepAliveInterval);
            _timer.Elapsed += OnKeepAliveEvent;
            _timer.Start();

        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    if (_fileStream != null)
                    {
                        _fileStream.Close();
                        _fileStream.Dispose();
                    }
                }
            }

            _timer = null;
            _fileStream = null;
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void OnKeepAliveEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                if (_fileStream == null)
                {
                    _fileStream = new FileStream(_filePath, FileMode.Open);
                }
                var readBytes = _fileStream.ReadByte();
                _fileStream.Position = 0;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to execute keep alive!");
                _fileStream.Close();
                _fileStream.Dispose();
                _fileStream = null;
            }
        }
    }
}
