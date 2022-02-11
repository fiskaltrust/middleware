namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public interface ITseData
    {
        TseDataTypeEnum DataType { get; set; }

        ushort DataLength { get; }

        byte[] DataBytes { get; set; }

        byte[] Read();

        void Write(byte[] data);
    }
}
