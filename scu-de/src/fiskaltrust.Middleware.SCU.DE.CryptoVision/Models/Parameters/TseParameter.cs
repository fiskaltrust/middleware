using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters
{
    public abstract class TseParameter<T> : ITseData
    {
        private byte[] dataBytes = Array.Empty<byte>();

        public TseDataTypeEnum DataType { get; set; }

        public ushort DataLength { get; private set; }

        public byte[] DataBytes
        {
            get => dataBytes;
            set
            {
                if (value != null)
                {
                    dataBytes = value;
                    DataLength = (ushort) dataBytes.Length;
                }
                else
                {
                    DataBytes = Array.Empty<byte>();
                    DataLength = 0;
                }
            }
        }

        public abstract T DataValue { get; set; }

        protected TseParameter() { }

        protected TseParameter(T dataValue) => DataValue = dataValue;

        public byte[] Read()
        {
            var buffer = new byte[DataLength + 3];
            Buffer.SetByte(buffer, 0, (byte) DataType);
            Buffer.SetByte(buffer, 1, (byte) (DataLength / 0x100));
            Buffer.SetByte(buffer, 2, (byte) (DataLength % 0x100));
            if (DataLength > 0)
            {
                Buffer.BlockCopy(dataBytes, 0, buffer, 3, DataLength);
            }
            return buffer;
        }

        public void Write(byte[] parameterBytes)
        {
            var len = (ushort) (parameterBytes[1] << 8);
            len += parameterBytes[2];
            if (parameterBytes.Length != len + 3)
            {
                throw new ArgumentException("ParameterBytes should have had a different value.", nameof(parameterBytes));
            }

            DataType = (TseDataTypeEnum) parameterBytes[0];
            DataLength = len;

            dataBytes = new byte[len];
            if (len > 0)
            {
                Buffer.BlockCopy(parameterBytes, 3, dataBytes, 0, len);
            }
        }
    }
}
