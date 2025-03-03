using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.SAFT.CLI;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Certification;

public static class PTVATRates
{
    public const decimal Discounted1 = 6;
    public const decimal Discounted2 = 13;
    public const decimal Normal = 23;
    public const decimal ParkingVatRate = 13;
    public const decimal NotTaxable = 0;
    public const decimal ZeroRate = 0;
}

public static class VATHelpers
{
    public static decimal CalculateVAT(decimal amount, decimal rate) => decimal.Round(amount / (100M + rate) * rate, 2, MidpointRounding.ToEven);
}

public static class PTCertificationExamples
{
    public const string CUSOMTER_VATNUMBER = "980833310";

    public static ReceiptRequest Case_5_1()
    {
        // Simplified invoice with customer identification with NIF (VAT number)
        var chargeItems = new List<ChargeItem>
        {
                new ChargeItem
                {
                    Position = 1,
                    Amount = 100,
                    VATRate = PTVATRates.Normal,
                    VATAmount = VATHelpers.CalculateVAT(100, PTVATRates.Normal),
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                    Quantity = 1,
                    Description = "Line item 1"
                }
        };

        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001,
            cbUser = 1,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest Case_5_2(string receiptReference)
    {
        // A voided document. Status “A”
        // Simplified invoice with customer identification with NIF (VAT number)
        throw new NotImplementedException();
    }

    public static ReceiptRequest Case_5_3()
    {
        // A proforma/table check.
        throw new NotImplementedException();
    }

    public static ReceiptRequest Case_5_4()
    {
        // An invoice based on document on point 5.3. (OrderReference)
        throw new NotImplementedException();
    }

    public static ReceiptRequest Case_5_5(string previousReceiptReference)
    {
        // A credit note based on the document 5.4. If you don’t have the number 5.4 you should do it based on a different document.
        var chargeItems = new List<ChargeItem>
        {
                new ChargeItem
                {
                    Position = 1,
                    Amount = -100,
                    VATRate = PTVATRates.Normal,
                    VATAmount = -VATHelpers.CalculateVAT(100, PTVATRates.Normal),
                    ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0002_0013,
                    Quantity = -1,
                    Description = "Line item 1"
                }
        };

        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0002_0001,
                }
            ],
            cbPreviousReceiptReference = previousReceiptReference,
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0100_0001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = CUSOMTER_VATNUMBER,
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest Case_5_6()
    {
        // Simplified invoice with customer identification with NIF (VAT number)
        // Line 1 – Article with reduced VAT 6 %
        // Line 2 – Article with 0 % VAT(With exempt reason)
        // Line 3 – Article with 13 % VAT
        // Line 4 – Article with article 23 % VAT
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Position = 1,
                Amount = 100,
                VATRate = PTVATRates.Discounted1,
                VATAmount = VATHelpers.CalculateVAT(100, PTVATRates.Discounted1),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0011,
                Quantity = 1,
                Description = "Line item 1 (reduced 6%)"
            },
            new ChargeItem
            {
                Position = 2,
                Amount = 50,
                VATRate = PTVATRates.NotTaxable,
                VATAmount = VATHelpers.CalculateVAT(50, PTVATRates.NotTaxable),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_3018,
                Quantity = 1,
                Description = "Line item 2 (exempt)"
            },
            new ChargeItem
            {
                Position = 3,
                Amount = 25,
                VATRate = PTVATRates.Discounted2,
                VATAmount = VATHelpers.CalculateVAT(25, PTVATRates.Discounted2),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0011,
                Quantity = 1,
                Description = "Line item 1 (13%)"
            },
            new ChargeItem
            {
                Position = 4,
                Amount = 12.5m,
                VATRate = PTVATRates.Normal,
                VATAmount = VATHelpers.CalculateVAT(12.5m, PTVATRates.Normal),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                Quantity = 1,
                Description = "Line item 1 (Normal)"
            }
        };
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001
        };
    }

    public static ReceiptRequest Case_5_7()
    {
        // A document (invoice or simplified invoice) with 2 lines;
        // Line 1 – Article with quantity 100 at unit price 0,55 with a line discount of 8.8%.
        // Line 2 – Another article.
        // On the total you need to give a global document discount. Any discount. ?????
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Position = 1,
                Amount = 100 * 0.55m,
                VATRate = PTVATRates.Normal,
                VATAmount = VATHelpers.CalculateVAT(100, PTVATRates.Normal),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                Quantity = 100,
                Description = "Line item 1 (reduced 6%)"
            },
            new ChargeItem
            {
                Position = 2,
                Amount = -(100 * 0.55m) * 0.088m,
                VATRate = PTVATRates.Normal,
                VATAmount = VATHelpers.CalculateVAT(-(100 * 0.55m) * 0.088m, PTVATRates.Normal),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0004_0013,
                Quantity = 1,
                Description = "Discount Line item 1 (reduced 6%)"
            },
            new ChargeItem
            {
                Position = 3,
                Amount = 12.5m,
                VATRate = PTVATRates.Normal,
                VATAmount = VATHelpers.CalculateVAT(100, PTVATRates.Normal),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                Quantity = 1,
                Description = "Line item 1 (Normal)"
            }
        };
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001
        };
    }

    public static ReceiptRequest Case_5_8()
    {
        // A document in a foreign currency.
        throw new Exception("Not implemented");
    }

    public static ReceiptRequest Case_5_9()
    {
        // A document to an identified customer without NIF (VAT Number) with total less than 1,00€ and registered before 10AM.
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Position = 1,
                Amount = 0.50m,
                VATRate = PTVATRates.Normal,
                VATAmount = VATHelpers.CalculateVAT(0.50m, PTVATRates.Normal),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                Quantity = 1,
                Description = "Line item 1"
            }
        };

        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = new DateTime(2025, 03, 03, 06, 00, 00, DateTimeKind.Utc),
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest Case_5_10()
    {
        // A document for another identified client who has also not indicated their VAT number
        var chargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Position = 1,
                Amount = 150m,
                VATRate = PTVATRates.Normal,
                VATAmount = VATHelpers.CalculateVAT(150m, PTVATRates.Normal),
                ftChargeItemCase = (ChargeItemCase) 0x5054_2000_0000_0013,
                Quantity = 1,
                Description = "Line item 1"
            }
        };

        return new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptAmount = chargeItems.Sum(x => x.Amount),
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbChargeItems = chargeItems,
            cbPayItems =
            [
                new PayItem
                {
                    Position = 1,
                    Quantity = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Cash",
                    ftPayItemCase = (PayItemCase) 0x5054_2000_0000_0001,
                }
            ],
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) 0x5054_2000_0000_0001,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerCity = "Salzburg",
                CustomerZip = "5020",
                CustomerStreet = "Alpenstraße 99/2.OG/02",
                CustomerName = "fiskaltrust consulting gmbh"
            }
        };
    }

    public static ReceiptRequest Case_5_11()
    {
        // 2 Deliver notes. One with value and other without value.
        throw new Exception("Not implemented");
    }

    public static ReceiptRequest Case_5_12()
    {
        // A proforma or proposal.
        throw new Exception("Not implemented");
    }

    public static ReceiptRequest Case_5_13()
    {
        // An example of all other type of documents emitted by the software not described above.
        // Notes
        // 1 – The UnitPrice should have the correct rounding’s calculated from the line and global discounts and should have the enough decimals to minimize the differences in the taxes. (4 to 6 decimals)
        // 2 – To each of the documents requested should be identified the request number. If you don’t emit the document should be “Não aplicável”
        throw new Exception("Not implemented");
    }
}