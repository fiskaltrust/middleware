using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Storage.PT;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

public static class SAFTMapping
{
    public static AuditFile CreateAuditFile(AccountMasterData accountMasterData, List<ftQueueItem> queueItems)
    {
        var receiptRequests = queueItems.Select(x => (receiptRequest: JsonSerializer.Deserialize<ReceiptRequest>(x.request)!, receiptResponse: JsonSerializer.Deserialize<ReceiptResponse>(x.response))).ToList();
        var actualReceiptRequests = receiptRequests.Where(x => x.receiptResponse != null && ((long) x.receiptResponse.ftState & 0xFF) == 0x00).Cast<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>().ToList();

        var invoices = actualReceiptRequests.Select(x => SAFTMapping.GetInvoiceForReceiptRequest(accountMasterData, x)).Where(x => x != null).ToList();
        return new AuditFile
        {
            Header = GetHeader(accountMasterData),
            MasterFiles = new MasterFiles
            {
                Customer = GetCustomers(actualReceiptRequests.Select(x => x.receiptRequest).ToList()),
                Product = GetProducts(actualReceiptRequests.Select(x => x.receiptRequest).ToList()),
                TaxTable = GetTaxTable(actualReceiptRequests.Select(x => x.receiptRequest).ToList())
            },
            SourceDocuments = new SourceDocuments
            {
                SalesInvoices = new SalesInvoices
                {
                    NumberOfEntries = invoices.Count,
                    TotalDebit = invoices.SelectMany(x => x!.Line).Sum(x => x.DebitAmount ?? 0.0m),
                    TotalCredit = invoices.SelectMany(x => x!.Line).Sum(x => x.CreditAmount),
                    Invoice = invoices!,
                }
            }
        };
    }

    private static List<Product> GetProducts(List<ReceiptRequest> receiptRequest)
    {
        return receiptRequest.SelectMany(x => x.cbChargeItems).Select(x =>
        {
            return new Product
            {
                ProductType = GetProductType(x),
                ProductCode = x.ProductNumber ?? Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(x.Description))),
                ProductGroup = x.ProductGroup,
                ProductDescription = x.Description,
                ProductNumberCode = x.ProductNumber ?? Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(x.Description))),
            };
        }).DistinctBy(x => x.ProductCode).ToList();
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
        var customerData = receiptRequest.Where(x => x.cbCustomer != null).Select(x =>
        {
            var middlewareCustomer = GetCustomerIfIncludeded(x)!;
            var customer = new Customer
            {
                CustomerID = middlewareCustomer.CustomerId,
                AccountID = "Desconhecido",
                CustomerTaxID = middlewareCustomer.CustomerVATId,
                CompanyName = middlewareCustomer.CustomerName,
                BillingAddress = new BillingAddress
                {
                    BuildingNumber = "Desconheci",
                    StreetName = middlewareCustomer.CustomerStreet,
                    AddressDetail = $"{middlewareCustomer.CustomerName} {middlewareCustomer.CustomerStreet}  {middlewareCustomer.CustomerZip} {middlewareCustomer.CustomerCity}",
                    City = middlewareCustomer.CustomerCity,
                    PostalCode = middlewareCustomer.CustomerZip,
                    Region = "Desconhecido",
                    Country = middlewareCustomer.CustomerCountry,
                }
            };
            return customer;
        }).DistinctBy(x => x.CustomerID).ToList();

        if (receiptRequest.Any(x => x.cbCustomer == null))
        {
            customerData.Add(new Customer
            {
                CustomerID = "Consumidor final",
                AccountID = "Desconhecido",
                CustomerTaxID = "999999990",
                CompanyName = "Consumidor final",
                BillingAddress = new BillingAddress
                {
                    AddressDetail = "Desconhecido",
                    City = "Desconhecido",
                    PostalCode = "Desconhecido",
                    Country = "Desconhecido"
                }
            });
        }
        return customerData;
    }

    public static MiddlewareCustomer? GetCustomerIfIncludeded(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbCustomer == null)
        {
            return null;
        }
        return JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer));
    }

    public static Header GetHeader(AccountMasterData accountMasterData)
    {
        return new Header
        {
            AuditFileVersion = "1.04_01",
            CompanyID = accountMasterData.TaxId,
            TaxRegistrationNumber = int.Parse(accountMasterData.TaxId),
            TaxAccountingBasis = TaxAccountingBasis.Invoicing,
            CompanyName = accountMasterData.AccountName,
            //BusinessName = null,
            CompanyAddress = new CompanyAddress
            {
                StreetName = accountMasterData.Street,
                AddressDetail = $"{accountMasterData.AccountName} {accountMasterData.Street}  {accountMasterData.Zip} {accountMasterData.City}",
                City = accountMasterData.City,
                PostalCode = accountMasterData.Zip,
                Region = "Desconhecido",
                Country = accountMasterData.Country,
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

    public static Invoice? GetInvoiceForReceiptRequest(AccountMasterData accountMasterData, (ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var lines = receiptRequest.cbChargeItems.Select(GetLine).ToList();
        var taxable = receiptRequest.cbChargeItems.Sum(x => x.VATAmount.GetValueOrDefault());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.Amount);
        var hashSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType == (long) SignatureTypesPT.Hash).FirstOrDefault();
        var atcudSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType == (long) SignatureTypesPT.ATCUD).FirstOrDefault();
        var netAmount = grossAmount - taxable;
        var invoiceType = GetInvoiceType(receiptRequest);
        if (hashSignature == null || atcudSignature == null)
        {
            return null;
        }
        var invoice = new Invoice
        {
            InvoiceNo = receipt.receiptResponse.ftReceiptIdentification.Split("#")[1],
            ATCUD = atcudSignature.Data,
            DocumentStatus = new DocumentStatus
            {
                InvoiceStatus = "N",
                InvoiceStatusDate = receiptRequest.cbReceiptMoment,
                SourceID = JsonSerializer.Serialize(receiptRequest.cbUser),
                SourceBilling = "P",
            },
            Hash = hashSignature.Data,
            HashControl = 1,
            Period = receiptRequest.cbReceiptMoment.Month,
            InvoiceDate = receiptRequest.cbReceiptMoment,
            InvoiceType = GetInvoiceType(receiptRequest),
            SpecialRegimes = new SpecialRegimes
            {
                SelfBillingIndicator = 0,
                CashVATSchemeIndicator = 0,
                ThirdPartiesBillingIndicator = 0,
            },
            SourceID = JsonSerializer.Serialize(receiptRequest.cbUser),
            SystemEntryDate = receiptRequest.cbReceiptMoment,
            CustomerID = "0",
            Line = lines,
            DocumentTotals = new DocumentTotals
            {
                TaxPayable = Helpers.CreateTwoDigitMonetaryValue(taxable),
                NetTotal = Helpers.CreateTwoDigitMonetaryValue(netAmount),
                GrossTotal = Helpers.CreateTwoDigitMonetaryValue(grossAmount),
            }
        };
        var customer = GetCustomerIfIncludeded(receiptRequest);
        if (customer != null)
        {
            invoice.CustomerID = customer.CustomerId;
        }
        if (receiptRequest.cbChargeItems.Any(x => GetProductType(x) == "P"))
        {
            if (customer != null)
            {
                invoice.ShipTo = new ShipTo
                {
                    Address = new Address
                    {
                        StreetName = customer.CustomerStreet,
                        AddressDetail = $"{customer.CustomerName} {customer.CustomerStreet}  {customer.CustomerZip} {customer.CustomerCity}",
                        City = customer.CustomerCity,
                        PostalCode = customer.CustomerZip,
                        Region = "Desconhecido",
                        Country = customer.CustomerCountry,
                    },
                };
            }
            invoice.ShipFrom = new ShipFrom
            {
                Address = new Address
                {
                    StreetName = accountMasterData.Street,
                    AddressDetail = $"{accountMasterData.AccountName} {accountMasterData.Street}  {accountMasterData.Zip} {accountMasterData.City}",
                    City = accountMasterData.City,
                    PostalCode = accountMasterData.Zip,
                    Region = "Desconhecido",
                    Country = accountMasterData.Country,
                }
            };
        }

        invoice.DocumentTotals.Payment = receiptRequest.cbPayItems.Select(x => new Payment
        {
            PaymentAmount = x.Amount,
            PaymentDate = x.Moment,
            PaymentMechanism = GetPaymentMecahnism(x),
        }).ToList();
        return invoice;
    }

    private static string GetInvoiceType(ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0xFFFF) switch
    {
        0x0000 => "FS",
        0x0001 => "FS",
        0x0002 => "FS",
        0x0003 => "FS",
        0x0004 => "FS",
        0x0005 => "FS", // no invoicetype.. workign document?
        0x1000 => "FT",
        0x1001 => "FT",
        0x1002 => "FT",
        0x1003 => "FT",
        _ => throw new NotImplementedException($"The given receipt case {receiptRequest.ftReceiptCase} is not supported in Portugal"),
    };

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

    public static string GetPaymentMecahnism(PayItem payItem) => (payItem.ftPayItemCase & 0xF) switch
    {
        0x0 => "OU", // Unknown � Other means not mentioned
        0x1 => "NU", // Cash
        0x2 => "OU", // Non Cash � Other means not mentioned
        0x3 => "CH", // Bank cheque
        0x4 => "CD", // Debit Card
        0x5 => "CC", // Credit Card
        0x6 => "CO", // Voucher Gift cheque or gift card;
        0x7 => "OU", // Online payment � Other means not mentioned
        0x8 => "OU", // Online payment � Other means not mentioned
        _ => "OU", // Other � Other means not mentioned
    };

    public static string GetProductType(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF0) switch
    {
        0x00 => "O", // Unknown type of service / - Others (e.g. charged freights, advance payments received or sale of assets);
        0x10 => "P", // Delivery (supply of goods) / Products
        0x20 => "S", // Other service (supply of service) / Services
        0x30 => "S", // Tip / Services
        0x40 => "?", // Voucher / ???
        0x50 => "S", // Catalog Service / Services
        0x60 => "?", // Not own sales Agency busines / ???
        0x70 => "?", // Own Consumption / ???
        0x80 => "?", // Grant / ???
        0x90 => "?", // Receivable / ???
        0xA0 => "?", // Receivable / ???
        _ => throw new NotImplementedException($"The given ChargeItemCase {chargeItem.ftChargeItemCase} type is not supported"),
    };
}