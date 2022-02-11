using System;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Helpers.ExceptionHelper
{
    public static class ByteHelpers
    {
        public static string ToOctetString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }
    }
}
