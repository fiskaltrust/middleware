using System;
using System.Linq;
using System.Text;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tlv.Models;
using Org.BouncyCastle.Asn1;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tlv
{
    public static class TlvParseHelpers
    {
        public static int ParseInteger(byte[] value)
        {
            var tempArray = value.ToArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(tempArray);
            }

            if (tempArray.Length < 4)
            {
                var temp = new byte[4];
                tempArray.CopyTo(temp, 0);
                tempArray = temp;
            }

            return BitConverter.ToInt32(tempArray, 0);
        }

        public static DateTime ParseUnixTimeStamp(byte[] value) => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ParseInteger(value));

        public static DateTime ParseUtcTime(byte[] value) => DateTime.ParseExact(Encoding.UTF8.GetString(value), "yyMMddHHmmssZ", null);

        public static DateTime ParseGeneralizedTime(byte[] value) => DateTime.ParseExact(Encoding.UTF8.GetString(value), "YYYYMMDDhhmmss.fffZ", null);

        public static string ParseObjectIdentifier(TlvRecord tlv)
        {
            var result = new Asn1StreamParser(tlv.RawData);
            var asn1Object = result.ReadObject();
            return asn1Object.ToString();
        }
    }
}
