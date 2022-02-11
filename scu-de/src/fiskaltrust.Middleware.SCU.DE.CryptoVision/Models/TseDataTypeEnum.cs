using System.Diagnostics.CodeAnalysis;

#pragma warning disable
namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public enum TseDataTypeEnum : byte
    {
        BYTE = 01,
        BYTE_ARRAY,
        SHORT,
        STRING,
        LONG_ARRAY,
        ERROR = 0x80,
        RAW = 0x90,
        UNKNOWN = 0x00
    }
}
