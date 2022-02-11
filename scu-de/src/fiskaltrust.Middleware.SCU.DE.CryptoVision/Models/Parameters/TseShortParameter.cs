using System;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters
{
    public class TseShortParameter : TseParameter<ushort>
    {
        public override ushort DataValue
        {
            get => (ushort) ((DataBytes[0] * 0x100) + DataBytes[1]);
            set
            {
                DataType = TseDataTypeEnum.SHORT;
                DataBytes = new byte[] { (byte) (value / 0x100), (byte) (value % 0x100) };
            }
        }

        public TseShortParameter() : base() { }

        public TseShortParameter(ushort dataValue) : base(dataValue) { }
    }
}
