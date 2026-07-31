using FluentAssertions;
using FluentAssertions.Execution;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.Abstraction.Validation;
using Xunit;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class CustomerTaxIdValidationTests
    {
        private const long PointOfSaleReceipt = 0x4954_2000_0000_0001;

        private const string InvalidCustomer = "{\"CustomerVATId\":\"12345\"}";

        private static ReceiptRequest CreateReceiptRequest(string cbCustomer, long ftReceiptCase = PointOfSaleReceipt) => new ReceiptRequest
        {
            ftReceiptCase = ftReceiptCase,
            cbReceiptReference = "test-reference",
            cbCustomer = cbCustomer
        };

        /// <summary>
        /// The whole point of the gate: a PoS that leaves a stale cbCustomer on a management receipt must
        /// never be prevented from closing the day or from recovering the till.
        /// </summary>
        [Theory]
        [InlineData((long) ITReceiptCases.InitialOperationReceipt0x4001)]
        [InlineData((long) ITReceiptCases.OutOfOperationReceipt0x4002)]
        [InlineData((long) ITReceiptCases.ZeroReceipt0x2000)]
        [InlineData((long) ITReceiptCases.DailyClosing0x2011)]
        [InlineData((long) ITReceiptCases.MonthlyClosing0x2012)]
        [InlineData((long) ITReceiptCases.YearlyClosing0x2013)]
        [InlineData((long) ITReceiptCases.Reprint0x3010)]
        public void CarriesCustomerTaxIds_ForAManagementReceiptWithAnInvalidCustomer_ReturnsFalse(long receiptCase)
        {
            var receiptRequest = CreateReceiptRequest(InvalidCustomer, ITConstants.BASE_STATE | receiptCase);

            receiptRequest.CarriesCustomerTaxIds().Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not json at all")]
        [InlineData("{\"CustomerName\":\"Mario Rossi\"}")]
        [InlineData("{\"CustomerId\":\"\",\"CustomerVATId\":\"\"}")]
        [InlineData("{\"CustomerId\":\"   \"}")]
        public void CarriesCustomerTaxIds_WithoutAnIdentifier_ReturnsFalse(string cbCustomer)
        {
            var receiptRequest = CreateReceiptRequest(cbCustomer);

            receiptRequest.CarriesCustomerTaxIds().Should().BeFalse();
        }

        [Theory]
        [InlineData("{\"CustomerId\":\"RSSMRA80A01H501U\"}")]
        [InlineData("{\"CustomerVATId\":\"01606720215\"}")]
        [InlineData(InvalidCustomer)]
        public void CarriesCustomerTaxIds_ForASaleReceiptWithAnIdentifier_ReturnsTrue(string cbCustomer)
        {
            var receiptRequest = CreateReceiptRequest(cbCustomer);

            receiptRequest.CarriesCustomerTaxIds().Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not json at all")]
        [InlineData("{\"CustomerName\":\"Mario Rossi\"}")]
        [InlineData("{\"CustomerId\":\"\",\"CustomerVATId\":\"\"}")]
        [InlineData("{\"CustomerId\":\"   \",\"CustomerVATId\":\"   \"}")]
        [InlineData("{\"CustomerId\":\"RSSMRA80A01H501U\",\"CustomerVATId\":\"01606720215\"}")]
        [InlineData("{\"CustomerId\":\"01606720215\"}")]
        [InlineData("{\"CustomerVATId\":\"IT01606720215\"}")]
        public void TryValidateCustomerTaxIds_WithAbsentOrValidIdentifiers_ReturnsTrue(string cbCustomer)
        {
            var receiptRequest = CreateReceiptRequest(cbCustomer);

            var isValid = receiptRequest.TryValidateCustomerTaxIds(out var errorMessage);

            using var scope = new AssertionScope();
            isValid.Should().BeTrue();
            errorMessage.Should().BeNull();
        }

        [Fact]
        public void TryValidateCustomerTaxIds_WithAnInvalidCodiceFiscale_ReturnsFalseAndNamesTheField()
        {
            var receiptRequest = CreateReceiptRequest("{\"CustomerId\":\"RSSMRA80A01H501Z\"}");

            var isValid = receiptRequest.TryValidateCustomerTaxIds(out var errorMessage);

            using var scope = new AssertionScope();
            isValid.Should().BeFalse();
            errorMessage.Should().Contain("RSSMRA80A01H501Z").And.Contain("cbCustomer.CustomerId");
        }

        [Fact]
        public void TryValidateCustomerTaxIds_WithAForeignVatNumber_ReturnsFalseAndNamesTheField()
        {
            var receiptRequest = CreateReceiptRequest("{\"CustomerVATId\":\"DE123456789\"}");

            var isValid = receiptRequest.TryValidateCustomerTaxIds(out var errorMessage);

            using var scope = new AssertionScope();
            isValid.Should().BeFalse();
            errorMessage.Should().Contain("DE123456789").And.Contain("cbCustomer.CustomerVATId");
        }

        [Fact]
        public void TryValidateCustomerTaxIds_WithBothIdentifiersInvalid_ReportsTheCodiceFiscaleFirst()
        {
            var receiptRequest = CreateReceiptRequest("{\"CustomerId\":\"RSSMRA80A01H501Z\",\"CustomerVATId\":\"12345\"}");

            var isValid = receiptRequest.TryValidateCustomerTaxIds(out var errorMessage);

            using var scope = new AssertionScope();
            isValid.Should().BeFalse();
            errorMessage.Should().Contain("cbCustomer.CustomerId").And.NotContain("cbCustomer.CustomerVATId");
        }
    }
}
