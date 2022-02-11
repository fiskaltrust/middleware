using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters
{
    public class TseLongArrayParameter : TseParameter<uint[]>
    {
        public override uint[] DataValue
        {
            get
            {
                var dataList = new List<uint>();
                for (var position = 0; position < DataLength; position += 4)
                {
                    var value = (uint) (DataBytes[position] << 24) + (uint) (DataBytes[position + 1] << 16) + (uint) (DataBytes[position + 2] << 8) + DataBytes[position + 3];
                    dataList.Add(value);
                }
                return dataList.ToArray();
            }
            set
            {
                DataType = TseDataTypeEnum.LONG_ARRAY;
                var buffer = new byte[value.Length * 4];
                for (var i = 0; i < value.Length; i++)
                {
                    buffer[(i * 4) + 0] = (byte) ((value[i] >> 24) % 0x100);
                    buffer[(i * 4) + 1] = (byte) ((value[i] >> 16) % 0x100);
                    buffer[(i * 4) + 2] = (byte) ((value[i] >> 8) % 0x100);
                    buffer[(i * 4) + 3] = (byte) (value[i] % 0x100);
                }
                DataBytes = buffer;
            }
        }

        public TseLongArrayParameter() : base() { }

        public TseLongArrayParameter(uint[] dataValue) : base(dataValue) { }
    }
}
