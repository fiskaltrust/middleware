using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Helpers;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication
{
    public class SerialPortCommunicationQueue : AbstractCommunicationQueue
    {
        private bool _disposed = false;
        private readonly SerialPort _serialPort;

        private readonly ILogger<SerialPortCommunicationQueue> _logger;

        public override bool DeviceConnected
        {
            get
            {
                try
                {
                    PerformWithLock(() => Open());
                    return _serialPort.IsOpen;
                }
                catch {
                    return false;
                }
            }   
        }

        protected override bool ReadyToRead => _serialPort.BytesToRead != 0;

        public SerialPortCommunicationQueue(ILogger<SerialPortCommunicationQueue> logger, string comPort, int readTimeoutMs, int writeTimeoutMs, bool enableDtr) : base (logger)
        {
            _serialPort = new SerialPort(comPort)
            {
                ReadTimeout = readTimeoutMs,
                WriteTimeout = writeTimeoutMs,
                DtrEnable = enableDtr
            };
            _logger = logger;
        }

        protected override byte[] ReadAdditionalDataFromBuffer()
        {
            var additionalCommandBuffer = new byte[_serialPort.BytesToRead];
            _serialPort.Read(additionalCommandBuffer, 0, additionalCommandBuffer.Length);
            return additionalCommandBuffer;
        }

        protected override void Write(byte[] buffer, int offset, int length) => _serialPort.Write(buffer, offset, length);

        protected override void Open()
        {
            if (!_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open();
                    _logger.LogDebug("Succeeded opening Port at: {0}", _serialPort.PortName);
                }
                catch (Exception x)
                {
                    _logger.LogDebug(x, "Not able to open port");
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
                    if (_serialPort != null)
                    {
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.Close();
                        }
                        _serialPort.Dispose();
                    }
                }
                _disposed = true;
            }
        }
    }
}