using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public class TseCommand : ITseCommand
    {
        private const string CommandTagBase64 = "XFQ=";
        private readonly byte[] CommandTagBytes = Convert.FromBase64String(CommandTagBase64);

        private readonly TseCommandCodeEnum _commandCode;
        private readonly ITseData[] _commandParameters;
        private readonly ushort _responseMode;

        public byte[] ResponseModeBytes => _responseMode.ToBytes();


        public TseCommand(TseCommandCodeEnum commandCode, ushort responseMode = 0x0000, params ITseData[] commandParameters)
        {
            _commandCode = commandCode;
            _responseMode = responseMode;
            _commandParameters = commandParameters;
        }

        protected IEnumerable<byte[]> CreateCommandBytes(TseCommandCodeEnum commandCode, params ITseData[] parameters)
        {
            yield return CommandTagBytes;
            yield return commandCode.ToBytes();

            var ParametersBytes = parameters.Select(p => p.Read());
            var ParametersBytesLength = ParametersBytes.Sum(p => p.Length);
            yield return ((ushort) ParametersBytesLength).ToBytes();
            foreach (var bytes in ParametersBytes)
            {
                yield return bytes;
            }
        }

        public byte[] GetCommandDataBytes()
        {
            var commandBytes = CreateCommandBytes(_commandCode, _commandParameters);
            var commandBytesLength = (ushort) commandBytes.Sum(c => c.Length);

            var commandDataBytes = new byte[commandBytesLength];
            var offset = 0;
            foreach (var commandFragment in commandBytes)
            {
                Buffer.BlockCopy(commandFragment, 0, commandDataBytes, offset, commandFragment.Length);
                offset += commandFragment.Length;
            }
            return commandDataBytes;
        }
    }
}
