using System;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters
{
    public class TseByteArrayParameter : TseParameter<byte[]>
    {
        public override byte[] DataValue
        {
            get => DataBytes;
            set
            {
                DataType = TseDataTypeEnum.BYTE_ARRAY;
                DataBytes = value;
            }
        }

        public TseByteArrayParameter() : base() { }

        public TseByteArrayParameter(byte[] dataValue) : base(dataValue)
        {
            if (dataValue == null)
            {
                DataValue = Array.Empty<byte>();
            }
        }
    }
}
