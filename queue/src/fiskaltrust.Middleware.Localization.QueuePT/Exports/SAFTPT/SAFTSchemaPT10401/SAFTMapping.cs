using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.HeaderContracts;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.MasterFileContracts;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401;

public static class SAFTMapping
{
    public static AuditFile CreateAuditFile(List<ftQueueItem> queueItems)
    {
        var receiptRequests = queueItems.Select(x => JsonSerializer.Deserialize<ReceiptRequest>(x.request)!).ToList();
        var invoices = receiptRequests.Select(GetInvoiceForReceiptRequest).ToList();
        return new AuditFile
        {
            Header = GetHeader(),
            MasterFiles = new MasterFiles
            {
                Customer = GetCustomers(receiptRequests),
                Product = GetProducts(receiptRequests),
                TaxTable = GetTaxTable(receiptRequests)
            },
            SourceDocuments = new SourceDocuments
            {
                SalesInvoices = new SalesInvoices
                {
                    NumberOfEntries = invoices.Count,
                    TotalDebit = invoices.SelectMany(x => x.Line).Sum(x => x.DebitAmount ?? 0.0m),
                    TotalCredit = invoices.SelectMany(x => x.Line).Sum(x => x.CreditAmount),
                    Invoice = invoices,
                }
            }
        };
    }

    private static List<Product> GetProducts(List<ReceiptRequest> receiptRequest)
    {
        return receiptRequest.SelectMany(x => x.cbChargeItems).Select(x => new Product
        {
            ProductType = "S",
            ProductCode = x.ProductNumber ?? "",
            ProductGroup = x.ProductGroup,
            ProductDescription = x.Description,
            ProductNumberCode = x.ProductNumber ?? ""
        }).DistinctBy(x => x.ProductCode).ToList();

        /*
         *  Product = [
                new Product
            {
                ProductType = "S",
                ProductCode = "SUPCERTIFIC",
                ProductGroup = "Sem fam lia",
                ProductDescription = "Suporte Certifica  o Software",
                ProductNumberCode = "SUPCERTIFIC"
            },
            new Product
            {
                ProductType = "S",
                ProductCode = "SRVCASAMO",
                ProductGroup = "Servi os",
                ProductDescription = "Support Casa Monthly fee",
                ProductNumberCode = "SRVCASAMO"
            }
            ],
         * */

    }

    private static TaxTable GetTaxTable(List<ReceiptRequest> receiptRequest)
    {
        var lines = receiptRequest.SelectMany(x => x.cbChargeItems).Select(GetLine);

        var taxTableEntries = lines.Select(x => new TaxTableEntry
        {
            TaxType = x.Tax.TaxType,
            TaxCountryRegion = x.Tax.TaxCountryRegion,
            TaxCode = x.Tax.TaxCode,
            Description = "",
            TaxPercentage = x.Tax.TaxPercentage
        }).DistinctBy(x => x.TaxCode).ToList();

        return new TaxTable
        {
            TaxTableEntry = taxTableEntries
        };

        /*
         * TaxTable = new TaxTable
            {
                TaxTableEntry = [
                    new TaxTableEntry
                {
                    TaxType = "IS",
                    TaxCountryRegion = "PT",
                    TaxCode = "1731",
                    Description = "Juros desc. letras e bil. tesouro",
                    TaxPercentage = 4.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IS",
                    TaxCountryRegion = "PT",
                    TaxCode = "1734",
                    Description = "Outras comis./contrapresta  es",
                    TaxPercentage = 4.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IS",
                    TaxCountryRegion = "PT-AC",
                    TaxCode = "1731",
                    Description = "Juros desc. letras e bil. tesouro",
                    TaxPercentage = 4.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IS",
                    TaxCountryRegion = "PT-AC",
                    TaxCode = "1734",
                    Description = "Outras comis./contrapresta  es",
                    TaxPercentage = 4.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IS",
                    TaxCountryRegion = "PT-MA",
                    TaxCode = "1731",
                    Description = "Juros desc. letras e bil. tesouro",
                    TaxPercentage = 4.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IS",
                    TaxCountryRegion = "PT-MA",
                    TaxCode = "1734",
                    Description = "Outras comis./contrapresta  es",
                    TaxPercentage = 4.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT",
                    TaxCode = "INT",
                    Description = "Taxa Interm dia",
                    TaxPercentage = 13.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT",
                    TaxCode = "ISE",
                    Description = "Isento",
                    TaxPercentage = 0.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT",
                    TaxCode = "NOR",
                    Description = "Taxa Normal",
                    TaxPercentage = 23.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT",
                    TaxCode = "RED",
                    Description = "Taxa Reduzida",
                    TaxPercentage = 6.000000m,
                },



                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT-AC",
                    TaxCode = "INT",
                    Description = "Taxa Interm dia",
                    TaxPercentage = 9.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT-AC",
                    TaxCode = "ISE",
                    Description = "Isento",
                    TaxPercentage = 0.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT-AC",
                    TaxCode = "NOR",
                    Description = "Taxa Normal",
                    TaxPercentage = 16.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT-AC",
                    TaxCode = "RED",
                    Description = "Taxa Reduzida",
                    TaxPercentage = 4.000000m,
                },

                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT-MA",
                    TaxCode = "INT",
                    Description = "Taxa Interm dia",
                    TaxPercentage = 12.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT-MA",
                    TaxCode = "ISE",
                    Description = "Isento",
                    TaxPercentage = 0.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT-MA",
                    TaxCode = "NOR",
                    Description = "Taxa Normal",
                    TaxPercentage = 22.000000m,
                },
                new TaxTableEntry
                {
                    TaxType = "IVA",
                    TaxCountryRegion = "PT-MA",
                    TaxCode = "RED",
                    Description = "Taxa Reduzida",
                    TaxPercentage = 5.000000m,
                },
            ],
            }
        */
    }

    private static List<Customer> GetCustomers(List<ReceiptRequest> receiptRequest)
    {
        return receiptRequest.Where(x => x.cbCustomer != null).Select(x =>
        {
            var customer = new Customer
            {
                CustomerID = "0047.ATU68541544",
                AccountID = "Desconhecido",
                CustomerTaxID = "ATU68541544",
                CompanyName = "fiskaltrust consulting gmbh",
                BillingAddress = new BillingAddress
                {
                    BuildingNumber = "Desconheci",
                    StreetName = "Alpenstra e",
                    AddressDetail = "fiskaltrust consulting gmbh  Alpenstra e 99  5020 Salzburg",
                    City = "Salzburg",
                    PostalCode = "5020",
                    Region = "Desconhecido",
                    Country = "AT",
                },
                Telephone = "Desconhecido", // not required
                Fax = "Desconhecido", // not required
                Email = "Desconhecido", // not required
                Website = "Desconhecido", // not required
                SelfBillingIndicator = 0, // not required
            };
            return customer;
        }).DistinctBy(x => x.CustomerID).ToList();

        //return [
        //        new Customer
        //        {
        //            CustomerID = "0047.ATU68541544",
        //            AccountID = "Desconhecido",
        //            CustomerTaxID = "ATU68541544",
        //            CompanyName = "fiskaltrust consulting gmbh",
        //            BillingAddress = new BillingAddress
        //            {
        //                BuildingNumber = "Desconheci",
        //                StreetName = "Alpenstra e",
        //                AddressDetail = "fiskaltrust consulting gmbh  Alpenstra e 99  5020 Salzburg",
        //                City = "Salzburg",
        //                PostalCode = "5020",
        //                Region = "Desconhecido",
        //                Country = "AT",
        //            },
        //            Telephone = "Desconhecido", // not required
        //            Fax = "Desconhecido", // not required
        //            Email = "Desconhecido", // not required
        //            Website = "Desconhecido", // not required
        //            SelfBillingIndicator = 0, // not required
        //        }
        //];
    }

    public static Header GetHeader()
    {
        return new Header
        {
            AuditFileVersion = "1.04_01",
            CompanyID = "TBD",
            TaxRegistrationNumber = 199998132,
            TaxAccountingBasis = TaxAccountingBasis.Invoicing,
            CompanyName = "fiskaltrust consulting gmbh",
            //BusinessName = null,
            CompanyAddress = new CompanyAddress
            {
                StreetName = "Alpenstra e",
                AddressDetail = "O seu endere o Alpenstra e 99  5020 Salzburg",
                City = "Salzburg",
                PostalCode = "5020",
                Region = "Desconhecido",
                Country = "AT",
            },
            FiscalYear = DateTime.UtcNow.Year,
            StartDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 01),
            EndDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month)),
            CurrencyCode = "EUR",
            DateCreated = new DateTime(2024, 06, 27),
            TaxEntity = "GLOBAL",
            ProductCompanyTaxID = 00000000,
            SoftwareCertificateNumber = 0000,
            ProductID = "fiskaltrust.Middleware",
            ProductVersion = "1.3",
        };
    }

    public static Invoice GetInvoiceForReceiptRequest(ReceiptRequest receiptRequest)
    {
        var lines = receiptRequest.cbChargeItems.Select(GetLine).ToList();
        var taxable = receiptRequest.cbChargeItems.Sum(x => x.VATAmount.GetValueOrDefault());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.Amount);
        var netAmount = grossAmount - taxable;
        var invoice = new Invoice
        {
            InvoiceNo = "TBD",
            ATCUD = "TBD",
            DocumentStatus = new DocumentStatus
            {
                InvoiceStatus = "N",
                InvoiceStatusDate = receiptRequest.cbReceiptMoment,
                SourceID = receiptRequest.ftCashBoxID?.ToString()!,
                SourceBilling = "P",
            },
            Hash = "TBD",
            HashControl = 1,
            Period = receiptRequest.cbReceiptMoment.Month,
            InvoiceDate = receiptRequest.cbReceiptMoment,
            InvoiceType = "FS",
            SpecialRegimes = new SpecialRegimes
            {
                SelfBillingIndicator = 0,
                CashVATSchemeIndicator = 0,
                ThirdPartiesBillingIndicator = 0,
            },
            SourceID = receiptRequest.ftCashBoxID?.ToString()!,
            SystemEntryDate = receiptRequest.cbReceiptMoment,
            CustomerID = "0",
            //CustomerID = "0047.ATU68541544",
            //ShipTo = new ShipTo
            //{
            //    Address = new Address
            //    {
            //        StreetName = "Alpenstra e",
            //        AddressDetail = "O seu endere o Alpenstra e 99  5020 Salzburg",
            //        City = "Salzburg",
            //        PostalCode = "5020",
            //        Region = "Desconhecido",
            //        Country = "AT",
            //    },
            //},
            //ShipFrom = new ShipFrom
            //{
            //    Address = new Address
            //    {
            //        StreetName = "R DO PORTO", // not required
            //        AddressDetail = "O Nosso Endere o R DO PORTO N33 7Dto  2775-543 Carcavelos",
            //        City = "Carcavelos",
            //        PostalCode = "2775-543",
            //        Region = "Desconhecido", // not required
            //        Country = "PT",
            //    },
            //},
            // MovementStartTime = new DateTime(2024, 06, 27, 11, 37, 18),
            Line = lines,
            DocumentTotals = new DocumentTotals
            {
                TaxPayable = Helpers.CreateTwoDigitMonetaryValue(taxable),
                NetTotal = Helpers.CreateTwoDigitMonetaryValue(netAmount),
                GrossTotal = Helpers.CreateTwoDigitMonetaryValue(grossAmount),
            }
        };

        invoice.DocumentTotals.Payment = receiptRequest.cbPayItems.Select(x => new Payment
        {
            PaymentAmount = x.Amount,
            PaymentDate = x.Moment,
            PaymentMechanism = GetPaymentMecahnism(x),
        }).ToList();
        return invoice;
    }

    public static Line GetLine(ChargeItem chargeItem)
    {
        var tax = new Tax
        {
            TaxType = "IVA", // one of IVA => vat; IS => stamp duty; NS => Not subject to VAT or Stamp Duty.
            TaxCountryRegion = "PT", // will depend on the location of the taxpayer.. autonomous regions madeira and azores
            TaxCode = GetIVATAxCode(chargeItem),
            TaxPercentage = Helpers.CreateMonetaryValue(chargeItem.VATRate)
        };
        return new Line
        {
            LineNumber = (long) chargeItem.Position,
            ProductCode = chargeItem.ProductNumber ?? "",
            ProductDescription = chargeItem.Description,
            Quantity = Helpers.CreateMonetaryValue(chargeItem.Quantity),
            UnitOfMeasure = chargeItem.Unit ?? "",
            UnitPrice = Helpers.CreateMonetaryValue(chargeItem.UnitPrice),
            TaxPointDate = chargeItem.Moment.GetValueOrDefault(), // need some more checks here.. fallback?
            Description = chargeItem.Description,
            CreditAmount = Helpers.CreateMonetaryValue(chargeItem.Amount - chargeItem.VATAmount),
            Tax = tax,
            //TaxExemptionReason = GetTaxExemptionReason(chargeItem),
            //TaxExemptionCode = GetTaxExemptionCode(chargeItem)
        };
    }

    // https://taxfoundation.org/data/all/eu/value-added-tax-2024-vat-rates-europe/
    public static string GetIVATAxCode(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF) switch
    {
        0x0 => throw new NotImplementedException("There is no unkown rate in Portugal"),
        0x1 => "RED",
        0x2 => throw new NotImplementedException("There is no reduced-2 rate in Portugal"),
        0x3 => "NOR",
        0x4 => throw new NotImplementedException("There is no super-reduced-1 rate in Portugal"),
        0x5 => throw new NotImplementedException("There is no super-reduced-2 rate in Portugal"),
        0x6 => "INT",
        0x7 => throw new NotImplementedException("There is no zero rate in Portugal"),
        0x8 => "ISE",
        _ => throw new NotImplementedException("The given tax scheme is not supported in Portugal"),
    };

    public static string GetTaxExemptionCode(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xFF00) switch
    {
        _ => "M16",
    };

    public static string GetTaxExemptionReason(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xFF00) switch
    {
        _ => "Isento Artigo 14.  do RITI (ou similar)",
    };


    /*
     * “CC” - Credit card;
“CD” - Debit card;
“CH” - Bank cheque;
“CI” – International Letter of Credit;
“CO” - Gift cheque or gift card;
“CS” - Balance compensation in current account;
    “DE” - Electronic Money, for example, on fidelity or points cards;
“LC” - Commercial Bill;
“MB” - Payment references for ATM;
“NU” – Cash;
“OU” – Other means not mentioned;
“PR” – Exchange of goods;
“TB” – Banking transfer or authorized direct debit;
“TR” - Non-wage compensation titles regardless of their support [paper or digital format], for instance, meal or education vouchers, etc.
    */
    public static string GetPaymentMecahnism(PayItem payItem) => (payItem.ftPayItemCase & 0xF) switch
    {
        0x0 => "OU", // Unknown – Other means not mentioned
        0x1 => "NU", // Cash
        0x2 => "OU", // Non Cash – Other means not mentioned
        0x3 => "CH", // Bank cheque
        0x4 => "CD", // Debit Card
        0x5 => "CC", // Credit Card
        0x6 => "CO", // Voucher Gift cheque or gift card;
        0x7 => "OU", // Online payment – Other means not mentioned
        0x8 => "OU", // Online payment – Other means not mentioned
        _ => "OU", // Other – Other means not mentioned
    };
}