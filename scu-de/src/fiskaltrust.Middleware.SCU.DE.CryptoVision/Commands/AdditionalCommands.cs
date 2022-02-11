using System;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public static class AdditionalCommands
    {
        public static TseCommand CreateDeleteDataUpToTseCommand(byte[] serialNumber, long signatureCounter) => new TseCommand(TseCommandCodeEnum.DeleteDataUpTo, 0x0000, new TseByteArrayParameter(serialNumber), new TseByteArrayParameter(signatureCounter.ToByteArray()));

        public static TseCommand CreateExportMoreDataTseCommand(byte[] serialNumber, long previousSignatureCounter, uint maxNumberOfRecords = 0) => new TseCommand(TseCommandCodeEnum.ExportMoreData, 0x0000, new TseByteArrayParameter(serialNumber), new TseByteArrayParameter(previousSignatureCounter.ToByteArray()), new TseByteArrayParameter(maxNumberOfRecords.ToByteArray()));

        public static TseCommand CreateExportPublicKeyTseCommand(byte[] serialNumber) => new TseCommand(TseCommandCodeEnum.GetKeyData, 0x0000, new TseShortParameter(0x0002), new TseByteArrayParameter(serialNumber));

        public static TseCommand CreateGetCertificateExpirationDateTseCommand(byte[] serialNumber) => new TseCommand(TseCommandCodeEnum.GetKeyData, 0x0000, new TseShortParameter(0x0001), new TseByteArrayParameter(serialNumber));

        public static TseCommand CreateMapERStoKeyTseCommand(string clientId, byte[] serialNumber) => new TseCommand(TseCommandCodeEnum.MapERStoKey, 0x0000, new TseStringParameter(clientId), new TseByteArrayParameter(serialNumber));

        public static TseCommand CreateGetSignatureCounterTseCommand(byte[] serialNumber) => new TseCommand(TseCommandCodeEnum.GetKeyData, 0x0000, new TseShortParameter(0x0000), new TseByteArrayParameter(serialNumber));

        public static TseCommand CreateInitializePinsTseCommand(byte[] adminPuk, byte[] adminPin, byte[] timeAdminPuk, byte[] timeAdminPin)
        {
            if (adminPuk.Length != 10)
            {
                throw new CryptoVisionProxyException(SeResult.ErrorParameterMismatch);
            }
            if (adminPin.Length != 8)
            {
                throw new CryptoVisionProxyException(SeResult.ErrorParameterMismatch);
            }
            if (timeAdminPuk.Length != 10)
            {
                throw new CryptoVisionProxyException(SeResult.ErrorParameterMismatch);
            }
            if (timeAdminPin.Length != 8)
            {
                throw new CryptoVisionProxyException(SeResult.ErrorParameterMismatch);
            }
            return new TseCommand(TseCommandCodeEnum.InitializePins, 0x0000, new TseByteArrayParameter(adminPuk), new TseByteArrayParameter(adminPin), new TseByteArrayParameter(timeAdminPuk), new TseByteArrayParameter(timeAdminPin));
        }
    }
}
