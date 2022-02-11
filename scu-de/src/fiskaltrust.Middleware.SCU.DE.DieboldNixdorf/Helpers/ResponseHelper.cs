using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public static class ResponseHelper
    {
        public static byte FromAsciiHexDigitToByte(List<byte> value) => Convert.ToByte(int.Parse(Encoding.ASCII.GetString(value.ToArray()), NumberStyles.HexNumber));

        public static byte[] FromAsciiHexDigitToBytes(List<byte> value)
        {
            var number1 = int.Parse(Encoding.ASCII.GetString(new byte[] { value[0], value[1] }), NumberStyles.HexNumber);
            var number2 = int.Parse(Encoding.ASCII.GetString(new byte[] { value[2], value[3] }), NumberStyles.HexNumber);
            return new byte[]
            {
               Convert.ToByte(number1),
               Convert.ToByte(number2)
            };
        }

        public static long GetResultForAsciiDigit(List<byte> value) => long.Parse(Encoding.ASCII.GetString(value.ToArray()));

        public static bool TryGetResultForAsciiDigit(List<byte> value, out long result, long defaultValue = -1)
        {
            try
            {
                if (value.Any())
                {
                    result = GetResultForAsciiDigit(value);
                    return true;
                }
            }
            catch (Exception) { }

            result = defaultValue;
            return false;
        }

        public static string GetResultForAsciiHexDigit(List<byte> value) => Encoding.ASCII.GetString(value.ToArray());

        public static string GetResultForAsciiPrintable(List<byte> value) => Encoding.ASCII.GetString(value.ToArray());

        public static string GetResultForAsciiAlpha(List<byte> value) => Encoding.ASCII.GetString(value.ToArray());

        public static DateTime FromDateTime(List<byte> value) => DateTime.ParseExact(Encoding.ASCII.GetString(value.ToArray()), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        public static string GetResultForBase64(List<byte> value) => Encoding.ASCII.GetString(value.ToArray());
    }
}
