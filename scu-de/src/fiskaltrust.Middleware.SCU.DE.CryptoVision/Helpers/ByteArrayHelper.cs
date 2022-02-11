using System;
using System.Linq;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers
{

    public static class ByteArrayHelper
    {
        public static string ToOctetString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        public static byte[] FromOctetString(this string data)
        {
            throw new NotImplementedException();
        }

        public static string ToBase64String(this byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public static byte[] FromBase64String(this string data)
        {
            return Convert.FromBase64String(data ?? string.Empty);
        }

        public static UInt16 ToUInt16(this byte[] data, int offset = 0, int length = sizeof(UInt16))
        {
            UInt16 value = 0;
            foreach (var item in data.Skip(offset).Take(length).ToArray())
            {
                value <<= 8;
                value += item;
            }
            return value;
        }

        public static UInt32 ToUInt32(this byte[] data, int offset = 0, int length = sizeof(UInt32))
        {
            UInt32 value = 0;
            foreach (var item in data.Skip(offset).Take(length).ToArray())
            {
                value <<= 8;
                value += item;
            }
            return value;
        }

        public static UInt64 ToUInt64(this byte[] data, int offset = 0, int length = sizeof(UInt64))
        {
            UInt64 value = 0;
            foreach (var item in data.Skip(offset).Take(length).ToArray())
            {
                value <<= 8;
                value += item;
            }
            return value;
        }

        public static Int16 ToInt16(this byte[] data, int offset = 0, int length = sizeof(Int16))
        {
            Int16 value = 0;
            foreach (var item in data.Skip(offset).Take(length).ToArray())
            {
                value <<= 8;
                value += item;
            }
            return value;
        }

        public static Int32 ToInt32(this byte[] data, int offset = 0, int length = sizeof(Int32))
        {
            var value = 0;
            foreach (var item in data.Skip(offset).Take(length).ToArray())
            {
                value <<= 8;
                value += item;
            }
            return value;
        }

        public static Int64 ToInt64(this byte[] data, int offset = 0, int length = sizeof(Int64))
        {
            Int64 value = 0;
            foreach (var item in data.Skip(offset).Take(length).ToArray())
            {
                value <<= 8;
                value += item;
            }
            return value;
        }

        public static byte[] ToByteArray(this UInt16 data)
        {
            return new byte[] {
                (byte) ((data >> 8) % 0x100),
                (byte) (data % 0x100)
            };
        }
        public static byte[] ToByteArray(this UInt32 data)
        {
            return new byte[] {
                (byte) ((data >> 24) % 0x100),
                (byte) ((data >> 16) % 0x100),
                (byte) ((data >> 8) % 0x100),
                (byte) (data % 0x100)
            };
        }
        public static byte[] ToByteArray(this UInt64 data)
        {
            return new byte[] {
                (byte) ((data >> 56) % 0x100),
                (byte) ((data >> 48) % 0x100),
                (byte) ((data >> 40) % 0x100),
                (byte) ((data >> 32) % 0x100),
                (byte) ((data >> 24) % 0x100),
                (byte) ((data >> 16) % 0x100),
                (byte) ((data >> 8) % 0x100),
                (byte) (data % 0x100)
            };
        }

        public static byte[] ToByteArray(this Int16 data)
        {
            return new byte[] {
                (byte) ((data >> 8) % 0x100),
                (byte) (data % 0x100)
            };
        }
        public static byte[] ToByteArray(this Int32 data)
        {
            return new byte[] {
                (byte) ((data >> 24) % 0x100),
                (byte) ((data >> 16) % 0x100),
                (byte) ((data >> 8) % 0x100),
                (byte) (data % 0x100)
            };
        }
        public static byte[] ToByteArray(this Int64 data)
        {
            return new byte[] {
                (byte) ((data >> 56) % 0x100),
                (byte) ((data >> 48) % 0x100),
                (byte) ((data >> 40) % 0x100),
                (byte) ((data >> 32) % 0x100),
                (byte) ((data >> 24) % 0x100),
                (byte) ((data >> 16) % 0x100),
                (byte) ((data >> 8) % 0x100),
                (byte) (data % 0x100)
            };
        }
    }

}
