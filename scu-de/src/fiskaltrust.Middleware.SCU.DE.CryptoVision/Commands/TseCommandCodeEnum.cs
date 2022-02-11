using System;

#pragma warning disable

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public enum TseCommandCodeEnum : ushort
    {
        Start = 0x0000,
        GetPinStates,
        InitializePins,
        AuthenticateUser,
        UnblockUser,
        Logout,
        Initialize,
        UpdateTime,
        GetSerialNumbers,
        MapERStoKey,
        StartTransaction,
        UpdateTransaction,
        FinishTransaction,
        ExportData,
        GetCertificates,
        ReadLogMessage,
        Erase,
        GetConfigData,
        GetStatus,
        Deactivate,
        Activate,
        Disable,
        ExportMoreData,
        GetERSMappings,
        GetKeyData,
        GetWearIndicator,
        UpdateCertificate,
        DeleteDataUpTo,
        UpdateFirmware = 0x0063,
        Shutdown = 0x00FF
    }
}