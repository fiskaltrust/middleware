using System;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public class RawTseCommand : ITseCommand
    {
        private readonly byte[] _rawCommandData = Array.Empty<byte>();
        private readonly ushort _responseMode;

        public byte[] ResponseModeBytes => _responseMode.ToBytes();

        public RawTseCommand(byte[] rawCommandData, ushort responseMode = 0x0000)
        {
            _rawCommandData = rawCommandData;
            _responseMode = responseMode;
        }

        public byte[] GetCommandDataBytes() => _rawCommandData;
    }
}
