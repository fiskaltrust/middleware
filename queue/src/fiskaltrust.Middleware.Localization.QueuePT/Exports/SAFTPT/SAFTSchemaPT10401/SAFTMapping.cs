using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using System.Xml;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments.PaymentDocumentModels;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

public static class CertificationPosSystem
{
    public const string ProductCompanyTaxID = "980833310";
    public const string SoftwareCertificateNumber = "9999";
    public const string ProductID = "fiskaltrust.CloudCashBox/FISKALTRUST CONSULTING GMBH - Sucursal em Portugal";
    public const string ProductVersion = "2.0";
}

public static class SAFTMapping
{
    private static readonly Customer _anonymousCustomer = new Customer
    {
        CustomerID = "0",
        AccountID = "Desconhecido",
        CustomerTaxID = "999999990",
        CompanyName = "Consumidor final",
        BillingAddress = new BillingAddress
        {
            AddressDetail = "Desconhecido",
            City = "Desconhecido",
            PostalCode = "Desconhecido",
            Country = "PT"
        }
    };

    public static Customer GetCustomerData(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbCustomer == null)
        {
            return _anonymousCustomer;
        }
        var middlewareCustomer = JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer))!;
        if (string.IsNullOrEmpty(middlewareCustomer.CustomerId))
        {
            if (string.IsNullOrEmpty(middlewareCustomer.CustomerVATId))
            {
                middlewareCustomer.CustomerId = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(middlewareCustomer.CustomerName)));
            }
            else
            {
                middlewareCustomer.CustomerId = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(middlewareCustomer.CustomerVATId)));
            }
        }
        var customer = new Customer
        {
            CustomerID = middlewareCustomer.CustomerId,
            AccountID = "Desconhecido",
            CustomerTaxID = middlewareCustomer.CustomerVATId ?? "999999990",
            CompanyName = middlewareCustomer.CustomerName,
            BillingAddress = new BillingAddress
            {
                StreetName = middlewareCustomer.CustomerStreet,
                AddressDetail = $"{middlewareCustomer.CustomerName} {middlewareCustomer.CustomerStreet}  {middlewareCustomer.CustomerZip} {middlewareCustomer.CustomerCity}",
                City = middlewareCustomer.CustomerCity,
                PostalCode = middlewareCustomer.CustomerZip,
                Country = middlewareCustomer.CustomerCountry ?? "PT",
            }
        };
        return customer;
    }

    public static string SerializeAuditFile(AccountMasterData accountMasterData, List<ftQueueItem> queueItems, int to)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var data = SAFTMapping.CreateAuditFile(accountMasterData, queueItems, to);
        using var memoryStream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.GetEncoding("windows-1252"),
            Indent = true
        };
        var serializer = new XmlSerializer(typeof(AuditFile));
        using var writer = XmlWriter.Create(memoryStream, settings);
        serializer.Serialize(writer, data);
        memoryStream.Position = 0;
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    public static AuditFile CreateAuditFile(AccountMasterData accountMasterData, List<ftQueueItem> queueItems, int to)
    {
        var receiptRequests = queueItems.Select(x => (receiptRequest: JsonSerializer.Deserialize<ReceiptRequest>(x.request)!, receiptResponse: JsonSerializer.Deserialize<ReceiptResponse>(x.response))).ToList();
        var actualReceiptRequests = receiptRequests.Where(x => x.receiptResponse != null && ((long) x.receiptResponse.ftState & 0xFF) == 0x00).Cast<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>().ToList();
        if (to < 0)
        {
            actualReceiptRequests = actualReceiptRequests.Take(-to).ToList();
        }
        actualReceiptRequests = actualReceiptRequests.OrderBy(x => x.receiptRequest.cbReceiptMoment).ToList();
        var invoices = actualReceiptRequests.Where(x => !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004) && !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002)).Select(x => SAFTMapping.GetInvoiceForReceiptRequest(x)).Where(x => x != null).ToList();
        var workingDocuments = actualReceiptRequests.Where(x => x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004)).Select(x => SAFTMapping.GetWorkDocumentForReceiptRequest(x)).Where(x => x != null).ToList();
        var paymentDocuments = actualReceiptRequests.Where(x => x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002)).Select(x => SAFTMapping.GetPaymentForReceiptRequest(x)).Where(x => x != null).ToList();
        return new AuditFile
        {
            Header = GetHeader(accountMasterData),
            MasterFiles = new MasterFiles
            {
                Customer = [.. actualReceiptRequests.Select(x => GetCustomerData(x.receiptRequest)).DistinctBy(x => x.CustomerID)],
                Product = GetProducts(actualReceiptRequests.Select(x => x.receiptRequest).ToList()),
                TaxTable = GetTaxTable(actualReceiptRequests.Select(x => x).ToList())
            },
            SourceDocuments = new SourceDocuments
            {
                SalesInvoices = new SalesInvoices
                {
                    NumberOfEntries = invoices.Count,
                    TotalDebit = invoices.SelectMany(x => x!.Line).Sum(x => x.DebitAmount ?? 0.0m),
                    TotalCredit = invoices.SelectMany(x => x!.Line).Sum(x => x.CreditAmount ?? 0.0m),
                    Invoice = invoices!
                },
                WorkingDocuments = new WorkingDocuments
                {
                    NumberOfEntries = workingDocuments.Count,
                    TotalDebit = workingDocuments.SelectMany(x => x!.Line).Sum(x => x.DebitAmount ?? 0.0m),
                    TotalCredit = workingDocuments.SelectMany(x => x!.Line).Sum(x => x.CreditAmount ?? 0.0m),
                    WorkDocument = workingDocuments!
                },
                Payments = new Payments
                {
                    NumberOfEntries = paymentDocuments.Count,
                    TotalDebit = 0,
                    TotalCredit = paymentDocuments.SelectMany(x => x!.Line).Sum(x => x.CreditAmount ?? 0.0m),
                    Payment = paymentDocuments!,
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
                ProductCode = x.ProductNumber ?? Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(x.Description + x.Amount + x.VATRate))),
                ProductGroup = x.ProductGroup,
                ProductDescription = x.Description,
                ProductNumberCode = x.ProductNumber ?? Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(x.Description))),
            };
        }).DistinctBy(x => x.ProductCode).ToList();
    }

    private static TaxTable GetTaxTable(List<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)> receipt)
    {
        var staticTaxes = new List<TaxTableEntry> {
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
                }
        };
        var lines = receipt.SelectMany(x => GetGroupedChargeItems(x.receiptRequest).Select(c => GetLine(x.receiptRequest, x.receiptResponse, c)));
        var taxTableEntries = lines.Select(x => staticTaxes.Single(t => t.TaxType == x.Tax.TaxType && t.TaxCountryRegion == x.Tax.TaxCountryRegion && t.TaxCode == x.Tax.TaxCode && t.TaxPercentage == x.Tax.TaxPercentage)).DistinctBy(x => x.TaxCode).ToList();
        return new TaxTable
        {
            TaxTableEntry = taxTableEntries
        };

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
            DateCreated = DateTime.UtcNow,
            TaxEntity = "GLOBAL",
            ProductCompanyTaxID = CertificationPosSystem.ProductCompanyTaxID,
            SoftwareCertificateNumber = CertificationPosSystem.SoftwareCertificateNumber,
            ProductID = CertificationPosSystem.ProductID,
            ProductVersion = CertificationPosSystem.ProductVersion,
        };
    }

    public static WorkDocument? GetWorkDocumentForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var lines = GetGroupedChargeItems(receiptRequest).Select(x => GetLine(receiptRequest, receipt.receiptResponse, x)).ToList();
        if (lines.Count == 0)
        {
            return null;
        }

        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.VATAmount.GetValueOrDefault() * -1 : x.VATAmount.GetValueOrDefault());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.Amount * -1 : x.Amount);
        var hashSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.Hash)).FirstOrDefault();
        var atcudSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.ATCUD)).FirstOrDefault()!;
        atcudSignature.Data = atcudSignature.Data.Replace("ATCUD: ", "");
        var netAmount = grossAmount - taxable;
        var invoiceType = GetInvoiceType(receiptRequest);
        if (hashSignature == null || atcudSignature == null)
        {
            return null;
        }
        var customer = GetCustomerData(receiptRequest);
        var workDocument = new WorkDocument
        {
            DocumentNumber = receipt.receiptResponse.ftReceiptIdentification,
            ATCUD = atcudSignature.Data,
            DocumentStatus = new WorkDocumentStatus
            {
                WorkStatus = "N",
                WorkStatusDate = receiptRequest.cbReceiptMoment,
                SourceID = JsonSerializer.Serialize(receiptRequest.cbUser),
                SourceBilling = "P",
            },
            Hash = hashSignature.Data,
            HashControl = 1,
            Period = receiptRequest.cbReceiptMoment.Month,
            WorkDate = receiptRequest.cbReceiptMoment,
            WorkType = GetWorkType(receiptRequest),
            SourceID = JsonSerializer.Serialize(receiptRequest.cbUser),
            SystemEntryDate = receiptRequest.cbReceiptMoment,
            Line = lines,
            CustomerID = customer.CustomerID,
            DocumentTotals = new WorkDocumentTotals
            {
                TaxPayable = Helpers.CreateTwoDigitMonetaryValue(taxable),
                NetTotal = Helpers.CreateTwoDigitMonetaryValue(netAmount),
                GrossTotal = Helpers.CreateTwoDigitMonetaryValue(grossAmount),
            }
        };
        return workDocument;
    }

    public static PaymentDocument? GetPaymentForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var payItem = receiptRequest.cbPayItems.First();
        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.VATAmount.GetValueOrDefault() * -1 : x.VATAmount.GetValueOrDefault());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.Amount * -1 : x.Amount);
        var hashSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.Hash)).FirstOrDefault();
        var atcudSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.ATCUD)).FirstOrDefault()!;
        atcudSignature.Data = atcudSignature.Data.Replace("ATCUD: ", "");
        var netAmount = grossAmount - taxable;
        var invoiceType = GetInvoiceType(receiptRequest);
        if (hashSignature == null || atcudSignature == null)
        {
            return null;
        }

        var customer = GetCustomerData(receiptRequest);
        var workDocument = new PaymentDocument
        {
            PaymentRefNo = receipt.receiptResponse.ftReceiptIdentification,
            ATCUD = atcudSignature.Data,
            DocumentStatus = new PaymentDocumentStatus
            {
                PaymentStatus = "N",
                PaymentStatusDate = receiptRequest.cbReceiptMoment,
                SourceID = JsonSerializer.Serialize(receiptRequest.cbUser),
                SourcePayment = "P",
            },
            Period = receiptRequest.cbReceiptMoment.Month,
            TransactionDate = receiptRequest.cbReceiptMoment,
            PaymentType = GetPaymentType(receiptRequest),
            PaymentMethod = new PaymentMethod
            {
                PaymentAmount = payItem.Amount,
                PaymentDate = receiptRequest.cbReceiptMoment
            },
            SourceID = JsonSerializer.Serialize(receiptRequest.cbUser),
            SystemEntryDate = receiptRequest.cbReceiptMoment,
            Line = [
                new PaymentLine
                {
                    LineNumber = 1,
                    CreditAmount = payItem.Amount
                }
            ],
            CustomerID = customer.CustomerID,
            DocumentTotals = new PaymentTotals
            {
                TaxPayable = Helpers.CreateTwoDigitMonetaryValue(taxable),
                NetTotal = Helpers.CreateTwoDigitMonetaryValue(payItem.Amount),
                GrossTotal = Helpers.CreateTwoDigitMonetaryValue(payItem.Amount),
            }
        };

        if (GetPaymentType(receiptRequest) == "RG" && receiptRequest.cbPreviousReceiptReference is not null)
        {
            var referencedReceiptReference = ((JsonElement) receipt.receiptResponse.ftStateData!).GetProperty("ReferencedReceiptResponse").Deserialize<ReceiptResponse>();
            workDocument.Line[0].SourceDocumentID = new SourceDocument
            {
                OriginatingON = referencedReceiptReference!.ftReceiptIdentification,
                InvoiceDate = receiptRequest.cbReceiptMoment
            };
        }

        return workDocument;
    }

    public static Invoice? GetInvoiceForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var lines = GetGroupedChargeItems(receiptRequest).Select(x => GetLine(receiptRequest, receipt.receiptResponse, x)).ToList();
        if (lines.Count == 0)
        {
            return null;
        }

        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.VATAmount.GetValueOrDefault() * -1 : x.VATAmount.GetValueOrDefault());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.Amount * -1 : x.Amount);
        var hashSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.Hash)).FirstOrDefault();
        var atcudSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.ATCUD)).FirstOrDefault()!;
        atcudSignature.Data = atcudSignature.Data.Replace("ATCUD: ", "");
        var netAmount = grossAmount - taxable;
        var invoiceType = GetInvoiceType(receiptRequest);
        if (hashSignature == null || atcudSignature == null)
        {
            return null;
        }
        var customer = GetCustomerData(receiptRequest);
        var invoice = new Invoice
        {
            InvoiceNo = receipt.receiptResponse.ftReceiptIdentification,
            ATCUD = atcudSignature.Data,
            DocumentStatus = new InvoiceDocumentStatus
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
            Line = lines,
            CustomerID = customer.CustomerID,
            DocumentTotals = new DocumentTotals
            {
                TaxPayable = Helpers.CreateTwoDigitMonetaryValue(taxable),
                NetTotal = Helpers.CreateTwoDigitMonetaryValue(netAmount),
                GrossTotal = Helpers.CreateTwoDigitMonetaryValue(grossAmount),
            }
        };
        //if (lines.Any(x => x.SettlementAmount.HasValue))
        //{
        //    invoice.DocumentTotals.Settlement = new Settlement
        //    {
        //        SettlementAmount = lines.Sum(x => x.SettlementAmount ?? 0)
        //    };
        //}
        invoice.DocumentTotals.Payment = receiptRequest.cbPayItems.Select(x => GetPayment(receiptRequest, x)).ToList();
        return invoice;
    }

    public static Payment GetPayment(ReceiptRequest receiptRequest, PayItem payItem)
    {
        var amount = payItem.Amount;
        if (payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Refund))
        {
            amount *= -1;
        }
        return new Payment
        {
            PaymentAmount = amount,
            PaymentDate = payItem.Moment ?? receiptRequest.cbReceiptMoment,
            PaymentMechanism = GetPaymentMecahnism(payItem),
        };
    }

    private static string GetWorkType(ReceiptRequest receiptRequest)
    {
        return receiptRequest.ftReceiptCase.Case() switch
        {
            _ => "PF"
        };
    }

    private static string GetPaymentType(ReceiptRequest receiptRequest)
    {
        return receiptRequest.ftReceiptCase.Case() switch
        {
            _ => "RG"
        };
    }

    private static string GetInvoiceType(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001) && receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            return "NC";
        }

        return receiptRequest.ftReceiptCase.Case() switch
        {
            ReceiptCase.UnknownReceipt0x0000 => "FS",
            ReceiptCase.PointOfSaleReceipt0x0001 => "FS",
            ReceiptCase.PaymentTransfer0x0002 => "FS",
            ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003 => "FS",
            ReceiptCase.ECommerce0x0004 => "FS",
            ReceiptCase.DeliveryNote0x0005 => "FS", // no invoicetype.. workign document?
            ReceiptCase.InvoiceUnknown0x1000 => "FT",
            ReceiptCase.InvoiceB2C0x1001 => "FT",
            ReceiptCase.InvoiceB2B0x1002 => "FT",
            ReceiptCase.InvoiceB2G0x1003 => "FT",
            _ => "FS"
        };
    }

    public static Line GetLine(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, (ChargeItem chargeItem, List<ChargeItem> modifiers) chargeItem)
    {
        var chargeItemData = chargeItem.chargeItem;
        var tax = new Tax
        {
            TaxType = "IVA", // one of IVA => vat; IS => stamp duty; NS => Not subject to VAT or Stamp Duty.
            TaxCountryRegion = "PT", // will depend on the location of the taxpayer.. autonomous regions madeira and azores
            TaxCode = GetIVATAxCode(chargeItemData),
            TaxPercentage = Helpers.CreateMonetaryValue(chargeItemData.VATRate)
        };

        var unitPrice = 0m;
        var grossAmount = chargeItemData.Amount;
        var vatAmount = chargeItem.chargeItem.VATAmount ?? 0.0m;
        var netAmount = grossAmount - vatAmount;

        var grossAmountModifiers = chargeItem.modifiers.Sum(x => x.Amount);
        var vatAmountModifiers = chargeItem.modifiers.Sum(x => x.VATAmount ?? 0.0m);
        var netAmountModifiers = grossAmountModifiers - vatAmountModifiers;
        if (netAmountModifiers < 0)
        {
            netAmountModifiers *= -1;
        }
        var netLinePrice = netAmount - netAmountModifiers;
        var quantity = chargeItemData.Quantity;
        if (chargeItem.chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund))
        {
            netLinePrice *= -1;
            quantity *= -1;
        }

        if (chargeItemData.Amount == 0 || quantity == 0)
        {
            unitPrice = 0m;
        }
        else
        {
            // calculate 5 digits after digit
            unitPrice = netLinePrice / quantity;
        }
        var line = new Line
        {
            LineNumber = (long) chargeItemData.Position,
            ProductCode = chargeItemData.ProductNumber ?? Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(chargeItemData.Description))),
            ProductDescription = chargeItemData.Description,
            Quantity = Helpers.CreateMonetaryValue(quantity),
            UnitOfMeasure = chargeItemData.Unit ?? "Unit",
            UnitPrice = Helpers.CreateMonetaryValue(unitPrice),
            TaxPointDate = chargeItemData.Moment ?? receiptRequest.cbReceiptMoment,
            Description = chargeItemData.Description,

            Tax = tax
        };

        if (GetInvoiceType(receiptRequest) == "NC")
        {
            var referencedReceiptReference = ((JsonElement) receiptResponse.ftStateData!).GetProperty("ReferencedReceiptResponse").Deserialize<ReceiptResponse>();
            line.References = new References
            {
                Reference = referencedReceiptReference!.ftReceiptIdentification,
                Reason = "Devolução"
            };
            line.DebitAmount = Helpers.CreateMonetaryValue(netLinePrice);
        }
        else
        {
            line.CreditAmount = Helpers.CreateMonetaryValue(netLinePrice);
        }

        if (GetInvoiceType(receiptRequest) == "FT" && receiptRequest.cbPreviousReceiptReference is not null)
        {
            var referencedReceiptReference = ((JsonElement) receiptResponse.ftStateData!).GetProperty("ReferencedReceiptResponse").Deserialize<ReceiptResponse>();
            line.OrderReferences = new OrderReferences
            {
                OriginatingON = referencedReceiptReference!.ftReceiptIdentification,
                OrderDate = referencedReceiptReference!.ftReceiptMoment
            };
        }

        if (chargeItem.modifiers.Count > 0)
        {
            line.SettlementAmount = netAmountModifiers;
        }
        if (((long) chargeItemData.ftChargeItemCase & (long) 0xFF00) > 0x0000)
        {
            line.TaxExemptionReason = GetTaxExemptionReason(chargeItemData);
            line.TaxExemptionCode = GetTaxExemptionCode(chargeItemData);
        }
        return line;
    }

    // https://taxfoundation.org/data/all/eu/value-added-tax-2024-vat-rates-europe/
    public static string GetIVATAxCode(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.Vat() switch
    {
        ChargeItemCase.UnknownService => throw new NotImplementedException("There is no unkown rate in Portugal"),
        ChargeItemCase.DiscountedVatRate1 => "RED",
        ChargeItemCase.DiscountedVatRate2 => "INT",
        ChargeItemCase.NormalVatRate => "NOR",
        ChargeItemCase.SuperReducedVatRate1 => throw new NotImplementedException("There is no super-reduced-1 rate in Portugal"),
        ChargeItemCase.SuperReducedVatRate2 => throw new NotImplementedException("There is no super-reduced-2 rate in Portugal"),
        ChargeItemCase.ParkingVatRate => throw new NotImplementedException("There is no parking vat rate in Portugal"),
        ChargeItemCase.ZeroVatRate => throw new NotImplementedException("There is no zero rate in Portugal"),
        ChargeItemCase.NotTaxable => "ISE",
        ChargeItemCase c => throw new NotImplementedException($"The given tax scheme 0x{c:X} is not supported in Portugal"),
    };

    public static string GetTaxExemptionCode(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.NatureOfVat() switch
    {
        _ => "M16",
    };

    public static string GetTaxExemptionReason(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.NatureOfVat() switch
    {
        _ => "Isento Artigo 14.  do RITI (ou similar)",
    };

    public static string GetPaymentMecahnism(PayItem payItem) => payItem.ftPayItemCase.Case() switch
    {
        PayItemCase.UnknownPaymentType => "OU", // Unknown � Other means not mentioned
        PayItemCase.CashPayment => "NU", // Cash
        PayItemCase.NonCash => "OU", // Non Cash � Other means not mentioned
        PayItemCase.CrossedCheque => "CH", // Bank cheque
        PayItemCase.DebitCardPayment => "CD", // Debit Card
        PayItemCase.CreditCardPayment => "CC", // Credit Card
        PayItemCase.VoucherPaymentCouponVoucherByMoneyValue => "CO", // Voucher Gift cheque or gift card;
        PayItemCase.OnlinePayment => "OU", // Online payment � Other means not mentioned
        PayItemCase.LoyaltyProgramCustomerCardPayment => "OU", // Online payment � Other means not mentioned
        _ => "OU", // Other � Other means not mentioned
    };

    public static string GetProductType(ChargeItem chargeItem) => chargeItem.ftChargeItemCase.TypeOfService() switch
    {
        ChargeItemCaseTypeOfService.UnknownService => "O", // Unknown type of service / - Others (e.g. charged freights, advance payments received or sale of assets);
        ChargeItemCaseTypeOfService.Delivery => "P", // Delivery (supply of goods) / Products
        ChargeItemCaseTypeOfService.OtherService => "S", // Other service (supply of service) / Services
        ChargeItemCaseTypeOfService.Tip => "S", // Tip / Services
        ChargeItemCaseTypeOfService.Voucher => "?", // Voucher / ???
        ChargeItemCaseTypeOfService.CatalogService => "S", // Catalog Service / Services
        ChargeItemCaseTypeOfService.NotOwnSales => "?", // Not own sales Agency busines / ???
        ChargeItemCaseTypeOfService.OwnConsumption => "?", // Own Consumption / ???
        ChargeItemCaseTypeOfService.Grant => "?", // Grant / ???
        ChargeItemCaseTypeOfService.Receivable => "?", // Receivable / ???
        ChargeItemCaseTypeOfService.CashTransfer => "?", // Receivable / ???
        _ => throw new NotImplementedException($"The given ChargeItemCase {chargeItem.ftChargeItemCase} type is not supported"),
    };

    public static List<(ChargeItem chargeItem, List<ChargeItem> modifiers)> GetGroupedChargeItems(this ReceiptRequest receiptRequest)
    {
        var data = new List<(ChargeItem chargeItem, List<ChargeItem> modifiers)>();
        foreach (var receiptChargeItem in receiptRequest.cbChargeItems)
        {
            if (((long) receiptChargeItem.ftChargeItemCase & 0x0000_0000_0004_0000) > 0)
            {
                var last = data.LastOrDefault();
                if (last == default)
                {
                    data.Add((receiptChargeItem, new List<ChargeItem>()));
                }
                else
                {
                    last.modifiers.Add(receiptChargeItem);
                }
            }
            else
            {
                data.Add((receiptChargeItem, new List<ChargeItem>()));
            }
        }
        return data;
    }
}