using System.Linq;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class ChargeItemCaseDataPrintingTests
    {
        private static ReceiptRequest CreateReceipt(params ChargeItem[] chargeItems) => new()
        {
            cbReceiptReference = "20260830000950400038",
            cbChargeItems = chargeItems,
            cbPayItems = new[]
            {
                new PayItem
                {
                    Quantity = 1,
                    Description = "Mastercard",
                    Amount = chargeItems.Sum(x => x.Amount),
                    ftPayItemCase = 0x4954_0000_0000_0004
                }
            },
            ftReceiptCase = 0x4954_2000_0000_0001,
            cbReceiptAmount = chargeItems.Sum(x => x.Amount)
        };

        private static ChargeItem CreateChargeItem(string caseData, long ftChargeItemCase = 0x4954_0000_0000_0003) => new()
        {
            Quantity = 1,
            Description = "Gonna a ruota in cotone",
            Amount = 80.1m,
            VATRate = 22m,
            ftChargeItemCase = ftChargeItemCase,
            ftChargeItemCaseData = caseData
        };

        [Theory]
        [InlineData("{\"netAmount\":65.66,\"discount\":8.90}")]
        [InlineData("plain text note")]
        [InlineData("")]
        [InlineData(null)]
        public void CreateInvoiceRequestContent_Should_Not_Print_ftChargeItemCaseData(string caseData)
        {
            var request = CreateReceipt(CreateChargeItem(caseData));

            var content = EpsonCommandFactory.CreateInvoiceRequestContent(new EpsonRTPrinterSCUConfiguration(), request);

            content.ItemAndMessages.Should().NotBeEmpty();
            content.ItemAndMessages.Where(x => x.PrintRecItem != null).Should().OnlyContain(x => x.PrintRecMessage == null);
        }

        [Fact]
        public void CreateInvoiceRequestContent_Should_Not_Print_ftChargeItemCaseData_For_Tips()
        {
            var tip = CreateChargeItem("{\"netAmount\":65.66}", ftChargeItemCase: 0x4954_0000_0000_0033);
            var request = CreateReceipt(tip);

            var content = EpsonCommandFactory.CreateInvoiceRequestContent(new EpsonRTPrinterSCUConfiguration(), request);

            content.ItemAndMessages.Should().NotBeEmpty();
            content.ItemAndMessages.Where(x => x.PrintRecItem != null).Should().OnlyContain(x => x.PrintRecMessage == null);
        }
    }
}
