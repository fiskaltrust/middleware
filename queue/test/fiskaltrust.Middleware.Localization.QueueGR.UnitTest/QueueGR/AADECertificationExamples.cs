using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.SAFT.CLI;

public static class AADECertificationExamples
{
    public static ReceiptRequest A1_1_1p1(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",
            }
        };
    }

    public static ReceiptRequest A1_1_1p2(Guid cashBoxId)
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
                    ftChargeItemCase = 0x4752_2000_0000_6017,
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4154_2000_0000_1001,
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

    public static ReceiptRequest A1_1_1p3(Guid cashBoxId)
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
                    ftChargeItemCase = 0x4752_2000_0000_6017,
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x5553_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "ATU68541544",
                CustomerCountry = "US",
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest A1_1_1p4(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4154_2000_0000_1001,
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

    public static ReceiptRequest A1_1_1p5(Guid cashBoxId)
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
                    Amount = 100m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",
            }
        };
    }

    public static ReceiptRequest A1_1_1p6(Guid cashBoxId)
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 100m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbPreviousReceiptReference = "400001941216788",
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",
            }
        };
    }

    public static ReceiptRequest A1_2_2p1(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",
            }
        };
    }

    public static ReceiptRequest A1_2_2p2(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4154_2000_0000_1001,
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

    public static ReceiptRequest A1_2_2p3(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x5553_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "ATU68541544",
                CustomerCountry = "US",
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest A1_2_2p4(Guid cashBoxId)
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptAmount = 100m,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbPreviousReceiptReference = "400001941216780", // need to replace this with lookup
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",
            }
        };
    }

    public static ReceiptRequest A1_5_5p1(Guid cashBoxId)
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
                    Amount = -100,
                    VATRate = 24,
                    VATAmount = 0,
                    ftChargeItemCase = 0x4752_2000_0002_6027,
                    Quantity = -1,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = -100m,
                    Quantity = -1,
                    Description = "Gutschrift",
                    ftPayItemCase = 0x4752_2000_0002_0001
                }
            ],
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x5553_2000_0100_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "ATU68541544",
                CustomerCountry = "US",
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest A1_6_6p1(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_3003,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",
            }
        };
    }

    public static ReceiptRequest A1_6_6p2(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_3003,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "997671770",
            }
        };
    }
 
    public static ReceiptRequest A1_7_7p1(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_3006,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerCountry = "AT",
                CustomerVATId = "ATU68541544",
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
            }
        };
    }

    public static ReceiptRequest A1_8_8p1(Guid cashBoxId)
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
                    Amount = 100,
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = 0x4752_2000_0000_0018,
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
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_3005, // Rent not defined yet
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

    public static ReceiptRequest A1_8_8p2(Guid cashBoxId)
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
                    ftChargeItemCase = 0x4752_2000_0000_1168, // Nature for the Climate Resilience Tax 
                    Quantity = 1,
                    Description = "Climate Resilience Tax"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 4,
                    Quantity = 1,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_0001
        };
    }
   
    public static ReceiptRequest A1_8_8p4(Guid cashBoxId)
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
                    Amount = 100,
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = 0x4752_2000_0000_0018,
                    Quantity = 1,
                    Description = "Something"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100,
                    Quantity = 1,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            ],
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_3004
        };
    }

    public static ReceiptRequest A1_8_8p5(Guid cashBoxId)
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
                    Amount = -100,
                    VATRate = 0,
                    VATAmount = 0,
                    ftChargeItemCase = 0x4752_2000_0002_0018,
                    Quantity = 1,
                    Description = "Something"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = -100,
                    Quantity = 1,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0002_0001
                }
            ],
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0100_3004
        };
    }

    public static ReceiptRequest A2_11_11p1(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_0001
        };
    }

    public static ReceiptRequest A2_11_11p2(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_0001
        };
    }

    public static ReceiptRequest A2_11_11p3(Guid cashBoxId)
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
                    Amount = 99,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_1001
                }
            ],
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4752_2000_0000_0001
        };
    }

    public static ReceiptRequest A2_11_1p5(Guid cashBoxId)
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
            ftCashBoxID = cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = 0x4555_2000_0000_0001
        };
    }

}
