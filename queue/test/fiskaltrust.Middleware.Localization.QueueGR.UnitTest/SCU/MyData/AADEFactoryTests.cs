using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest;

public class AADEFactoryTests
{
    [Fact]
    public void MapToInvoicesDoc_ShouldThrowException_IfHandwritten_AndFieldsAreNotDefined()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001).WithFlag(ReceiptCaseFlags.HandWritten),
            ftReceiptCaseData = new
            {
                GR = new
                {
                    
                }
            }
        };
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft123#"
        };  

        var aadeFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
        {
            Account = new storage.V0.MasterData.AccountMasterData
            {
              
            },
        });

        var action = () => aadeFactory.MapToInvoicesDoc(receiptRequest, receiptResponse, []);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Customer is required for AADE invoice mapping.");
    }
}
