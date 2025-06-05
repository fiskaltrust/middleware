using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

public static class Examples
{
    public static ReceiptRequest A1_WithDiscountCase()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 100m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = 24,
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                    Quantity = 1,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 2,
                    Amount = -2.5m,
                    VATAmount = -0.4838709677419355m,
                    VATRate = 24,
                    ftChargeItemCase = (ChargeItemCase) 0x4752200000040003,
                    Quantity = 1,
                    Description = "Espresso",
                    Moment = DateTime.Parse("2025-04-14T08:17:08.536Z"),
                },

            ],
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = 97.5m,
                    Description = "Κάρτα",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0004,
                },
                new PayItem
                {
                    Description = "Φιλοδώρημα",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0040_0004,
                    Amount = 0
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_1002,
            cbCustomer = new
            {
                customerVATId = "GR026883248",
                customerName = "Πελάτης A.E.",
                customerStreet = "Κηφισίας 12, 12345, Αθήνα",
                customerCity = "Αθηνών",
                customerCountry = "GR",
            }
        };
    }

    public static ReceiptRequest A2_ReceiptRefund()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = -99,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = -99,
                    VATRate = -24,
                    VATAmount = -decimal.Round(99 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0013).WithFlag(ChargeItemCaseFlags.Refund),
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = -99,
                    Description = "Μετρητά",
                    ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_1001).WithFlag(PayItemCaseFlags.Refund),
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0001).WithFlag(ReceiptCaseFlags.Refund),
        };
    }
}
