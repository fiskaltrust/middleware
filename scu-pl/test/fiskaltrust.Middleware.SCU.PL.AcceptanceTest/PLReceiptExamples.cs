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

    /// <summary>
    /// Both discount levels on one receipt: 2.00 off the position (the rabat travels on the sale
    /// line) and 1.00 off the subtotal, which the register distributes over the PTU rates itself.
    /// The subtotal discount stands before the position it cannot belong to — that is what marks it
    /// as one — and reaches the device after every line.
    /// </summary>
    public static ProcessRequest DiscountSale() => Wrap(new ReceiptRequest
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        Currency = Currency.PLN,
        cbChargeItems =
        [
            new ChargeItem { Description = "Stały klient", Amount = -1.00m, Quantity = 1m, VATRate = 8m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0004_0011, Currency = Currency.PLN },
            new ChargeItem { Description = "Candies", Amount = 10.00m, Quantity = 1m, VATRate = 8m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0011, Currency = Currency.PLN },
            new ChargeItem { Description = "Rabat na Candies", Amount = -2.00m, Quantity = 1m, VATRate = 8m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0004_0011, Currency = Currency.PLN },
        ],
        cbPayItems =
        [
            new PayItem { Description = "Gotówka", Amount = 7.00m, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001, Currency = Currency.PLN },
        ],
    });

    /// <summary>
    /// The other direction of the same two levels: a narzut on the position and one on the subtotal.
    /// It travels in the same fields as a rabat, with rd0 instead of rd1, and adds to the total the
    /// receipt is settled with — 10.00 plus 2.00 on the line plus 1.00 on the subtotal.
    /// </summary>
    public static ProcessRequest MarkupSale() => Wrap(new ReceiptRequest
    {
        ftReceiptCase = (ReceiptCase)0x504C_2000_0000_0001,
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        Currency = Currency.PLN,
        cbChargeItems =
        [
            new ChargeItem { Description = "Opłata serwisowa", Amount = 1.00m, Quantity = 1m, VATRate = 8m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0004_0011, Currency = Currency.PLN },
            new ChargeItem { Description = "Candies", Amount = 10.00m, Quantity = 1m, VATRate = 8m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0011, Currency = Currency.PLN },
            new ChargeItem { Description = "Dopłata za pakowanie", Amount = 2.00m, Quantity = 1m, VATRate = 8m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0004_0011, Currency = Currency.PLN },
        ],
        cbPayItems =
        [
            new PayItem { Description = "Gotówka", Amount = 13.00m, ftPayItemCase = (PayItemCase)0x504C_2000_0000_0001, Currency = Currency.PLN },
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
