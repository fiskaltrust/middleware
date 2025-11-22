using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.PaymentDocumentModels;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

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
            Country = "Desconhecido"
        }
    };

    private Dictionary<string, (ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)> InvoicedProformas { get; } = new Dictionary<string, (ReceiptRequest, ReceiptResponse)>();

    /// <summary>
    /// Validates a Portuguese Tax Identification Number (NIF - Número de Identificação Fiscal).
    /// This method delegates to the centralized validation in ReceiptRequestValidatorPT.
    /// </summary>
    /// <param name="taxId">The tax ID to validate</param>
    /// <returns>True if the tax ID is valid, false otherwise</returns>
    public static bool IsValidPortugueseTaxId(string taxId)
    {
        return PortugalValidationHelpers.IsValidPortugueseTaxId(taxId);
    }

    private static string GetCustomerCountry(MiddlewareCustomer middlewareCustomer)
    {
        if (!string.IsNullOrEmpty(middlewareCustomer.CustomerCountry))
        {
            return middlewareCustomer.CustomerCountry;
        }

        if (!string.IsNullOrEmpty(middlewareCustomer.CustomerVATId) && IsValidPortugueseTaxId(middlewareCustomer.CustomerVATId))
        {
            return "PT";
        }
        return "Desconhecido";
    }

    public string GetSourceID(ReceiptRequest receiptRequest)
    {
        var user = receiptRequest.GetcbUserOrNull();
        if(string.IsNullOrEmpty(user))
        {
            // This should not happen in production since we have inbound validation. For data that made it into the queue we rather export something than fail the export.
            return "Desconhecido";
        }

        return Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(user)));
    }

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
            CompanyName = string.IsNullOrEmpty(middlewareCustomer.CustomerName) ? "Desconhecido" : middlewareCustomer.CustomerName,
            CustomerTaxID = string.IsNullOrEmpty(middlewareCustomer.CustomerVATId) ? "999999990" : middlewareCustomer.CustomerVATId,
            BillingAddress = new BillingAddress
            {
                AddressDetail = string.IsNullOrEmpty(middlewareCustomer.CustomerStreet) ? "Desconhecido" : middlewareCustomer.CustomerStreet,
                City = string.IsNullOrEmpty(middlewareCustomer.CustomerCity) ? "Desconhecido" : middlewareCustomer.CustomerCity,
                PostalCode = string.IsNullOrEmpty(middlewareCustomer.CustomerZip) ? "Desconhecido" : middlewareCustomer.CustomerZip,
                Country = GetCustomerCountry(middlewareCustomer)
            }
        };
        return customer;
    }

    public byte[] SerializeAuditFile(AccountMasterData accountMasterData, List<ftQueueItem> queueItems, int to)
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
        return memoryStream.ToArray();
    }

    public AuditFile CreateAuditFile(AccountMasterData accountMasterData, List<ftQueueItem> queueItems, int to)
    {
        var receiptRequests = queueItems.Select(x => (receiptRequest: JsonSerializer.Deserialize<ReceiptRequest>(x.request)!, receiptResponse: JsonSerializer.Deserialize<ReceiptResponse>(x.response))).ToList();
        var actualReceiptRequests = receiptRequests.Where(x => x.receiptResponse != null && ((long) x.receiptResponse.ftState & 0xFF) == 0x00 && 
            !x.receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.Lifecycle)
            && !x.receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.DailyOperations)
            && !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.ProtocolUnspecified0x3000)
            && !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.ProtocolTechnicalEvent0x3001)
            && !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.ProtocolAccountingEvent0x3002)).Cast<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)>().ToList();
        if (to < 0)
        {
            actualReceiptRequests = actualReceiptRequests.Take(-to).ToList();
        }
        actualReceiptRequests = actualReceiptRequests.OrderBy(x => x.receiptRequest.cbReceiptMoment).ToList();
        var invoices = actualReceiptRequests.Where(x =>  !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.ProtocolAccountingEvent0x3002) && !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004) && !x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002)).Select(x => GetInvoiceForReceiptRequest(x)).Where(x => x != null).OrderBy(x => x.InvoiceNo.Split("/")[0]).ThenBy(x => int.Parse(x.InvoiceNo.Split("/")[1])).ToList();
        var workingDocuments = actualReceiptRequests.Where(x => x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.Order0x3004)).Select(x => GetWorkDocumentForReceiptRequest(x)).Where(x => x != null).OrderBy(x => x.DocumentNumber.Split("/")[0]).ThenBy(x => int.Parse(x.DocumentNumber.Split("/")[1])).ToList();
        var paymentDocuments = actualReceiptRequests.Where(x => x.receiptRequest.ftReceiptCase.IsCase(ReceiptCase.PaymentTransfer0x0002)).Select(x => GetPaymentForReceiptRequest(x)).Where(x => x != null).OrderBy(x => x.PaymentRefNo.Split("/")[0]).ThenBy(x => int.Parse(x.PaymentRefNo.Split("/")[1])).ToList();
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
        if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount) || chargeItem.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.Receivable))
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
        if(x.ProductNumber != null && x.ProductNumber.Length >=3)
        {
            return x.ProductNumber;
        }

        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(x.Description)));
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
        var lines = receipt.Where(x => !x.receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)).SelectMany(x => x.receiptRequest.GetGroupedChargeItemsModifyPositionsIfNotSet().Select(c => GetLine(x.receiptRequest, x.receiptResponse, c)));
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
        var lines = receiptRequest.GetGroupedChargeItemsModifyPositionsIfNotSet().Select(x => GetLine(receiptRequest, receipt.receiptResponse, x)).ToList();
        if (lines.Count == 0)
        {
            return null;
        }

        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.GetVATAmount() * -1 : x.GetVATAmount());
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
        
        // Convert UTC time to Portugal local time
        var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receiptRequest.cbReceiptMoment);
        
        var customer = GetCustomerData(receiptRequest);
        var workDocument = new WorkDocument
        {
            DocumentNumber = receipt.receiptResponse.ftReceiptIdentification.Split("#").Last(),
            ATCUD = atcudSignature.Data,
            DocumentStatus = new WorkDocumentStatus
            {
                WorkStatus = "N",
                WorkStatusDate = portugalTime,
                SourceID = GetSourceID(receiptRequest),
                SourceBilling = GetSourceBilling(receiptRequest),
            },
            Hash = hashSignature.Data,
            HashControl = 1,
            Period = portugalTime.Month,
            WorkDate = portugalTime,
            WorkType = PTMappings.GetWorkType(receiptRequest),
            SourceID = GetSourceID(receiptRequest),
            SystemEntryDate = portugalTime,
            Line = lines,
            CustomerID = customer.CustomerID,
            DocumentTotals = new WorkDocumentTotals
            {
                TaxPayable = Helpers.CreateTwoDigitMonetaryValue(taxable),
                NetTotal = Helpers.CreateTwoDigitMonetaryValue(netAmount),
                GrossTotal = Helpers.CreateTwoDigitMonetaryValue(grossAmount),
            }
        };

        if (InvoicedProformas.ContainsKey(receipt.receiptResponse.ftReceiptIdentification.Split("#").Last()))
        {
            var request = InvoicedProformas[receipt.receiptResponse.ftReceiptIdentification.Split("#").Last()].receiptRequest;
            var referencedPortugalTime = PortugalTimeHelper.ConvertToPortugalTime(request.cbReceiptMoment);
            workDocument.DocumentStatus.WorkStatus = "F";
            workDocument.DocumentStatus.WorkStatusDate = referencedPortugalTime;
            workDocument.DocumentStatus.SourceID = GetSourceID(request);
            workDocument.DocumentStatus.Reason = "Faturado";
        }

        return workDocument;
    }

    public PaymentDocument? GetPaymentForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var payItem = receiptRequest.cbPayItems.First();
        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.GetVATAmount() * -1 : x.GetVATAmount());
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

        // Convert UTC time to Portugal local time
        var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receiptRequest.cbReceiptMoment);

        var customer = GetCustomerData(receiptRequest);
        var workDocument = new PaymentDocument
        {
            PaymentRefNo = receipt.receiptResponse.ftReceiptIdentification.Split("#").Last(),
            ATCUD = atcudSignature.Data,
            DocumentStatus = new PaymentDocumentStatus
            {
                PaymentStatus = "N",
                PaymentStatusDate = portugalTime,
                SourceID = GetSourceID(receiptRequest),
                SourcePayment = GetSourcePayment(receiptRequest)
            },
            Period = portugalTime.Month,
            TransactionDate = portugalTime,
            PaymentType = PTMappings.GetPaymentType(receiptRequest),
            PaymentMethod = new PaymentMethod
            {
                PaymentMechanism = PTMappings.GetPaymentMecahnism(payItem),
                PaymentAmount = payItem.Amount,
                PaymentDate = portugalTime
            },
            SourceID = GetSourceID(receiptRequest),
            SystemEntryDate = portugalTime,
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
                OriginatingON = referencedReceiptReference!.ftReceiptIdentification.Split("#").Last(),
                InvoiceDate = receiptRequest.cbReceiptMoment
            };
        }

        return workDocument;
    }

    public Invoice? GetInvoiceForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var receiptResponse = receipt.receiptResponse;
        var lines = receiptRequest.GetGroupedChargeItemsModifyPositionsIfNotSet().Select(x => GetLine(receiptRequest, receipt.receiptResponse, x)).ToList();
        if (lines.Count == 0)
        {
            return null;
        }

        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? x.GetVATAmount() * -1 : x.GetVATAmount());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) ? x.Amount * -1 : x.Amount);
        var hashSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.Hash)).FirstOrDefault();
        var atcudSignature = receipt.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypePT.ATCUD)).FirstOrDefault()!;
        if (atcudSignature != null)
        {
            atcudSignature.Data = atcudSignature.Data.Replace("ATCUD: ", "");
        }
        var netAmount = grossAmount - taxable;
        var invoiceType = PTMappings.GetInvoiceType(receiptRequest);
        var customer = GetCustomerData(receiptRequest);
        
        // Convert UTC time to Portugal local time
        var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receiptRequest.cbReceiptMoment);
        
        var invoice = new Invoice
        {
            InvoiceNo = receipt.receiptResponse.ftReceiptIdentification.Split("#").Last(),
            ATCUD = atcudSignature?.Data ?? "",
            DocumentStatus = new InvoiceDocumentStatus
            {
                InvoiceStatus = "N",
                InvoiceStatusDate = portugalTime,
                SourceID = GetSourceID(receiptRequest),
                SourceBilling = GetSourceBilling(receiptRequest),
            },
            Hash = hashSignature?.Data ?? "",
            HashControl = "1",
            Period = portugalTime.Month,
            InvoiceDate = portugalTime,
            InvoiceType = PTMappings.GetInvoiceType(receiptRequest),
            SpecialRegimes = new SpecialRegimes
            {
                SelfBillingIndicator = 0,
                CashVATSchemeIndicator = 0,
                ThirdPartiesBillingIndicator = 0,
            },
            SourceID = GetSourceID(receiptRequest),
            SystemEntryDate = portugalTime,
            Line = lines,
            CustomerID = customer.CustomerID,
            DocumentTotals = new DocumentTotals
            {
                TaxPayable = Helpers.CreateTwoDigitMonetaryValue(taxable),
                NetTotal = Helpers.CreateTwoDigitMonetaryValue(netAmount),
                GrossTotal = Helpers.CreateTwoDigitMonetaryValue(grossAmount),
            }
        };

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            var handwrittenPortugalTime = PortugalTimeHelper.ConvertToPortugalTime(receiptResponse.ftReceiptMoment);
            invoice.DocumentStatus.InvoiceStatusDate = handwrittenPortugalTime;
            invoice.SystemEntryDate = handwrittenPortugalTime;
        }

        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && receiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var data))
        {
            invoice.HashControl = invoice.HashControl + "-" + PTMappings.GetInvoiceType(receiptRequest) + "M " + data.PT.Series + "/" + data.PT.Number!.ToString()!.PadLeft(4, '0');
        }
        //if (lines.Any(x => x.SettlementAmount.HasValue))
        //{
        //    invoice.DocumentTotals.Settlement = new Settlement
        //    {
        //        SettlementAmount = lines.Sum(x => x.SettlementAmount ?? 0)
        //    };
        //}
        invoice.DocumentTotals.Payment = receiptRequest.cbPayItems.Where(x => !x.ftPayItemCase.IsCase(PayItemCase.AccountsReceivable)).Select(x => GetPayment(receiptRequest, x)).ToList();
        return invoice;
    }

    private static string GetSourcePayment(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            return "M";
        }
        return "P";
    }

    private static string GetSourceBilling(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            return "M";
        }
        return "P";
    }

    public Payment GetPayment(ReceiptRequest receiptRequest, PayItem payItem)
    {
        var amount = payItem.Amount;
        if (payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Refund) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
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
        var vatAmount = chargeItem.chargeItem.GetVATAmount();
        var netAmount = grossAmount - vatAmount;

        var grossAmountModifiers = chargeItem.modifiers.Sum(x => x.Amount);
        var vatAmountModifiers = chargeItem.modifiers.Sum(x => x.GetVATAmount());
        var netAmountModifiers = grossAmountModifiers - vatAmountModifiers;
        if (netAmountModifiers < 0)
        {
            netAmountModifiers *= -1;
        }
        var netLinePrice = netAmount - netAmountModifiers;
        var quantity = chargeItemData.Quantity;
        if (chargeItem.chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
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
                Reference = referencedReceiptReference!.ftReceiptIdentification.Split("#").Last(),
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
                OriginatingON = referencedReceiptReference!.ftReceiptIdentification.Split("#").Last(),
                OrderDate = referencedReceiptReference!.ftReceiptMoment
            };
            if (!InvoicedProformas.ContainsKey(referencedReceiptReference!.ftReceiptIdentification.Split("#").Last()))
            {
                InvoicedProformas.Add(referencedReceiptReference.ftReceiptIdentification.Split("#").Last(), (receiptRequest, receiptResponse));
            }
        }


        if (PTMappings.GetInvoiceType(receiptRequest) == "FS" && receiptRequest.cbPreviousReceiptReference != null)
        {
            var referencedReceiptReference = ((JsonElement) receiptResponse.ftStateData!).GetProperty("ReferencedReceiptResponse").Deserialize<ReceiptResponse>();
            line.OrderReferences = new OrderReferences
            {
                OriginatingON = referencedReceiptReference!.ftReceiptIdentification.Split("#").Last(),
                OrderDate = referencedReceiptReference!.ftReceiptMoment
            };
            if (!InvoicedProformas.ContainsKey(referencedReceiptReference!.ftReceiptIdentification.Split("#").Last()))
            {
                InvoicedProformas.Add(referencedReceiptReference.ftReceiptIdentification.Split("#").Last(), (receiptRequest, receiptResponse));
            }
        }

        if (chargeItem.modifiers.Count > 0)
        {
            line.SettlementAmount = netAmountModifiers;
        }
        if (((long) chargeItemData.ftChargeItemCase &  0xFF00) > 0x0000)
        {
            line.TaxExemptionReason = PTMappings.GetTaxExemptionReason(chargeItemData);
            line.TaxExemptionCode = PTMappings.GetTaxExemptionCode(chargeItemData);
        }
        return line;

    }
}