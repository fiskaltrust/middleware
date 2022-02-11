using System;
using System.Collections.Generic;
using System.Threading;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Helpers;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication
{
    public abstract class AbstractCommunicationQueue : ISerialCommunicationQueue, IDisposable
    {
        private bool _disposed = false;

        private readonly SemaphoreSlim _hwSemaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger<AbstractCommunicationQueue> _logger;
        private const int maxHwSemaphoreWaitTimeout = 120 * 1000;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public bool SerialPortDeviceUnavailable { get; set; }

        public abstract bool DeviceConnected { get; }

        protected abstract bool ReadyToRead { get; }

        public AbstractCommunicationQueue(ILogger<AbstractCommunicationQueue> logger)
        {
            _logger = logger;
        }

        protected abstract byte[] ReadAdditionalDataFromBuffer();
        protected abstract void Write(byte[] buffer, int offset, int length);
        protected abstract void Open();

        public void PerformWithLock(Action action)
        {
            var locked = false;
            try
            {
                locked = _hwSemaphore.Wait(maxHwSemaphoreWaitTimeout);
                if (!locked)
                {
                    throw new Exception($"Unable to perform lock after {maxHwSemaphoreWaitTimeout} ms");
                }
                action();
            }
            finally
            {
                if (locked)
                {
                    _hwSemaphore.Release(1);
                }
            }
        }

        public T PerformWithLock<T>(Func<T> action)
        {
            var locked = false;
            try
            {
                locked = _hwSemaphore.Wait(maxHwSemaphoreWaitTimeout);
                if (!locked)
                {
                    throw new Exception($"Unable to perform lock after {maxHwSemaphoreWaitTimeout} ms");
                }
                return action();
            }
            finally
            {
                if (locked)
                {
                    _hwSemaphore.Release(1);
                }
            }
        }

        public void SendCommand(byte[] command, DieboldNixdorfCommand commandType, Guid requestId)
        {
            PerformWithLock(() =>
            {
                Open();
                _logger.LogDebug("Sending -----> Command: {0}; RequestId: {1}; Body: {2}", commandType, requestId, BitConverter.ToString(command));
                Write(command, 0, command.Length);
            });
        }

        public TseResult SendCommandWithResult(byte[] command, Guid requestId, DieboldNixdorfCommand commandType, double timeOutInMilliSeconds = 1000)
        {
            return PerformWithLock(() =>
            {
                Open();
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(timeOutInMilliSeconds));
                _logger.LogDebug("Sending -----> Command: {0}; RequestId: {1}; Body: {2}", commandType, requestId, BitConverter.ToString(command));
                Write(command, 0, command.Length);
                return ReceiveData(requestId, commandType, _cancellationTokenSource.Token);
            });
        }

        private TseResult ReceiveData(Guid requestId, DieboldNixdorfCommand command, CancellationToken token)
        {
            while (!ReadyToRead)
            {
                if (token.IsCancellationRequested)
                {
                    _logger.LogWarning("Failed to receive a response for {0}", command);
                    throw new NoResponseException();
                }
            }
            var response = new List<byte>();
            response.AddRange(ReadAdditionalDataFromBuffer());

            if (response[0] == 0x1b && response[1] == 0x6b)
            {
                _logger.LogDebug("Received ASB Status for command {0}. ({1})", command, BitConverter.ToString(response.ToArray()));
                // 202-10-06 Stefan Kert: If we get this we just ignore it. This is the SETASB stuff that we disable in our implementation
                return ReceiveData(requestId, command, token);
            }

            if (!ContainsEndOfBuffer(response))
            {
                Thread.Sleep(50);
                response.AddRange(ReadAdditionalDataFromBuffer());
            }

            if (TseResultHelper.GetRequestId(response) != requestId)
            {
                Thread.Sleep(50);
                response.AddRange(ReadAdditionalDataFromBuffer());
            }
            _logger.LogDebug("Received -----> Command: {0}; RequestId: {1}; Body: {2}", command, requestId, BitConverter.ToString(response.ToArray()));
            var tseResult = CreateResultBasedOnCommandAndResponse(command, response);
            if (tseResult.RequestId != requestId)
            {
                throw new Exception($"Something weird happend. Expected RequestId to be {requestId} but was {tseResult.RequestId}.");
            }
            return tseResult;
        }

        private static TseResult CreateResultBasedOnCommandAndResponse(DieboldNixdorfCommand command, List<byte> response)
        {
            if (command == DieboldNixdorfCommand.GetCommandResponse)
            {
                return TseResultHelper.CreateTseResult(response.ToArray(), 5);
            }
            else
            {
                return TseResultHelper.CreateTseResult(response.ToArray());
            }
        }

        private static bool ContainsEndOfBuffer(List<byte> readBuffer) => readBuffer.Count >= 2 && readBuffer[readBuffer.Count - 1] == 0x9F && readBuffer[readBuffer.Count - 2] == 0x1b;

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
                    if (_hwSemaphore != null)
                    {
                        _hwSemaphore.Dispose();
                    }
                }
                _disposed = true;
            }
        }
    }
}