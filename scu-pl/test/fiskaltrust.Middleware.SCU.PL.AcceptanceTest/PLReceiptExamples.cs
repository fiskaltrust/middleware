using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest;

/// <summary>PLN receipt examples mirroring the POSNET specification's sale scenarios.</summary>
public static class PLReceiptExamples
{
    public static ProcessRequest CashSale() => Wrap(new ReceiptRequest
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        Currency = Currency.PLN,
        cbChargeItems =
        [
            // VAT case 1 (Discounted-1, 8%) resolves to PTU slot B of the default rate table.
            new ChargeItem { Description = "Candies", Amount = 9.99m, Quantity = 1m, VATRate = 8m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0011, Currency = Currency.PLN },
        ],
        cbPayItems =
        [
            new PayItem { Description = "Gotówka", Amount = 9.99m, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001, Currency = Currency.PLN },
        ],
    });

    /// <summary>The specification's transaction-end example: 2.00 sale, 5.00 card, 3.00 change.</summary>
    public static ProcessRequest CardSaleWithChange() => Wrap(new ReceiptRequest
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        Currency = Currency.PLN,
        cbChargeItems =
        [
            new ChargeItem { Description = "Apples", Amount = 2.00m, Quantity = 1m, VATRate = 8m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0011, Currency = Currency.PLN },
        ],
        cbPayItems =
        [
            new PayItem { Description = "Karta", Amount = 5.00m, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0005, Currency = Currency.PLN },
            new PayItem { Description = "Reszta", Amount = -3.00m, ftPayItemCase = (PayItemCase)0x504C_2000_0020_0001, Currency = Currency.PLN },
        ],
    });

    /// <summary>A paragon z NIP: ReceiverIsBusiness flag with the buyer's NIP in cbCustomer.</summary>
    public static ProcessRequest NipReceipt()
    {
        var request = CashSale();
        request.ReceiptRequest.ftReceiptCase = (ReceiptCase)0x504C_2000_0020_0001;
        request.ReceiptRequest.cbCustomer = """{"CustomerVATId": "123-456-32-18"}""";
        return request;
    }

    /// <summary>An e-paragon sale: the e-receipt customer identifier (IDZ) travels in cbCustomer.</summary>
    public static ProcessRequest EReceiptSale(string eReceiptCustomerId = "KID0123456789ABC")
    {
        var request = CashSale();
        request.ReceiptRequest.cbCustomer = $$"""{"eReceiptCustomerId": "{{eReceiptCustomerId}}"}""";
        return request;
    }

    public static ProcessRequest ZeroReceipt() => Wrap(new ReceiptRequest
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_2000,
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        Currency = Currency.PLN,
        cbChargeItems = [],
        cbPayItems = [],
    });

    private static ProcessRequest Wrap(ReceiptRequest request) => new()
    {
        ReceiptRequest = request,
        ReceiptResponse = new ReceiptResponse
        {
            ftCashBoxIdentification = "ACPT0001",
            ftQueueID = Guid.NewGuid(),
            ftReceiptIdentification = "ft1#",
        },
    };
}
