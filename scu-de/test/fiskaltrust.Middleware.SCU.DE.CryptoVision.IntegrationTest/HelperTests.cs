using System;
using Xunit;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;
using FluentAssertions;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.IntegrationTest
{
    public class HelperTests
    {
        [Fact]
        public void ByteArrayHelper_ToUInt16_ToUInt32_ToUInt64()
        {
            var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0, 0x0f, 0xed, 0xcb, 0xa9, 0x87, 0x65, 0x43, 0x21 };

            var unsigned_int_16 = data.ToUInt16();
            unsigned_int_16.Should().Be(0x1234);

            unsigned_int_16 = data.ToUInt16(3);
            unsigned_int_16.Should().Be(0x789a);

            var unsigned_int_32 = data.ToUInt32();
            unsigned_int_32.Should().Be(0x12345678);

            unsigned_int_32 = data.ToUInt32(2);
            unsigned_int_32.Should().Be(0x56789abc);

            var unsigned_int_64 = data.ToUInt64();
            unsigned_int_64.Should().Be(0x123456789abcdef0);

            unsigned_int_64 = data.ToUInt64(5);
            unsigned_int_64.Should().Be(0xbcdef00fedcba987);
        }

        [Fact]
        public void ByteArrayHelper_ToSmall_ToUInt16()
        {
            (new byte[] { }).ToUInt16().Should().Be(0x0000);
            (new byte[] { 0xa0 }).ToUInt16().Should().Be(0x00a0);
        }

        [Fact]
        public void ByteArrayHelper_ToSmall_ToUInt32()
        {
            (new byte[] { }).ToUInt32().Should().Be(0x0000);
            (new byte[] { 0xa0 }).ToUInt32().Should().Be(0x00a0);
        }

        [Fact]
        public void ByteArrayHelper_ToSmall_ToUInt64()
        {
            (new byte[] { }).ToUInt64().Should().Be(0x0000);
            (new byte[] { 0xa0 }).ToUInt64().Should().Be(0x00a0);
        }

        [Fact]
        public void ByteArrayHelper_ToOctetString()
        {
            var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0 };
            data.ToOctetString().ToUpper().Should().Be("123456789ABCDEF0");
        }

        [Fact]
        public void DateTimeHelper_ToTimestamp()
        {
            var dateTime = new DateTime(2020, 12, 05, 3, 5, 25, DateTimeKind.Utc);
            dateTime.ToTimestamp().Should().Be(1607137525);
        }

        [Fact]
        public void DateTimeHelper_FromTimestamp()
        {
            ulong timeStamp = 1589310209L;
            timeStamp.ToDateTime().Should().Be(new DateTime(2020, 5, 12, 19, 3, 29, DateTimeKind.Utc));
        }
    }
}
