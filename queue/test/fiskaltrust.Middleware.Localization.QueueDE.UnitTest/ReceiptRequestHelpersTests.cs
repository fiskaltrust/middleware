using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using FluentAssertions;
using Xunit;


namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest
{
    public class ReceiptRequestHelpersTests
    {
        [Fact]
        public void IsImplictFlow_ForImplictReceiptCase_ShouldReturnTrue()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x4445_0001_0000_0010
            };

            receiptRequest.IsImplictFlow().Should().BeTrue();
        }

        [Fact]
        public void IsImplictFlow_ForExplicitReceiptCase_ShouldReturnFalse()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x4445_0000_0000_0010
            };

            receiptRequest.IsImplictFlow().Should().BeFalse();
        }

        [Fact]
        public void GetReceiptIdentification_ForExplicitReceiptCase_ShouldReturn_IdentificationWithout_TransactionPrefix()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x4445_0000_0000_0010
            };

            receiptRequest.GetReceiptIdentification(10, 20).Should().Be("ftA#T20");
        }

        [Fact]
        public void GetReceiptIdentification_ForImplicitReceiptCase_ShouldReturn_IdentificationWith_TransactionPrefix()
        {
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = 0x4445_0001_0000_0010
            };

            receiptRequest.GetReceiptIdentification(10, 20).Should().Be("ftA#IT20");
        }

        [Fact]
        public void GetReceiptActionStartMoment_ShouldNotThrow_If_PayItemsAreEmpty()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbPayItems = new PayItem[0],
                cbChargeItems = null
            };
            receiptRequest.GetReceiptActionStartMoment();
        }

        [Fact]
        public void GetReceiptActionStartMoment_ShouldNotThrow_If_ChargeItemsAreEmpty()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbPayItems = null,
                cbChargeItems = new ChargeItem[0]
            };
            receiptRequest.GetReceiptActionStartMoment();
        }
    }
}
