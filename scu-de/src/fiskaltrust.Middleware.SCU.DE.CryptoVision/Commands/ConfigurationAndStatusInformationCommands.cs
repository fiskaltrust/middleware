using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public static class ConfigurationAndStatusInformationCommands
    {
        public static class GetStatusCommands
        {
            public static TseCommand CreateGetCurrentNumberOfClientsTseCommand() => new TseCommand(TseCommandCodeEnum.GetStatus, 0x0000, new TseShortParameter(0x0000));

            public static TseCommand CreateGetCurrentNumberOfTransactionsTseCommand() => new TseCommand(TseCommandCodeEnum.GetStatus, 0x0000, new TseShortParameter(0x0001));

            public static TseCommand CreateGetOpenTransactionsTseCommand() => new TseCommand(TseCommandCodeEnum.GetStatus, 0x0000, new TseShortParameter(0x0002));

            public static TseCommand CreateGetTransactionCounterTseCommand() => new TseCommand(TseCommandCodeEnum.GetStatus, 0x0000, new TseShortParameter(0x0003));

            public static TseCommand CreateGetLifeCycleStateTseCommand() => new TseCommand(TseCommandCodeEnum.GetStatus, 0x0000, new TseShortParameter(0x0004));

            public static TseCommand CreateGetTotalLogMemoryTseCommand() => new TseCommand(TseCommandCodeEnum.GetStatus, 0x0000, new TseShortParameter(0x0005));

            public static TseCommand CreateGetAvailableLogMemoryTseCommand() => new TseCommand(TseCommandCodeEnum.GetStatus, 0x0000, new TseShortParameter(0x0006));
        }

        public static class GetConfigDataCommands
        {
            public static TseCommand CreateGetSignatureAlgorithmTseCommand() => new TseCommand(TseCommandCodeEnum.GetConfigData, 0x0000, new TseShortParameter(0x0001));

            public static TseCommand CreateGetSupportedTransactionUpdateVariantsTseCommand() => new TseCommand(TseCommandCodeEnum.GetConfigData, 0x0000, new TseShortParameter(0x0002));

            public static TseCommand CreateGetMaxNumberOfClientsTseCommand() => new TseCommand(TseCommandCodeEnum.GetConfigData, 0x0000, new TseShortParameter(0x0004));

            public static TseCommand CreateGetMaxNumberOfTransactionsTseCommand() => new TseCommand(TseCommandCodeEnum.GetConfigData, 0x0000, new TseShortParameter(0x0005));

            public static TseCommand CreateGetTimeSyncIntervalTseCommand() => new TseCommand(TseCommandCodeEnum.GetConfigData, 0x0000, new TseShortParameter(0x0006));

            public static TseCommand CreateGetTimeSyncVariantTseCommand() => new TseCommand(TseCommandCodeEnum.GetConfigData, 0x0000, new TseShortParameter(0x0007));

            public static TseCommand CreateGetCertificationIdTseCommand() => new TseCommand(TseCommandCodeEnum.GetConfigData, 0x0000, new TseShortParameter(0x0008));
        }
    }
}
