using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Queue.PostFiscalization;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest.PostFiscalization
{
    /// <summary>The non-fiscalizing answers of the legacy markets, which must not reach the RFC 712 finalize phase.</summary>
    public class LegacyFiscalizationOutcomeTests
    {
        [Theory]
        // failed receipts, every market
        [InlineData("DE", 0x4445_0000_EEEE_EEEEL, true, true, false)]
        [InlineData("IT", 0x4954_2000_EEEE_EEEEL, true, true, false)]
        // the queue-state early exit (bit 0): not operational before, not made operational
        [InlineData("DE", 0x4445_0000_0000_0001L, false, false, false)]
        [InlineData("AT", 0x4154_0000_0000_0001L, false, false, false)]
        [InlineData("FR", 0x4652_0000_0000_0001L, false, false, false)]
        [InlineData("IT", 0x4954_2000_0000_0001L, false, false, false)]
        // the out-of-operation receipt carries bit 0 but was processed on the operational queue
        [InlineData("DE", 0x4445_0000_0000_0001L, true, false, true)]
        [InlineData("IT", 0x4954_2000_0000_0001L, true, false, true)]
        // the initial-operation receipt makes the queue operational
        [InlineData("DE", 0x4445_0000_0000_0000L, false, true, true)]
        [InlineData("AT", 0x4154_0000_0000_0080L, false, true, true)]
        // DE: an SCU switch in progress answers without signing; SSCD failed mode and late signing are recorded receipts
        [InlineData("DE", 0x4445_0000_0000_0100L, true, true, false)]
        [InlineData("DE", 0x4445_0000_0000_0002L, true, true, true)]
        [InlineData("DE", 0x4445_0000_0000_0008L, true, true, true)]
        [InlineData("DE", 0x4445_0000_0000_0000L, true, true, true)]
        // ME: SetStateToError codes are receipts that were not processed; SSCD failed mode is a recorded receipt
        [InlineData("ME", 0x4D45_0000_0000_0003L, true, true, false)]
        [InlineData("ME", 0x4D45_0000_0000_0004L, true, true, false)]
        [InlineData("ME", 0x4D45_0000_0000_0005L, true, true, false)]
        [InlineData("ME", 0x4D45_0000_0000_0002L, true, true, true)]
        [InlineData("ME", 0x4D45_0000_0000_0000L, true, true, true)]
        // AT and FR informational flags on processed receipts
        [InlineData("AT", 0x4154_0000_0000_0082L, true, true, true)]
        [InlineData("AT", 0x4154_0000_0000_0030L, true, true, true)]
        [InlineData("FR", 0x4652_0000_0000_0040L, true, true, true)]
        [InlineData("IT", 0x4954_2000_0000_0000L, true, true, true)]
        // the market is matched case-insensitively, and an unknown market only knows the common rules
        [InlineData("de", 0x4445_0000_0000_0100L, true, true, false)]
        [InlineData(null, 0x0000_0000_0000_0000L, true, true, true)]
        public void IsFiscalized_KnowsTheNonFiscalizingAnswersOfEveryMarket(string country, long ftState, bool queueWasOperational, bool queueIsOperational, bool expected)
        {
            // every processed receipt carries signature items; for AT that is the outcome itself
            var signed = new ReceiptResponse { ftState = ftState, ftSignatures = new[] { new SignaturItem { Caption = "signature", Data = "...", ftSignatureFormat = 1, ftSignatureType = 1 } } };

            LegacyFiscalizationOutcome.IsFiscalized(country, signed, queueWasOperational, queueIsOperational).Should().Be(expected);
        }

        [Theory]
        [InlineData(0x4154_0000_0000_0000L, true, true)]  // a lifecycle request with charge or pay items: the unsigned default response
        [InlineData(0x4154_0000_0000_0000L, false, true)] // the same for an initial-operation request on a new queue
        [InlineData(0x4154_0000_0000_0001L, false, false)] // the disabled-queue answer is unsigned as well
        public void IsFiscalized_TreatsAnUnsignedAtAnswerAsNotFiscalized(long ftState, bool queueWasOperational, bool queueIsOperational)
        {
            LegacyFiscalizationOutcome.IsFiscalized("AT", new ReceiptResponse { ftState = ftState, ftSignatures = new SignaturItem[0] }, queueWasOperational, queueIsOperational).Should().BeFalse();
            LegacyFiscalizationOutcome.IsFiscalized("AT", new ReceiptResponse { ftState = ftState, ftSignatures = null }, queueWasOperational, queueIsOperational).Should().BeFalse();
        }

        [Fact]
        public void IsFiscalized_DoesNotNeedSignaturesOutsideAt()
        {
            LegacyFiscalizationOutcome.IsFiscalized("DE", new ReceiptResponse { ftState = 0x4445_0000_0000_0000L, ftSignatures = new SignaturItem[0] }, true, true).Should().BeTrue();
            LegacyFiscalizationOutcome.IsFiscalized("IT", new ReceiptResponse { ftState = 0x4954_2000_0000_0000L, ftSignatures = null }, true, true).Should().BeTrue();
        }

        [Fact]
        public void IsOperational_RequiresAStartAndNoStop()
        {
            LegacyFiscalizationOutcome.IsOperational(new ftQueue()).Should().BeFalse();
            LegacyFiscalizationOutcome.IsOperational(new ftQueue { StartMoment = DateTime.UtcNow }).Should().BeTrue();
            LegacyFiscalizationOutcome.IsOperational(new ftQueue { StartMoment = DateTime.UtcNow.AddDays(-1), StopMoment = DateTime.UtcNow }).Should().BeFalse();
        }
    }
}
