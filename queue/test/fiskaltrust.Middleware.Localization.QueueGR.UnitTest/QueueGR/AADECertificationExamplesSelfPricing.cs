using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.SAFT.CLI;

public static class AADECertificationExamplesSelfPricing
{
    public const string CUSOMTER_VATNUMBER = "026883248";
    //public const string CUSOMTER_VATNUMBER = "997671770";

    public static ReceiptRequest A1_1_1p1()
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
                    ftChargeItemCase = 0x4752_2000_0000_0013,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
            }
        };
    }

    public static ReceiptRequest A1_1_1p4()
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
                    VATAmount = 0,
                    ftChargeItemCase = 0x4752_2000_0000_0067,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4154_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "ATU68541544",
                CustomerCountry = "AT",
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest A1_1_1p5()
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
                    ftChargeItemCase = 0x4752_2000_0000_0093,
                    Quantity = 1,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 2,
                    Amount = 100,
                    VATRate = 24,
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = 0x4752_2000_0000_0013,
                    Quantity = 1,
                    Description = "Line item 1"
                }

            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 200m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
            }
        };
    }

    public static ReceiptRequest A1_1_1p6()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 100m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbPreviousReceiptReference = "400001941223252",
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = 24,
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = 0x4752_2000_0000_0013,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
            }
        };
    }

    public static ReceiptRequest A1_2_2p1()
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
                    ftChargeItemCase = 0x4752_2000_0000_0023,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
            }
        };
    }

    public static ReceiptRequest A1_2_2p4()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 100m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbPreviousReceiptReference = "400001941223255", // need to replace this with lookup
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = 24,
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = 0x4752_2000_0000_0023,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
            }
        };
    }

    public static ReceiptRequest A1_5_5p1()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 100m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbPreviousReceiptReference = "400001941221523",
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = 24,
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = 0x4752_2000_0000_0013,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1004,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
            }
        };
    }

    public static ReceiptRequest A1_5_5p2()
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
                    VATAmount = 0,
                    ftChargeItemCase = 0x4752_2000_0000_6027,
                    Quantity = -1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Quantity = 1,
                    Description = "Gutschrift",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1004,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
            }
        };
    }
}
