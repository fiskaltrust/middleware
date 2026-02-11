using System.Reflection.Metadata;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using Org.BouncyCastle.Asn1.Ocsp;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public static class InvoiceStatus
{
    public const string Normal = "N";
    public const string Cancelled = "A";
    public const string SelfBilling = "S";
    public const string SummaryDocumentForOtherDocuments = "R";
    public const string InvoicedDocument = "F";
}

public static class PortugalReceiptCalculations
{
    public static string GetPrintHash(string hash) => new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();

    public static string CreateCreditNoteQRCode(string qrCodeHash, string issuerTIN, NumberSeries numberSeries, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var atcud = numberSeries.ATCUD + "-" + numberSeries.Numerator;
        var taxGroups = request.cbChargeItems.GroupBy(PTMappings.GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "ISE").ToList();

        var customer = new SaftExporter().GetCustomerData(request);
        var customerTIN = customer.CustomerTaxID;
        var customerCountry = string.IsNullOrWhiteSpace(customer.BillingAddress.Country)
            ? "Desconhecido"
            : customer.BillingAddress.Country;
        
        var (extractedDocumentType, uniqueIdentification) = PTMappings.ExtractDocumentTypeAndUniqueIdentification(receiptResponse.ftReceiptIdentification);

        // Convert UTC time to Portugal local time
        var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receiptResponse.ftReceiptMoment);
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            portugalTime = PortugalTimeHelper.ConvertToPortugalTime(request.cbReceiptMoment);
        }

        return new PTQrCode
        {
            IssuerTIN = issuerTIN,
            CustomerTIN = customerTIN,
            CustomerCountry = customerCountry,
            DocumentType = extractedDocumentType,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = portugalTime,
            UniqueIdentificationOfTheDocument = uniqueIdentification,
            ATCUD = atcud,
            TaxCountryRegion = "PT",
            TaxableBasisOfVAT_ExemptRate = exemptChargeItems.Sum(x => Math.Abs(x.Amount)),
            TaxableBasisOfVAT_ReducedRate = reducedChargeItems.Sum(x => Math.Abs(x.Amount) - Math.Abs(x.VATAmount ?? 0.0m)),
            TotalVAT_ReducedRate = reducedChargeItems.Sum(x => Math.Abs(x.VATAmount ?? 0.0m)),
            TaxableBasisOfVAT_IntermediateRate = intermediateChargeItems.Sum(x => Math.Abs(x.Amount) - Math.Abs(x.VATAmount ?? 0.0m)),
            TotalVAT_IntermediateRate = intermediateChargeItems.Sum(x => Math.Abs(x.VATAmount ?? 0.0m)),
            TaxableBasisOfVAT_StandardRate = normalChargeItems.Sum(x => Math.Abs(x.Amount) - Math.Abs(x.VATAmount ?? 0.0m)),
            TotalVAT_StandardRate = normalChargeItems.Sum(x => Math.Abs(x.VATAmount ?? 0.0m)),
            TotalTaxes = request.cbChargeItems.Sum(x => Math.Abs(x.VATAmount ?? 0.0m)),
            GrossTotal = request.cbChargeItems.Sum(x => Math.Abs(x.Amount)),
            Hash = qrCodeHash,
            SoftwareCertificateNumber = PTMappings.CertificationPosSystem.SoftwareCertificateNumber,
            OtherInformation = "qiid=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }

    public static string CreateVatFreeQRCode(string qrCodeHash, string issuerTIN, NumberSeries numberSeries, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var atcud = numberSeries.ATCUD + "-" + numberSeries.Numerator;
        var taxGroups = request.cbChargeItems.GroupBy(PTMappings.GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "ISE").ToList();

        var customer = new SaftExporter().GetCustomerData(request);
        var customerTIN = customer.CustomerTaxID;
        var customerCountry = string.IsNullOrWhiteSpace(customer.BillingAddress.Country)
            ? "Desconhecido"
            : customer.BillingAddress.Country;

        var (extractedDocumentType, uniqueIdentification) = PTMappings.ExtractDocumentTypeAndUniqueIdentification(receiptResponse.ftReceiptIdentification);

        // Convert UTC time to Portugal local time
        var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receiptResponse.ftReceiptMoment);
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            portugalTime = PortugalTimeHelper.ConvertToPortugalTime(request.cbReceiptMoment);
        }

        return new PTQrCode
        {
            IssuerTIN = issuerTIN,
            CustomerTIN = customerTIN,
            CustomerCountry = customerCountry,
            DocumentType = extractedDocumentType,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = portugalTime,
            UniqueIdentificationOfTheDocument = uniqueIdentification,
            ATCUD = atcud,
            // Fill according to the technical notes of the TaxCountryRegion field of SAF-T (PT).In case of a document without an indication of the VAT rate, which must be shown in table 4.2, 4.3 or 4.4 of the SAF - T(PT), fill in with «0» (I1: 0).
            TaxCountryRegion = "0",
            TaxableBasisOfVAT_ExemptRate = exemptChargeItems.Sum(x => x.Amount),
            TaxableBasisOfVAT_ReducedRate = reducedChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_ReducedRate = reducedChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TaxableBasisOfVAT_IntermediateRate = intermediateChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_IntermediateRate = intermediateChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TaxableBasisOfVAT_StandardRate = normalChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_StandardRate = normalChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TotalTaxes = request.cbChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            GrossTotal = request.cbChargeItems.Sum(x => x.Amount),
            Hash = qrCodeHash,
            SoftwareCertificateNumber = PTMappings.CertificationPosSystem.SoftwareCertificateNumber,
            OtherInformation = "qiid=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }

    public static string CreateQRCode(string qrCodeHash, string issuerTIN, NumberSeries numberSeries, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var atcud = numberSeries.ATCUD + "-" + numberSeries.Numerator;
        var taxGroups = request.cbChargeItems.GroupBy(PTMappings.GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "ISE").ToList();

        var customer = new SaftExporter().GetCustomerData(request);
        var customerTIN = customer.CustomerTaxID;
        var customerCountry = customer.BillingAddress.Country;

        var (extractedDocumentType, uniqueIdentification) = PTMappings.ExtractDocumentTypeAndUniqueIdentification(receiptResponse.ftReceiptIdentification);

        // Convert UTC time to Portugal local time
        var portugalTime = PortugalTimeHelper.ConvertToPortugalTime(receiptResponse.ftReceiptMoment);
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            portugalTime = PortugalTimeHelper.ConvertToPortugalTime(request.cbReceiptMoment);
        }

        return new PTQrCode
        {
            IssuerTIN = issuerTIN,
            CustomerTIN = customerTIN,
            CustomerCountry = customerCountry,
            DocumentType = extractedDocumentType,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = portugalTime,
            UniqueIdentificationOfTheDocument = uniqueIdentification,
            ATCUD = atcud,
            TaxCountryRegion = "PT",
            TaxableBasisOfVAT_ExemptRate = exemptChargeItems.Sum(x => x.Amount),
            TaxableBasisOfVAT_ReducedRate = reducedChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_ReducedRate = reducedChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TaxableBasisOfVAT_IntermediateRate = intermediateChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_IntermediateRate = intermediateChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TaxableBasisOfVAT_StandardRate = normalChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_StandardRate = normalChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TotalTaxes = request.cbChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            GrossTotal = request.cbChargeItems.Sum(x => x.Amount),
            Hash = qrCodeHash,
            SoftwareCertificateNumber = PTMappings.CertificationPosSystem.SoftwareCertificateNumber,
            OtherInformation = "qiid=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }
}
