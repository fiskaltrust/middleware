using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.SAFT.CLI;

public static class AADECertificationExamples
{
    public const string CUSOMTER_VATNUMBER = "026883248";
    //public const string CUSOMTER_VATNUMBER = "997671770";

    public static ReceiptRequest A1_1_1p2()
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
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_6017,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4154_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "ATU68541544",
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest A1_1_1p3()
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
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_6017,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x0000_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "GB300325371",
                CustomerCountry = "GB",
                CustomerCity = "Milton Keynes, Buckinghamshire",
                CustomerZip = "MK9 2AH",
                CustomerStreet = "Part Second Floor West Wing, Ashton House Silbury Boulevard, United Kingdom",
                CustomerName = "VIVA WALLET.COM LTD"
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
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0093,
                    Quantity = 1,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 2,
                    Amount = 100,
                    VATRate = 24,
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_1001,
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
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0093,
                    Quantity = 1,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 2,
                    Amount = 100,
                    VATRate = 24,
                    VATAmount = decimal.Round(100 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_1001,
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

    public static ReceiptRequest A1_2_2p2()
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
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_6027,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4154_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "ATU68541544",
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest A1_2_2p3()
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
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_6027,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x0000_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "GB300325371",
                CustomerCountry = "GB",
                CustomerCity = "Milton Keynes, Buckinghamshire",
                CustomerZip = "MK9 2AH",
                CustomerStreet = "Part Second Floor West Wing, Ashton House Silbury Boulevard, United Kingdom",
                CustomerName = "VIVA WALLET.COM LTD"
            }
        };
    }

    public static ReceiptRequest A1_3_3p1()
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
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0028,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0003,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerZip = "12345",
                CustomerCountry = "GR",
            }
        };
    }

    public static ReceiptRequest A1_3_3p2()
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
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0028,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0003,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerCountry = "GR",
                CustomerZip = "12345"
            },
            ftReceiptCaseData = "3.2"
        };
    }

    public static ReceiptRequest A1_6_6p1()
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
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Μετρητά",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_3003,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerZip = "12345",
                CustomerCountry = "GR",
            }
        };
    }

    public static ReceiptRequest A1_6_6p2()
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
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0023,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_3003,
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

    public static ReceiptRequest A1_7_7p1()
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
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0023,
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
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_3006,
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

    public static ReceiptRequest A1_8_8p1()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 4m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 4,
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0018,
                    Quantity = 1,
                    Description = "Renting something"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 4,
                    Quantity = 1,
                    Description = "Μετρητά",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_3005, // Rent not defined yet
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

    public static ReceiptRequest A1_8_8p2()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 4m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 4,
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_1168, // Nature for the Climate Resilience Tax 
                    Quantity = 1,
                    Description = "Τέλος ανθεκτικότητας κλιματικής κρίσης"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 4,
                    Quantity = 1,
                    Description = "Μετρητά",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerName = "Πελάτης A.E.",
                CustomerStreet = "Κηφισίας 12, 12345, Αθήνα",
                CustomerCity = "Αθηνών",
                CustomerZip = "12345",
                CustomerCountry = "GR",
            }
        };
    }

    public static ReceiptRequest A2_11_11p3()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 99,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 99,
                    VATRate = 24,
                    VATAmount = decimal.Round(99 / (100M + 24) * 24, 2, MidpointRounding.ToEven),
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 99,
                    Description = "Μετρητά",
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_1001,
                }
            ],

            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001
        };
    }
}
