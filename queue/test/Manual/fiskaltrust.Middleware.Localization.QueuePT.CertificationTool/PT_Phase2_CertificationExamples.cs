using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.SAFT.CLI;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;

public static class PT_Phase2_CertificationExamples
{
    public const string CUSOMTER_VATNUMBER = "199998132";

    public static PTUserObject User1ObjectId => new PTUserObject
    {
        UserId = Guid.Parse("2e794dff-3123-4281-8810-4716d717cda5").ToString(),
        UserDisplayName = "Stefan Kert",
        UserEmail = "stefan.kert@fiskaltrust.eu"
    };

    public static PTUserObject User2ObjectId => new PTUserObject
    {
        UserId = Guid.Parse("ae289084-130c-4e37-806c-b6bf876c50b0").ToString(),
        UserDisplayName = "Christina Kert",
        UserEmail = "christina.kert@fiskaltrust.eu"
    };

    public static MiddlewareCustomer VAT_ONLY_CUSTOMER => new MiddlewareCustomer
    {
        CustomerVATId = CUSOMTER_VATNUMBER
    };

    public static MiddlewareCustomer VAT_INCLUDED_CUSTOMER_1 => new MiddlewareCustomer
    {
        CustomerVATId = CUSOMTER_VATNUMBER,
        CustomerCity = "Lissbon",
        CustomerZip = "1050-189",
        CustomerStreet = "Demo street",
        CustomerName = "Nuno Cazeiro"
    };

    public static MiddlewareCustomer NO_VAT_GIVEN_CUSTOMER_1 => new MiddlewareCustomer
    {
        CustomerCity = "Lissbon",
        CustomerZip = "1050-189",
        CustomerStreet = "Demo street",
        CustomerName = "Nuno Cazeiro"
    };

    public static MiddlewareCustomer NO_VAT_GIVEN_CUSTOMER_2 => new MiddlewareCustomer
    {
        CustomerCity = "Lissbon",
        CustomerZip = "1050-190",
        CustomerStreet = "Demo street",
        CustomerName = "Stefan Kert"
    };

    public static MiddlewareCustomer ONLY_CUSTOMER_NAME_GIVEN_1 => new MiddlewareCustomer
    {
        CustomerName = "Jakob Kert"
    };

    public static MiddlewareCustomer ONLY_CUSTOMER_NAME_GIVEN_2 => new MiddlewareCustomer
    {
        CustomerName = "Christoph Kert"
    };

    public static DateTime ReferenceDate = new DateTime(2025, 09, 16, 04, 15, 53);

    public static ReceiptRequest Case_T6()
    {
        return new ReceiptRequest
        {
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = ReferenceDate.AddMinutes(6),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 100,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) PTVATRates.NormalCase,
                    Description = "Line item 1"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 100,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001,
            cbUser = User1ObjectId,
            cbCustomer = VAT_ONLY_CUSTOMER
        };
    }

    public static ReceiptRequest Case_T7() => throw new NotImplementedException("This test case is not applicable for QueuePT as it involves a correction receipt which is not supported.");

    public static ReceiptRequest Case_T8()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(8),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 100m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase =  (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbPayItems = [],
            cbUser = User1ObjectId,
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_3004
        };
    }

    public static ReceiptRequest Case_T9() => throw new NotImplementedException("This test case is not applicable for QueuePT as it involves a correction receipt which is not supported.");

    public static ReceiptRequest Case_T10()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(10),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 100m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase =  (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbPayItems = [
                new PayItem
                {
                    Amount = 100m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001
                }],
            cbUser = User2ObjectId,
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0001)
        };
    }

    public static ReceiptRequest Case_T11()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(11),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = -100m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1",
                    Quantity = -1
                }
            ],
            cbPayItems = [
                new PayItem
                {
                    Amount = -100m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                    Quantity = -1
                }
            ],
            cbUser = User1ObjectId,
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0001).WithFlag(ReceiptCaseFlags.Refund)
        };
    }

    public static ReceiptRequest Case_T12() => throw new NotImplementedException("WHAT?");

    public static ReceiptRequest Case_T13()
    {
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 12,
                VATRate = PTVATRates.Discounted1,
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0011,
                Description = "Line item 1"
            },
            new ChargeItem
            {
                Amount = 10,
                VATRate = PTVATRates.NotTaxable,
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_4018,
                Description = "Line item 2"
            },
            new ChargeItem
            {
                Amount = 10,
                VATRate = PTVATRates.Discounted2,
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0012,
                Description = "Line item 3"
            },
            new ChargeItem
            {
                Amount = 10,
                VATRate = PTVATRates.Normal,
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0023,
                Description = "Line item 4"
            },
            new ChargeItem
            {
                Amount = 10,
                VATRate = PTVATRates.NotTaxable,
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_3018,
                Description = "Line item 5"
            },
        };
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = ReferenceDate.AddMinutes(13),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_1001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerCity = "Lissbon",
                CustomerZip = "1050-189",
                CustomerStreet = "Demo street",
                CustomerName = "Nuno Cazeiro"
            }
        };
    }

    public static ReceiptRequest Case_T14() => throw new NotImplementedException("WHAT?");

    public static ReceiptRequest Case_T15() => throw new NotImplementedException("WHAT?");

    public static ReceiptRequest Case_T16()
    {
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 55.00m,
                Quantity = 100,
                VATRate = PTVATRates.Normal,
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                Description = "Line item 1"
            },
            new ChargeItem
            {
                Amount = -4.84m,
                Quantity = 1,
                VATRate = PTVATRates.Normal,
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0004_0013,
                Description = "Desconto"
            },
            new ChargeItem
            {
                Amount = 13.8m,
                Quantity = 4,
                VATRate = PTVATRates.Normal,
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                Description = "Line item 1"
            },
        };
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = ReferenceDate.AddMinutes(16),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "A crédito",
                    ftPayItemCase = ((PayItemCase) 0x5054_2000_0000_0000).WithCase(PayItemCase.AccountsReceivable),
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2C0x1001),
        };
    }

    public static ReceiptRequest Case_T17() => new ReceiptRequest
    {
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems = [],
        cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = 63.96m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0002
    };

    public static ReceiptRequest Case_T18() => throw new NotImplementedException("Foreign currency is not supported");

    public static ReceiptRequest Case_T19()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(19),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 0.90m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 0.90m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001,
            cbCustomer = ONLY_CUSTOMER_NAME_GIVEN_1
        };
    }

    public static ReceiptRequest Case_T20()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(20),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 0.90m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 0.90m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001,
            cbCustomer = ONLY_CUSTOMER_NAME_GIVEN_2
        };
    }

    public static ReceiptRequest Case_T21()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(21),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 20m,
                    Quantity = 2000,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Low price product"
                }
            ],
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 20m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001
        };
    }

    public static ReceiptRequest Case_T22()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(22),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 20m,
                    Quantity = 1,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 20m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001
        };
    }

    public static ReceiptRequest Case_T23() => throw new NotImplementedException("Multi page invoices are not supported");

    public static ReceiptRequest Case_T24() => throw new NotImplementedException("Credit memos are not supported");

    public static ReceiptRequest Case_T25() => throw new NotImplementedException("Serial numbers are not supported");

    public static ReceiptRequest Case_T26() => throw new NotImplementedException("e-DA invoices are not supported");

    public static ReceiptRequest Case_T27() => throw new NotImplementedException("Special income taxes are not supported");

    // Handwritten receipt - Manual document integration per Dispatch 8632/2014 point 2.4
    public static ReceiptRequest Case_T28()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(28),
            cbReceiptReference = "F/23", // Manual document reference
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 50m,
                    Quantity = 1,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Manual document - Product item"
                }
            ],
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 50m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0001).WithFlag(ReceiptCaseFlags.HandWritten)
        };
    }

    // Second manual document for T28 - series D #3 from 12-01-2022
    public static ReceiptRequest Case_T28_Second()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(29), // Document date for series D #3
            cbReceiptReference = "D/3", // Manual document reference
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 75m,
                    Quantity = 2,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0023,
                    Description = "Manual document D/3 - Service item"
                }
            ],
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 75m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0001).WithFlag(ReceiptCaseFlags.HandWritten)
        };
    }

    public static ReceiptRequest Case_T29() => throw new NotImplementedException("Backup/restore is not supported");

    public static ReceiptRequest Case_T30() => throw new NotImplementedException("Including other systems is not supported");

    public static ReceiptRequest Case_T31() => throw new NotImplementedException("Not applicable, duplicate of another item");

    public static ReceiptRequest Case_T32() => throw new NotImplementedException("Withholding taxes are not supported.");

    public static ReceiptRequest Case_T33()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(33),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 20m,
                    Quantity = 1,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbUser = User1ObjectId,
            cbPayItems =
            [
                new PayItem
                {
                    Amount = 20m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0001).WithFlag(ReceiptCaseFlags.HandWritten)
        };
    }

    public static ReceiptRequest Case_T33_CM()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(34),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 100m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase =  (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbPayItems = [],
            cbUser = User1ObjectId,
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0000).WithCase(ReceiptCase.Order0x3004) | (ReceiptCase) 0x5054_2001_0000_0000,
            ftReceiptCaseData = new
            {
                OrderType = "CM"
            }
        };
    }

    public static ReceiptRequest Case_T33_OR()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = ReferenceDate.AddMinutes(35),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 100m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase =  (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbPayItems = [],
            cbUser = User1ObjectId,
            ftReceiptCase = ((ReceiptCase) 0x5054_2000_0000_0000).WithCase(ReceiptCase.Order0x3004) | (ReceiptCase) 0x5054_2002_0000_0000,
            ftReceiptCaseData = new
            {
                OrderType = "OR"
            }
        };
    }
}