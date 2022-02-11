using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.Helpers
{
    public static class ByteArrayHelper
    {
        public static string ToOctetString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }
    }
}
