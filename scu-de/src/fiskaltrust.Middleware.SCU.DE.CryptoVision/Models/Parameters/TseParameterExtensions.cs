using System;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters
{
    public static class TseParameterExtensions
    {
        public static ITseData ToTseParameter(this byte[] buffer, int offset = 0)
        {
            var length = 0;
            if (buffer.Length >= offset - 1 + 2)
            {
                // 2 bytes length are readable
                length += buffer[offset + 1] << 8;
                length += buffer[offset + 2];
            }

            var parameterBuffer = new byte[length + 3];
            if (length > 0 && offset + parameterBuffer.Length <= buffer.Length)
            {
                Buffer.BlockCopy(buffer, offset, parameterBuffer, 0, parameterBuffer.Length);
            }
            else
            {
                // tse bug on long-array-return-type.
                // observation: given 4 open transactions gave an transportation-response-length of 21 and a prameter-response-length of 23. this numbers are exact exchanged.
                // this happen on 'GetOpenTransactions'
                // if this is the case, correct lenth to avoid buffer-overrun.
                Buffer.BlockCopy(buffer, offset, parameterBuffer, 0, buffer.Length - offset);
            }

            ITseData parameter = ((TseDataTypeEnum) buffer[offset]) switch
            {
                TseDataTypeEnum.BYTE => new TseByteParameter(),
                TseDataTypeEnum.SHORT => new TseShortParameter(),
                TseDataTypeEnum.STRING => new TseStringParameter(),
                TseDataTypeEnum.LONG_ARRAY => new TseLongArrayParameter(),
                TseDataTypeEnum.BYTE_ARRAY => new TseByteArrayParameter(),
                _ => new TseRawData(),
            };
            parameter.Write(parameterBuffer);
            return parameter;
        }
    }
}
