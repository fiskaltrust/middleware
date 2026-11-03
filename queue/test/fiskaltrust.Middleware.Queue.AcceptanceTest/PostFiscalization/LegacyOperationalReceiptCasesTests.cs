using System;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Queue.PostFiscalization;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest.PostFiscalization
{
    public class LegacyOperationalReceiptCasesTests
    {
        [Theory]
        [InlineData(0x4445_0000_0000_0002L, true)]  // DE zero receipt
        [InlineData(0x4445_0000_0000_0003L, true)]  // DE initial operation
        [InlineData(0x4445_0000_0000_0007L, true)]  // DE daily closing
        [InlineData(0x4445_0000_0000_0001L, false)] // DE POS receipt
        [InlineData(0x4445_0000_0000_000CL, false)] // DE B2B invoice
        [InlineData(0x4154_0000_0000_0004L, true)]  // AT out of operation
        [InlineData(0x4154_0000_0000_0007L, false)] // AT: 0x0007 is not a closing
        [InlineData(0x4D45_0000_0000_0005L, true)]  // ME monthly closing
        [InlineData(0x4D45_0000_0000_0007L, false)] // ME cash deposit: a business receipt
        [InlineData(0x4652_0000_0000_0010L, true)]  // FR start receipt
        [InlineData(0x4652_0000_0000_000FL, true)]  // FR zero receipt
        [InlineData(0x4652_0000_0000_0003L, false)] // FR invoice
        [InlineData(0x4954_2000_0000_4001L, true)]  // IT initial operation (type nibble)
        [InlineData(0x4954_2000_0000_2000L, true)]  // IT zero receipt (type nibble)
        [InlineData(0x4954_2000_0000_1001L, false)] // IT invoice
        [InlineData(0x0000_0000_0000_0002L, true)]  // countryless v1 case: the shared numbering
        public void IsOperationalReceipt_KnowsEveryMarketsNumbering(long ftReceiptCase, bool expected)
        {
            LegacyOperationalReceiptCases.IsOperationalReceipt(new V2.ReceiptRequest { ftReceiptCase = (ReceiptCase) unchecked((ulong) ftReceiptCase) }).Should().Be(expected);
        }

        [Theory]
        [InlineData("DE", 0x4445_0000_0000_0003L, true)]
        [InlineData("DE", 0x4445_0000_0000_0002L, false)]
        [InlineData("FR", 0x4652_0000_0000_0010L, true)]
        [InlineData("FR", 0x4652_0000_0000_0003L, false)]
        [InlineData("IT", 0x4954_2000_0000_4001L, true)]
        [InlineData("IT", 0x4954_2000_0000_0003L, false)]
        public void IsActivationReceipt_IsTheMarketsStartReceipt(string country, long ftReceiptCase, bool expected)
        {
            LegacyOperationalReceiptCases.IsActivationReceipt(country, ftReceiptCase).Should().Be(expected);
        }

        [Fact]
        public void MayBeFiscalized_OnAnOperationalQueue_OrForTheActivationReceiptOfANewQueue()
        {
            var operational = new ftQueue { StartMoment = DateTime.UtcNow.AddDays(-1) };
            var fresh = new ftQueue();
            var deactivated = new ftQueue { StartMoment = DateTime.UtcNow.AddDays(-1), StopMoment = DateTime.UtcNow };

            LegacyOperationalReceiptCases.MayBeFiscalized(operational, "DE", 0x4445_0000_0000_0001L).Should().BeTrue();
            LegacyOperationalReceiptCases.MayBeFiscalized(fresh, "DE", 0x4445_0000_0000_0003L).Should().BeTrue("the activation receipt makes the queue operational");
            LegacyOperationalReceiptCases.MayBeFiscalized(fresh, "DE", 0x4445_0000_0000_0001L).Should().BeFalse("the market answers it without fiscalizing");
            LegacyOperationalReceiptCases.MayBeFiscalized(deactivated, "DE", 0x4445_0000_0000_0003L).Should().BeFalse("a deactivated queue cannot be activated again");
            LegacyOperationalReceiptCases.MayBeFiscalized(deactivated, "DE", 0x4445_0000_0000_0001L).Should().BeFalse();
        }
    }
}
