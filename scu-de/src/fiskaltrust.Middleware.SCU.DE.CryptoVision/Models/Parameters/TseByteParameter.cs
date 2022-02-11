namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters
{
    public class TseByteParameter : TseParameter<byte>
    {
        public override byte DataValue
        {
            get => DataBytes[0];
            set
            {
                DataType = TseDataTypeEnum.BYTE;
                DataBytes = new byte[] { value };
            }
        }

        public TseByteParameter() : base() { }

        public TseByteParameter(byte dataValue) : base(dataValue) { }
    }
}
