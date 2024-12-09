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
                    Description = "Μετρητά",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
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
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = 0x4752_2000_0000_0063,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Μετρητά",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
                CustomerZip = "12345"
            }
        };
    }

    public static ReceiptRequest A1_1_1p5_1()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 200m,
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
                    Description = "Μετρητά",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
            }
        };
    }

    public static ReceiptRequest A1_1_1p5_2()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 220m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 120,
                    VATRate = 24,
                    VATAmount = decimal.Round(120 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
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
                    Amount = 220m,
                    Description = "Μετρητά",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
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
                    Description = "Μετρητά",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
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
                    Description = "Μετρητά",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
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
                    Description = "Μετρητά",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
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
                    Description = "Μετρητά",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1004,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
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
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = 0x4752_2000_0000_6023,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Quantity = 1,
                    Description = "Πίστωση",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2100_0000_1004,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
            }
        };
    }
}
