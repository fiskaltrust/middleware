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

    public static ReceiptRequest Case_T6()
    {
        return new ReceiptRequest
        {
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
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
            cbUser = 1,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "199998132"
            }
        };
    }

    public static ReceiptRequest Case_T7() => throw new NotImplementedException("This test case is not applicable for QueuePT as it involves a correction receipt which is not supported.");

    public static ReceiptRequest Case_T8()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = DateTime.UtcNow,
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
            cbUser = 1,
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_3004
        };
    }

    public static ReceiptRequest Case_T9() => throw new NotImplementedException("This test case is not applicable for QueuePT as it involves a correction receipt which is not supported.");

    public static ReceiptRequest Case_T10(string previousReceiptReference)
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = DateTime.UtcNow,
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
            cbUser = 1,
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_3004
        };
    }

    public static ReceiptRequest Case_T11(ReceiptRequest receiptToCancel)
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbPreviousReceiptReference = receiptToCancel.cbReceiptReference,
            cbChargeItems = [.. receiptToCancel.cbChargeItems.Select(x => new ChargeItem
            {
                Amount = -x.Amount,
                Quantity = -x.Quantity,
                Description = x.Description,
                ftChargeItemCase = x.ftChargeItemCase,
                VATRate = x.VATRate
            })],
            cbPayItems = [.. receiptToCancel.cbPayItems.Select(x => new PayItem
            {
                Amount = -x.Amount,
                Quantity = -x.Quantity,
                Description = x.Description,
                ftPayItemCase = x.ftPayItemCase
            })],
            cbUser = 1,
            ftReceiptCase = receiptToCancel.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund)
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
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_4018,
                Description = "Line item 5"
            },
        };
        return new ReceiptRequest
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
                Description = "Line item 1"
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
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001
        };
    }

    public static ReceiptRequest Case_T17() => throw new NotImplementedException("WHAT?");

    public static ReceiptRequest Case_T18() => throw new NotImplementedException("Foreign currency is not supported");

    public static ReceiptRequest Case_T19()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = DateTime.UtcNow,
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
            cbUser = 1,
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
            cbCustomer = new MiddlewareCustomer
            {
                CustomerName = "Christoph Kert"
            }
        };
    }

    public static ReceiptRequest Case_T20()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = DateTime.UtcNow,
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
            cbUser = 1,
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
            cbCustomer = new MiddlewareCustomer
            {
                CustomerName = "Jakob Kert"
            }
        };
    }

    public static ReceiptRequest Case_T21()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = [
                new ChargeItem
                {
                    Amount = 20m,
                    Quantity = 2000,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
            cbUser = 1,
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
            cbReceiptMoment =  new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 6, 50, 54, DateTimeKind.Utc),
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
            cbUser = 1,
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

    // Handwritten receipt
    public static ReceiptRequest Case_T28()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 6, 50, 54, DateTimeKind.Utc),
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
            cbUser = 1,
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

    public static ReceiptRequest Case_T29() => throw new NotImplementedException("Backup/restore is not supported");

    public static ReceiptRequest Case_T30() => throw new NotImplementedException("Including other systems is not supported");

    public static ReceiptRequest Case_T31()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 6, 50, 54, DateTimeKind.Utc),
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
            cbUser = 1,
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

    public static ReceiptRequest Case_T32() => throw new NotImplementedException("Withholding taxes are not supported.");

    public static ReceiptRequest Case_T33()
    {
        return new ReceiptRequest
        {
            cbReceiptMoment = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 6, 50, 54, DateTimeKind.Utc),
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
            cbUser = 1,
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
}