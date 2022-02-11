using System;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication
{
    public interface ISerialCommunicationQueue : IDisposable
    {
        bool DeviceConnected { get; }
        bool SerialPortDeviceUnavailable { get; set; }

        void SendCommand(byte[] command, DieboldNixdorfCommand commandType, Guid requestId);
        TseResult SendCommandWithResult(byte[] command, Guid requestId, DieboldNixdorfCommand commandType, double timeOutInMilliSeconds = 100);
    }
}