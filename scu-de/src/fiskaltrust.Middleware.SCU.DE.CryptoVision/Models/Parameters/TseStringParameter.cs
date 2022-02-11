using System;
using System.Text;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public class TseStringParameter : TseParameter<string>
    {
        public override string DataValue
        {
            get => Encoding.ASCII.GetString(DataBytes);
            set
            {
                DataType = TseDataTypeEnum.STRING;
                DataBytes = string.IsNullOrEmpty(value) ? Array.Empty<byte>() : Encoding.ASCII.GetBytes(value);
            }
        }

        public TseStringParameter() : base() { }

        public TseStringParameter(string dataValue) : base(dataValue)
        {
            if (dataValue == null)
            {
                DataValue = string.Empty;
            }
        }
    }
}
