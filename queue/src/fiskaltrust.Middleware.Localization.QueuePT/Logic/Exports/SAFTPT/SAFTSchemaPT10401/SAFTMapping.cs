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
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

public class SaftExporter
{

    private readonly DocumentStatusProvider? _documentStatusProvider;

    public static bool IsValidPortugueseTaxId(string taxId)
    {
        return PortugalValidationHelpers.IsValidPortugueseTaxId(taxId);
    }

    private static string GetCustomerCountry(MiddlewareCustomer middlewareCustomer)
    {
        if (!string.IsNullOrEmpty(middlewareCustomer.CustomerCountry))
        {
            return middlewareCustomer.CustomerCountry.Trim();
        }
        else
        {
            return "Desconhecido";
        }
    }

    public SaftExporter(DocumentStatusProvider? documentStatusProvider = null)
    {
        _documentStatusProvider = documentStatusProvider;
    }

    private DocumentStatusProvider DocumentStatusProvider => _documentStatusProvider ?? throw new InvalidOperationException("DocumentStatusProvider is required for this operation.");

    public string GetSourceID(ReceiptRequest receiptRequest)
    {
        var user = receiptRequest.GetcbUserOrNull();
        if (string.IsNullOrEmpty(user))
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
            return PTMappings.AnonymousCustomer;
        }

        var middlewareCustomer = JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer))!;
        if (!string.IsNullOrEmpty(middlewareCustomer.CustomerName))
        {
            middlewareCustomer.CustomerName = middlewareCustomer.CustomerName.Trim();
        }

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
            CompanyName = string.IsNullOrEmpty(middlewareCustomer.CustomerName) ? "Desconhecido" : middlewareCustomer.CustomerName.Trim(),
            CustomerTaxID = string.IsNullOrEmpty(middlewareCustomer.CustomerVATId) ? "999999990" : middlewareCustomer.CustomerVATId.Trim(),
            BillingAddress = new BillingAddress
            {
                AddressDetail = string.IsNullOrEmpty(middlewareCustomer.CustomerStreet) ? "Desconhecido" : middlewareCustomer.CustomerStreet.Trim(),
                City = string.IsNullOrEmpty(middlewareCustomer.CustomerCity) ? "Desconhecido" : middlewareCustomer.CustomerCity.Trim(),
                PostalCode = string.IsNullOrEmpty(middlewareCustomer.CustomerZip) ? "Desconhecido" : middlewareCustomer.CustomerZip.Trim(),
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

    public bool ShouldBeExported(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            // Void is a status, not a document that should be exported
            return false;
        }

        // maybe we should change this to checking for the response
        if (receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.Lifecycle)
            || receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.DailyOperations)
            || receiptRequest.ftReceiptCase.IsType(ReceiptCaseType.Log))
        {
            return false;
        }
        return true;
    }

    public AuditFile CreateAuditFile(AccountMasterData accountMasterData, List<ftQueueItem> queueItems, int to)
    {
        var receipts = queueItems.Where(x => !string.IsNullOrEmpty(x.request) && !string.IsNullOrEmpty(x.response)).Select(x => (receiptRequest: JsonSerializer.Deserialize<ReceiptRequest>(x.request)!, receiptResponse: JsonSerializer.Deserialize<ReceiptResponse>(x.response))).ToList();
        receipts = receipts.Where(x => x.receiptRequest != null && x.receiptResponse != null && x.receiptResponse.ftState.IsState(State.Success)).ToList();
        if (to < 0)
        {
            receipts = receipts.Take(-to).ToList();
        }
        return CreateAuditFile(accountMasterData, receipts);
    }

    public AuditFile CreateAuditFile(AccountMasterData accountMasterData, List<(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)> receipts)
    {
        var actualReceiptRequests = receipts.Where(x => ShouldBeExported(x.receiptRequest)).OrderBy(x => x.receiptResponse.ftReceiptMoment).ToList();

        var invoiceTasks = actualReceiptRequests
            .Where(x => PTMappings.InvoiceTypes.Contains(PTMappings.ExtractDocumentTypeAndUniqueIdentification(x.receiptResponse.ftReceiptIdentification).documentType))
            .Select(x => GetInvoiceForReceiptRequest(x))
            .ToList();
        var invoices = Task.WhenAll(invoiceTasks).GetAwaiter().GetResult()
            .Where(x => x != null)
            .OrderBy(x => x!.InvoiceNo.Split("/")[0])
            .ThenBy(x => int.Parse(x!.InvoiceNo.Split("/")[1]))
            .ToList();

        var workTasks = actualReceiptRequests
            .Where(x => PTMappings.WorkTypes.Contains(PTMappings.ExtractDocumentTypeAndUniqueIdentification(x.receiptResponse.ftReceiptIdentification).documentType))
            .Select(x => GetWorkDocumentForReceiptRequest(x))
            .ToList();
        var workingDocuments = Task.WhenAll(workTasks).GetAwaiter().GetResult()
            .Where(x => x != null)
            .OrderBy(x => x!.DocumentNumber.Split("/")[0])
            .ThenBy(x => int.Parse(x!.DocumentNumber.Split("/")[1]))
            .ToList();

        var paymentTasks = actualReceiptRequests
            .Where(x => PTMappings.PaymentTypes.Contains(PTMappings.ExtractDocumentTypeAndUniqueIdentification(x.receiptResponse.ftReceiptIdentification).documentType))
            .Select(x => GetPaymentForReceiptRequest(x))
            .ToList();
        var paymentDocuments = Task.WhenAll(paymentTasks).GetAwaiter().GetResult()
            .Where(x => x != null)
            .OrderBy(x => x!.PaymentRefNo.Split("/")[0])
            .ThenBy(x => int.Parse(x!.PaymentRefNo.Split("/")[1]))
            .ToList();

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
                    TotalDebit = invoices.Where(x => x!.DocumentStatus.InvoiceStatus != PTMappings.InvoiceStatus.Cancelled).SelectMany(x => x!.Line).Sum(x => x.DebitAmount ?? 0.0m),
                    TotalCredit = invoices.Where(x => x!.DocumentStatus.InvoiceStatus != PTMappings.InvoiceStatus.Cancelled).SelectMany(x => x!.Line).Sum(x => x.CreditAmount ?? 0.0m),
                    Invoice = invoices!
                },
                WorkingDocuments = new WorkingDocuments
                {
                    NumberOfEntries = workingDocuments.Count,
                    TotalDebit = workingDocuments.Where(x => x!.DocumentStatus.WorkStatus != PTMappings.WorkStatus.Cancelled).SelectMany(x => x!.Line).Sum(x => x.DebitAmount ?? 0.0m),
                    TotalCredit = workingDocuments.Where(x => x!.DocumentStatus.WorkStatus != PTMappings.WorkStatus.Cancelled).SelectMany(x => x!.Line).Sum(x => x.CreditAmount ?? 0.0m),
                    WorkDocument = workingDocuments!
                },
                Payments = new Payments
                {
                    NumberOfEntries = paymentDocuments.Count,
                    TotalDebit = 0,
                    TotalCredit = paymentDocuments.Where(x => x!.DocumentStatus.PaymentStatus != PTMappings.PaymentStatus.Cancelled).SelectMany(x => x!.Line).Sum(x => x.CreditAmount ?? 0.0m),
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
                ProductGroup = x.ProductGroup?.Trim(),
                ProductDescription = x.Description?.Trim(),
                ProductNumberCode = GenerateUniqueProductIdentifier(x),
            };
        }).DistinctBy(x => x.ProductCode).ToList();
    }

    public static string GenerateUniqueProductIdentifier(ChargeItem x) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(x.Description.Trim())));

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
            ProductCompanyTaxID = PTMappings.CertificationPosSystem.ProductCompanyTaxID,
            SoftwareCertificateNumber = PTMappings.CertificationPosSystem.SoftwareCertificateNumber,
            ProductID = PTMappings.CertificationPosSystem.ProductID,
            ProductVersion = PTMappings.CertificationPosSystem.ProductVersion,
        };
    }

    public async Task<WorkDocument?> GetWorkDocumentForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var lines = receiptRequest.GetGroupedChargeItemsModifyPositionsIfNotSet().Select(x => GetLine(receiptRequest, receipt.receiptResponse, x)).ToList();
        if (lines.Count == 0)
        {
            return null;
        }

        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.GetVATAmount() * -1 : x.GetVATAmount());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.Amount * -1 : x.Amount);
        var hashSignature = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypePT.Hash));
        var atcudSignature = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypePT.ATCUD))!;
        atcudSignature.Data = atcudSignature.Data.Replace("ATCUD: ", "");
        var netAmount = grossAmount - taxable;
        if (hashSignature == null || atcudSignature == null)
        {
            return null;
        }

        // Convert UTC time to Portugal local time
        var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receipt.receiptResponse.ftReceiptMoment);
        var customer = GetCustomerData(receiptRequest);
        var workDocument = new WorkDocument
        {
            DocumentNumber = receipt.receiptResponse.ftReceiptIdentification.Split("#").Last(),
            ATCUD = atcudSignature.Data,
            DocumentStatus = await GetWorkStatusForDocument(receipt),
            Hash = hashSignature.Data,
            HashControl = 1,
            Period = portugalTime.Month,
            WorkDate = portugalTime,
            WorkType = PTMappings.ExtractDocumentTypeAndUniqueIdentification(receipt.receiptResponse.ftReceiptIdentification).documentType,
            SourceID = GetSourceID(receiptRequest),
            SystemEntryDate = portugalTime,
            Line = lines,
            CustomerID = customer.CustomerID,
            DocumentTotals = new WorkDocumentTotals
            {
                TaxPayable = Helpers.CreateTwoDigitMonetaryValue(taxable),
                NetTotal = Helpers.CreateTwoDigitMonetaryValue(netAmount),
                GrossTotal = Helpers.CreateTwoDigitMonetaryValue(taxable) + Helpers.CreateTwoDigitMonetaryValue(netAmount)
            }
        };
        return workDocument;
    }

    public async Task<PaymentDocument?> GetPaymentForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        var receiptRequest = receipt.receiptRequest;
        var payItem = receiptRequest.cbPayItems.First();
        var taxable = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.GetVATAmount() * -1 : x.GetVATAmount());
        var grossAmount = receiptRequest.cbChargeItems.Sum(x => x.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund) ? x.Amount * -1 : x.Amount);
        var hashSignature = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypePT.Hash));
        var atcudSignature = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypePT.ATCUD))!;
        atcudSignature.Data = atcudSignature.Data.Replace("ATCUD: ", "");
        var netAmount = grossAmount - taxable;
        if (hashSignature == null || atcudSignature == null)
        {
            return null;
        }

        // Convert UTC time to Portugal local time
        var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receipt.receiptResponse.ftReceiptMoment);
        var customer = GetCustomerData(receiptRequest);
        var workDocument = new PaymentDocument
        {
            PaymentRefNo = receipt.receiptResponse.ftReceiptIdentification.Split("#").Last(),
            ATCUD = atcudSignature.Data,
            DocumentStatus = await GetPaymentStatusForDocument(receipt),
            Period = portugalTime.Month,
            TransactionDate = portugalTime,
            PaymentType = PTMappings.ExtractDocumentTypeAndUniqueIdentification(receipt.receiptResponse.ftReceiptIdentification).documentType,
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

        if (workDocument.PaymentType == "RG" && receiptRequest.cbPreviousReceiptReference != null)
        {
            var referencedReceiptReference = receipt.receiptResponse.GetRequiredPreviousReceiptReference().First().Response;
            workDocument.Line[0].SourceDocumentID = new SourceDocument
            {
                OriginatingON = referencedReceiptReference!.ftReceiptIdentification.Split("#").Last(),
                InvoiceDate = referencedReceiptReference.ftReceiptMoment
            };
        }

        return workDocument;
    }

    public async Task<Invoice?> GetInvoiceForReceiptRequest((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        try
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
            var hashSignature = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypePT.Hash));
            var atcudSignature = receipt.receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypePT.ATCUD))!;
            if (atcudSignature != null)
            {
                atcudSignature.Data = atcudSignature.Data.Replace("ATCUD: ", "");
            }
            var netAmount = grossAmount - taxable;
            var customer = GetCustomerData(receiptRequest);

            // Convert UTC time to Portugal local time
            var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receiptResponse.ftReceiptMoment);
            var invoice = new Invoice
            {
                InvoiceNo = receipt.receiptResponse.ftReceiptIdentification.Split("#").Last(),
                ATCUD = atcudSignature?.Data ?? "",
                DocumentStatus = await GetInvoiceDocumentStatusForDocument(receipt),
                Hash = hashSignature?.Data ?? "",
                HashControl = "1",
                Period = portugalTime.Month,
                InvoiceDate = portugalTime,
                InvoiceType = PTMappings.ExtractDocumentTypeAndUniqueIdentification(receipt.receiptResponse.ftReceiptIdentification).documentType,
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
                invoice.HashControl = invoice.HashControl + "-" + PTMappings.ExtractDocumentTypeAndUniqueIdentification(receiptResponse.ftReceiptIdentification).documentType + "M " + data.PT.Series + "/" + data.PT.Number!.ToString()!.PadLeft(4, '0');
            }
            //if (lines.Any(x => x.SettlementAmount.HasValue))
            //{
            //    invoice.DocumentTotals.Settlement = new Settlement
            //    {
            //        SettlementAmount = lines.Sum(x => x.SettlementAmount ?? 0)
            //    };
            //}
            invoice.DocumentTotals.Payment = receiptRequest.cbPayItems.Where(x => !x.ftPayItemCase.IsCase(PayItemCase.AccountsReceivable)).Select(x => GetPayment(receiptResponse, x)).ToList();
            return invoice;
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while generating the invoice for the receipt request.", ex);
        }
    }

    private static string GetSourcePayment(ReceiptRequest receiptRequest) => receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) ? "M" : "P";

    private static string GetSourceBilling(ReceiptRequest receiptRequest) => receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) ? "M" : "P";

    public Payment GetPayment(ReceiptResponse receiptResponse, PayItem payItem)
    {
        return new Payment
        {
            PaymentAmount = Math.Abs(payItem.Amount),
            PaymentDate = PortugalTimeHelper.ConvertToPortugalTime(receiptResponse.ftReceiptMoment),
            PaymentMechanism = PTMappings.GetPaymentMecahnism(payItem),
        };
    }

    public Line GetLine(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse, (ChargeItem chargeItem, List<ChargeItem> modifiers) chargeItem)
    {
        try
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
                TaxPointDate = chargeItemData.Moment ?? receiptResponse.ftReceiptMoment,
                Description = chargeItemData.Description,
                Tax = tax
            };

            if (PTMappings.ExtractDocumentTypeAndUniqueIdentification(receiptResponse.ftReceiptIdentification).documentType == "NC")
            {
                line.DebitAmount = Helpers.CreateMonetaryValue(netLinePrice);
            }
            else
            {
                line.CreditAmount = Helpers.CreateMonetaryValue(netLinePrice);
            }

            if (receiptRequest.cbPreviousReceiptReference != null)
            {
                if (PTMappings.ExtractDocumentTypeAndUniqueIdentification(receiptResponse.ftReceiptIdentification).documentType == "NC")
                {
                    var referencedReceiptResponse = receiptResponse.GetRequiredPreviousReceiptReference().First().Response;
                    line.References = new References
                    {
                        Reference = referencedReceiptResponse!.ftReceiptIdentification.Split("#").Last(),
                        Reason = "Devolução"
                    };
                }
                else
                {
                    var referencedReceiptResponse = receiptResponse.GetRequiredPreviousReceiptReference().First();
                    var matchingChargeItem = referencedReceiptResponse.Request.cbChargeItems.FirstOrDefault(x => GenerateUniqueProductIdentifier(x) == line.ProductCode);
                    if (matchingChargeItem != null)
                    {
                        line.OrderReferences = new OrderReferences
                        {
                            OriginatingON = referencedReceiptResponse!.Response.ftReceiptIdentification.Split("#").Last(),
                            OrderDate = referencedReceiptResponse!.Response.ftReceiptMoment
                        };
                    }
                }
            }

            if (chargeItem.modifiers.Count > 0)
            {
                line.SettlementAmount = netAmountModifiers;
            }
            if (((long) chargeItemData.ftChargeItemCase & 0xFF00) > 0x0000)
            {
                line.TaxExemptionReason = PTMappings.GetTaxExemptionReason(chargeItemData);
                line.TaxExemptionCode = PTMappings.GetTaxExemptionCode(chargeItemData);
            }
            return line;
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while generating the line for the receipt request.", ex);
        }
    }

    private async Task<InvoiceDocumentStatus> GetInvoiceDocumentStatusForDocument((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        // Supported States
        // N, S,A, R, F
        var documentStatus = await DocumentStatusProvider.GetDocumentStatusStateAsync(receipt);
        if (documentStatus.Status == DocumentStatus.Voided)
        {
            var voidProforma = documentStatus.SourceReceipt.Value;
            return new InvoiceDocumentStatus
            {
                InvoiceStatus = "A",
                InvoiceStatusDate = PortugalTimeHelper.ConvertToPortugalTime(voidProforma.receiptResponse.ftReceiptMoment),
                SourceID = GetSourceID(voidProforma.receiptRequest),
                SourceBilling = GetSourceBilling(voidProforma.receiptRequest),
                Reason = "Anulado"
            };
        }
        else if (documentStatus.Status == DocumentStatus.Invoiced)
        {
            var invoicedProforma = documentStatus.SourceReceipt.Value;
            return new InvoiceDocumentStatus
            {
                InvoiceStatus = "F",
                InvoiceStatusDate = PortugalTimeHelper.ConvertToPortugalTime(invoicedProforma.receiptResponse.ftReceiptMoment),
                SourceID = GetSourceID(invoicedProforma.receiptRequest),
                SourceBilling = GetSourceBilling(invoicedProforma.receiptRequest),
                Reason = "Faturado"
            };
        }
        else
        {
            return new InvoiceDocumentStatus
            {
                InvoiceStatus = "N",
                InvoiceStatusDate = PortugalTimeHelper.ConvertToPortugalTime(receipt.receiptResponse.ftReceiptMoment),
                SourceID = GetSourceID(receipt.receiptRequest),
                SourceBilling = GetSourceBilling(receipt.receiptRequest),
            };
        }
    }

    private async Task<WorkDocumentStatus> GetWorkStatusForDocument((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        // Supported States
        // N, A, F
        var workDocumentStatus = await DocumentStatusProvider.GetDocumentStatusStateAsync(receipt);
        if (workDocumentStatus.Status == DocumentStatus.Voided)
        {
            var voidProforma = workDocumentStatus.SourceReceipt.Value;
            return new WorkDocumentStatus
            {
                WorkStatus = "A",
                WorkStatusDate = PortugalTimeHelper.ConvertToPortugalTime(voidProforma.receiptResponse.ftReceiptMoment),
                SourceID = GetSourceID(voidProforma.receiptRequest),
                SourceBilling = GetSourceBilling(voidProforma.receiptRequest),
                Reason = "Anulado"
            };
        }
        else if (workDocumentStatus.Status == DocumentStatus.Invoiced)
        {
            var invoicedProforma = workDocumentStatus.SourceReceipt.Value;
            return new WorkDocumentStatus
            {
                WorkStatus = "F",
                WorkStatusDate = PortugalTimeHelper.ConvertToPortugalTime(invoicedProforma.receiptResponse.ftReceiptMoment),
                SourceID = GetSourceID(invoicedProforma.receiptRequest),
                SourceBilling = GetSourceBilling(invoicedProforma.receiptRequest),
                Reason = "Faturado"
            };
        }
        else
        {
            return new WorkDocumentStatus
            {
                WorkStatus = "N",
                WorkStatusDate = PortugalTimeHelper.ConvertToPortugalTime(receipt.receiptResponse.ftReceiptMoment),
                SourceID = GetSourceID(receipt.receiptRequest),
                SourceBilling = GetSourceBilling(receipt.receiptRequest),
            };
        }
    }

    private async Task<PaymentDocumentStatus> GetPaymentStatusForDocument((ReceiptRequest receiptRequest, ReceiptResponse receiptResponse) receipt)
    {
        // Supported States
        // A, N
        var documentStatus = await DocumentStatusProvider.GetDocumentStatusStateAsync(receipt);
        if (documentStatus.Status == DocumentStatus.Voided)
        {
            var voidProforma = documentStatus.SourceReceipt.Value;
            return new PaymentDocumentStatus
            {
                PaymentStatus = "A",
                PaymentStatusDate = PortugalTimeHelper.ConvertToPortugalTime(voidProforma.receiptResponse.ftReceiptMoment),
                SourceID = GetSourceID(voidProforma.receiptRequest),
                SourcePayment = GetSourcePayment(voidProforma.receiptRequest),
                Reason = "Anulado"
            };
        }
        else
        {
            return new PaymentDocumentStatus
            {
                PaymentStatus = "N",
                PaymentStatusDate = PortugalTimeHelper.ConvertToPortugalTime(receipt.receiptResponse.ftReceiptMoment),
                SourceID = GetSourceID(receipt.receiptRequest),
                SourcePayment = GetSourcePayment(receipt.receiptRequest),
            };
        }
    }
}