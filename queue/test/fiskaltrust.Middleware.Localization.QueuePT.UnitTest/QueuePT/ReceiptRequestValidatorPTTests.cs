using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT;

public class ReceiptRequestValidatorPTTests
{
    [Fact]
    public void Test()
    {
        var chargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = PTVATRates.Discounted1,
                    VATAmount = VATHelpers.CalculateVAT(100, PTVATRates.Discounted1),
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0011,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            };
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbUser = 1,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_1001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_1001,
        };
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(receiptRequest);
    }
}
