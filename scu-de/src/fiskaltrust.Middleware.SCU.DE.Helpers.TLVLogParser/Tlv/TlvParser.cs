using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tlv.Models;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tlv
{
    public static class TlvParser
    {
        public static List<TlvRecord> ParseTlv(byte[] rawTlv)
        {
            var result = new List<TlvRecord>();
            for (int i = 0, start = 0; i < rawTlv.Length; start = i)
            {
                // parse Tag
                var constructedTlv = (rawTlv[i] & 0x20) != 0;
                var moreBytes = (rawTlv[i] & 0x1F) == 0x1F;
                while (moreBytes && (rawTlv[++i] & 0x80) != 0)
                { }

                i++;

                var tag = GetInt(rawTlv, start, i - start);
                var multiByteLength = (rawTlv[i] & 0x80) != 0;
                var length = multiByteLength ? GetInt(rawTlv, i + 1, rawTlv[i] & 0x1F) : rawTlv[i];
                i = multiByteLength ? i + (rawTlv[i] & 0x1F) + 1 : i + 1;
                i += length;

                var rawData = new byte[i - start];
                Array.Copy(rawTlv, start, rawData, 0, i - start);
                var tlv = new TlvRecord(tag, length, rawData.Length - length, rawData);
                result.Add(tlv);

                if (constructedTlv)
                {
                    tlv.Children.AddRange(ParseTlv(tlv.Value));
                }
            }
            return result;
        }

        private static int GetInt(byte[] data, int offset, int length)
        {
            var result = 0;
            for (var i = 0; i < length; i++)
            {
                result = (result << 8) | data[offset + i];
            }
            return result;
        }
    }
}