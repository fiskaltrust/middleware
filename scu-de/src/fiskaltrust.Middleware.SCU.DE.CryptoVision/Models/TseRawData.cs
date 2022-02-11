using System;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public class TseRawData : ITseData
    {
        public TseDataTypeEnum DataType { get; set; }

        public ushort DataLength => (ushort) DataBytes.Length;

        public byte[] DataBytes { get; set; } = Array.Empty<byte>();

        public byte[] Read() => DataBytes;

        public void Write(byte[] data) => DataBytes = data;
    }
}
