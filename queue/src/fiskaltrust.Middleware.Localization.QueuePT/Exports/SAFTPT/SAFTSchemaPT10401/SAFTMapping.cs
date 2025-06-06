using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using System.Xml;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments.PaymentDocumentModels;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

public static class CertificationPosSystem
{
    public const string ProductCompanyTaxID = "980833310";
    public const string SoftwareCertificateNumber = "9999";
    public const string ProductID = "fiskaltrust.CloudCashBox/FISKALTRUST CONSULTING GMBH - Sucursal em Portugal";
    public const string ProductVersion = "2.0";
}

public class SaftExporter
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

    private Dictionary<string, (ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)> InvoicedProformas { get; } = new Dictionary<string, (ReceiptRequest, ReceiptResponse)>();

    public Customer GetCustomerData(ReceiptRequest receiptRequest)
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

    public string SerializeAuditFile(AccountMasterData accountMasterData, List<ftQueueItem> queueItems, int to)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var data = CreateAuditFile(accountMasterData, queueItems, to);
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

    public AuditFile CreateAuditFile(AccountMasterData accountMasterData, List<ftQueueItem> queueItems, int to)
    {
        var receiptRequests = queueItems.Select(x => (receiptRequest: JsonSerializer.Deserialize<ReceiptRequest>(x.request)!, receiptResponse: JsonSerializer.Deserialize<ReceiptResponse>(x.response))).ToList();
        var actualReceiptRequests = receiptRequests.Where(x => x.receiptResponse != null && ((long) x.receiptResponse.ftState & 0xFF) == 0x00).Cast<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>().ToList();
        if (to < 0)
        {
            actualReceiptRequests = actualReceiptRequests.Take(-to).ToList();
        }
        actualReceiptRequests = actualReceiptRequests.OrderBy(x => x.receiptRequest.cbReceiptMoment).ToList();
        var invoices = actualReceiptRequests.Where(x => !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004) && !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002)).Select(x => GetInvoiceForReceiptRequest(x)).Where(x => x != null).ToList();
        var workingDocuments = actualReceiptRequests.Where(x => x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004)).Select(x => GetWorkDocumentForReceiptRequest(x)).Where(x => x != null).ToList();
        var paymentDocuments = actualReceiptRequests.Where(x => x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002)).Select(x => GetPaymentForReceiptRequest(x)).Where(x => x != null).ToList();
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

    public static bool IsProductChargeItem(ChargeItem chargeItem)
    {
        if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount))
        {
            return false;
        }
        return true;
    }

    private List<Product> GetProducts(List<ReceiptRequest> receiptRequest)
    {
        return receiptRequest.SelectMany(x => x.cbChargeItems).Where(IsProductChargeItem).Select(x =>
        {
            return new Product
            {
                ProductType = PTMappings.GetProductType(x),
                ProductCode = GenerateUniqueProductIdentifier(x),
                ProductGroup = x.ProductGroup,
                ProductDescription = x.Description,
                ProductNumberCode = GenerateUniqueProductIdentifier(x),
            };
        }).DistinctBy(x => x.ProductCode).ToList();
    }

    private static string GenerateUniqueProductIdentifier(ChargeItem x)
    {
        var grossAmount = Math.Abs(x.Amount);
        var quantity = Math.Abs(x.Quantity);
        var singleItemPrice = x.Amount == 0 || quantity == 0 ? 0m : grossAmount / quantity;
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(x.Description + singleItemPrice + x.VATRate)));
    }

    private TaxTable GetTaxTable(List<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)> receipt)
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
        var lines = receipt.SelectMany(x => x.receiptRequest.GetGroupedChargeItems().Select(c => GetLine(x.receiptRequest, x.receiptResponse, c)));
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

    public WorkDocument? GetWorkDocumentForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var lines = receiptRequest.GetGroupedChargeItems().Select(x => GetLine(receiptRequest, receipt.receiptResponse, x)).ToList();
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
        var invoiceType = PTMappings.GetInvoiceType(receiptRequest);
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
            WorkType = PTMappings.GetWorkType(receiptRequest),
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

        if (InvoicedProformas.ContainsKey(receipt.receiptResponse.ftReceiptIdentification))
        {
            workDocument.DocumentStatus.WorkStatus = "F";
            workDocument.DocumentStatus.WorkStatusDate = InvoicedProformas[receipt.receiptResponse.ftReceiptIdentification].receiptRequest.cbReceiptMoment;
            workDocument.DocumentStatus.SourceID = JsonSerializer.Serialize(InvoicedProformas[receipt.receiptResponse.ftReceiptIdentification].receiptRequest.cbUser);
            workDocument.DocumentStatus.Reason = "Faturado";
        }

        return workDocument;
    }

    public PaymentDocument? GetPaymentForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var payItem = receiptRequest.cbPayItems.First();
        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.VATAmount.GetValueOrDefault() * -1 : x.VATAmount.GetValueOrDefault());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.Amount * -1 : x.Amount);
        var hashSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.Hash)).FirstOrDefault();
        var atcudSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.ATCUD)).FirstOrDefault()!;
        atcudSignature.Data = atcudSignature.Data.Replace("ATCUD: ", "");
        var netAmount = grossAmount - taxable;
        var invoiceType = PTMappings.GetInvoiceType(receiptRequest);
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
            PaymentType = PTMappings.GetPaymentType(receiptRequest),
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

        if (PTMappings.GetPaymentType(receiptRequest) == "RG" && receiptRequest.cbPreviousReceiptReference != null)
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

    public Invoice? GetInvoiceForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var lines = receiptRequest.GetGroupedChargeItems().Select(x => GetLine(receiptRequest, receipt.receiptResponse, x)).ToList();
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
        var invoiceType = PTMappings.GetInvoiceType(receiptRequest);
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
            InvoiceType = PTMappings.GetInvoiceType(receiptRequest),
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

    public Payment GetPayment(ReceiptRequest receiptRequest, PayItem payItem)
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
            PaymentMechanism = PTMappings.GetPaymentMecahnism(payItem),
        };
    }

    public Line GetLine(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, (ChargeItem chargeItem, List<ChargeItem> modifiers) chargeItem)
    {
        var chargeItemData = chargeItem.chargeItem;
        var tax = new Tax
        {
            TaxType = "IVA", // one of IVA => vat; IS => stamp duty; NS => Not subject to VAT or Stamp Duty.
            TaxCountryRegion = "PT", // will depend on the location of the taxpayer.. autonomous regions madeira and azores
            TaxCode = PTMappings.GetIVATAxCode(chargeItemData),
            TaxPercentage = Helpers.CreateMonetaryValue(chargeItemData.VATRate)
        };


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

        var unitPrice = 0m;
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
            ProductCode = GenerateUniqueProductIdentifier(chargeItemData),
            ProductDescription = chargeItemData.Description,
            Quantity = Helpers.CreateMonetaryValue(quantity),
            UnitOfMeasure = chargeItemData.Unit ?? "Unit",
            UnitPrice = Helpers.CreateMonetaryValue(unitPrice),
            TaxPointDate = chargeItemData.Moment ?? receiptRequest.cbReceiptMoment,
            Description = chargeItemData.Description,

            Tax = tax
        };

        if (PTMappings.GetInvoiceType(receiptRequest) == "NC")
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

        if (PTMappings.GetInvoiceType(receiptRequest) == "FT" && receiptRequest.cbPreviousReceiptReference != null)
        {
            var referencedReceiptReference = ((JsonElement) receiptResponse.ftStateData!).GetProperty("ReferencedReceiptResponse").Deserialize<ReceiptResponse>();
            line.OrderReferences = new OrderReferences
            {
                OriginatingON = referencedReceiptReference!.ftReceiptIdentification,
                OrderDate = referencedReceiptReference!.ftReceiptMoment
            };
            if (!InvoicedProformas.ContainsKey(referencedReceiptReference!.ftReceiptIdentification))
            {
                InvoicedProformas.Add(referencedReceiptReference.ftReceiptIdentification, (receiptRequest, receiptResponse));
            }
        }

        if (chargeItem.modifiers.Count > 0)
        {
            line.SettlementAmount = netAmountModifiers;
        }
        if (((long) chargeItemData.ftChargeItemCase & (long) 0xFF00) > 0x0000)
        {
            line.TaxExemptionReason = PTMappings.GetTaxExemptionReason(chargeItemData);
            line.TaxExemptionCode = PTMappings.GetTaxExemptionCode(chargeItemData);
        }
        return line;

    }
}