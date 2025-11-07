using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;

public record BusinessCase(string title, string description, bool supported, ReceiptRequest? receiptRequest, string? referencedCase = null) { }

public static class PTCertificationExamples
{
    public const string CUSOMTER_VATNUMBER = "199998132";

    public static ReceiptCase BaseCase = ((ReceiptCase) 0x2000_0000_0000).WithCountry("PT");

    public static DateTime ReferenceDate = new DateTime(2025, 09, 22, 04, 15, 53);

    public static PTUserObject User1ObjectId => new PTUserObject
    {
        UserId = Convert.ToBase64String(Guid.Parse("2e794dff-3123-4281-8810-4716d717cda5").ToByteArray()),
        UserDisplayName = "Stefan Kert",
        UserEmail = "stefan.kert@fiskaltrust.eu"
    };

    public static PTUserObject User2ObjectId => new PTUserObject
    {
        UserId = Convert.ToBase64String(Guid.Parse("ae289084-130c-4e37-806c-b6bf876c50b0").ToByteArray()),
        UserDisplayName = "Christina Kert",
        UserEmail = "christina.kert@fiskaltrust.eu"
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


    public static ReceiptRequest Case_5_1() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(1),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Description = "Line item 1",
                    Amount = 100,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) PTVATRates.NormalCase,
                }
            ],
        cbPayItems =
            [
                new PayItem
                {
                    Description = "Numerario",
                    Amount = 100,
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001
                }
            ],
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
        cbUser = User1ObjectId,
        cbCustomer = VAT_INCLUDED_CUSTOMER_1
    };

    public static ReceiptRequest Case_5_2() => new ReceiptRequest();

    public static ReceiptRequest Case_5_3() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(3),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Amount = 150m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase =  (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
        cbPayItems = [],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.Order0x3004)
    };

    public static ReceiptRequest Case_5_4() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(4),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Amount = 150m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
        cbPayItems =
            [
                new PayItem
                {
                    Amount = 150m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.InvoiceB2C0x1001),
        cbCustomer = VAT_INCLUDED_CUSTOMER_1
    };

    public static ReceiptRequest Case_5_5() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(5),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Amount = -100,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) PTVATRates.NormalCase,
                    Quantity = -1,
                    Description = "Line item 1"
                }
            ],
        cbPayItems =
            [
                new PayItem
                {
                    Quantity = -1,
                    Amount = -100,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.InvoiceB2C0x1001).WithFlag(ReceiptCaseFlags.Refund),
        cbCustomer = VAT_INCLUDED_CUSTOMER_1
    };

    public static ReceiptRequest Case_5_6() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(6),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = PTVATRates.Discounted1,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0011,
                    Quantity = 1,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 2,
                    Amount = 50,
                    VATRate = PTVATRates.NotTaxable,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_4018,
                    Quantity = 1,
                    Description = "Line item 2"
                },
                new ChargeItem
                {
                    Position = 3,
                    Amount = 25,
                    VATRate = PTVATRates.Discounted2,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0012,
                    Quantity = 1,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 4,
                    Amount = 12.5m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0023,
                    Quantity = 1,
                    Description = "Service Line item 1"
                }
            ],
        cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = 187.5m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.InvoiceB2C0x1001),
        cbCustomer = VAT_INCLUDED_CUSTOMER_1
    };

    public static ReceiptRequest Case_5_7() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(7),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100 * 0.55m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Quantity = 100,
                    Description = "Line item 1"
                },
                new ChargeItem
                {
                    Position = 1,
                    Amount = -(100 * 0.55m) * 0.088m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = ((ChargeItemCase) PTVATRates.NormalCase).WithFlag(ChargeItemCaseFlags.ExtraOrDiscount),
                    Quantity = 1,
                    Description = "Discount Line item 1"
                },
                new ChargeItem
                {
                    Position = 2,
                    Amount = 12.5m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Quantity = 1,
                    Description = "Line item 1"
                }
            ],
        cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = 62.66m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
    };

    public static ReceiptRequest Case_5_8() => new ReceiptRequest();

    public static ReceiptRequest Case_5_9() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(9),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Amount = 0.50m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
        cbPayItems =
            [
                new PayItem
                {
                    Amount = 0.50m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
        cbCustomer = NO_VAT_GIVEN_CUSTOMER_1
    };

    public static ReceiptRequest Case_5_10() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(10),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Amount = 150m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
        cbPayItems =
            [
                new PayItem
                {
                    Amount = 150m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
        cbCustomer = NO_VAT_GIVEN_CUSTOMER_2
    };

    public static ReceiptRequest Case_5_11() => new ReceiptRequest();

    public static ReceiptRequest Case_5_12() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(12),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems = [
                new ChargeItem
                {
                    Description = "Line item 1",
                    Amount = 150m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                }
            ],
        cbPayItems = [],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.Order0x3004),
    };

    public static ReceiptRequest Case_5_13() => new ReceiptRequest();

    public static ReceiptRequest Case_5_13_1_Invoice() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(13),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems =
            [
                new ChargeItem
                {
                    Amount = 150m,
                    VATRate = PTVATRates.Normal,
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Description = "Line item 1"
                }
            ],
        cbPayItems =
            [
                new PayItem
                {
                    Amount = 150m,
                    Description = "A crédito",
                    ftPayItemCase = ((PayItemCase) 0x5054_2000_0000_0000).WithCase(PayItemCase.AccountsReceivable),
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = ((ReceiptCase)0x2000_0000_0000).WithCountry("PT").WithCase(ReceiptCase.InvoiceB2C0x1001),
        cbCustomer = VAT_INCLUDED_CUSTOMER_1
    };

    public static ReceiptRequest Case_5_13_2_Payment() => new ReceiptRequest
    {
        cbReceiptMoment = ReferenceDate.AddMinutes(14),
        cbReceiptReference = Guid.NewGuid().ToString(),
        cbChargeItems = [],
        cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = 187.5m,
                    Description = "Numerario",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
        cbUser = User1ObjectId,
        ftReceiptCase = BaseCase.WithCase(ReceiptCase.PaymentTransfer0x0002)
    };
}