using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tlv.Models
{
#pragma warning disable CA1819 // Properties should not return arrays
    public class TlvRecord
    {
        public int Tag { get; }
        public byte[] RawData { get; }
        public byte[] Value { get; }

        public List<TlvRecord> Children { get; }

        public TlvRecord(int tag, int length, int valueOffset, byte[] data)
        {
            Tag = tag;
            RawData = data;
            var result = new byte[length];
            Array.Copy(RawData, valueOffset, result, 0, length);
            Value = result;
            Children = new List<TlvRecord>();
        }
    }
}