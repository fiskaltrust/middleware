using System;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public static class RequestHelper
    {
        public static string GetParameterValueAsAsciiDigit(bool value) => value ? "1" : "0";

        public static string AsDateTime(DateTime dateTime) => dateTime.ToString("dd.MM.yyyy HH:mm:ss");

        public static string GetParameterValueAsAsciiDigit(int value) => value.ToString();

        public static string AsAsciiDigit(long value) => value.ToString();

        public static string GetParameterForSlotNumber(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > 10)
            {
                throw new ArgumentOutOfRangeException($"Parameter {nameof(slotNumber)} should be 1 - 10. Given value {slotNumber} is invalid.");
            }
            return slotNumber.ToString();
        }

        public static string AsBase64(byte[] value) => Convert.ToBase64String(value);

        public static string AsAsn1Printable(string processType) => processType;
    }
}