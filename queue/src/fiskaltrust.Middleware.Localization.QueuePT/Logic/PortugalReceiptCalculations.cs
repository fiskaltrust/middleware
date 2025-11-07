using System.Reflection.Metadata;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.ifPOS.v2.Cases;
using Org.BouncyCastle.Asn1.Ocsp;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public static class InvoiceType
{
    public const string Invoice = "FT";
    public const string SimplifiedInvoice = "FS";
    public const string Receipt = "FR";
    public const string Payment = "RG";
    public const string DebitNote = "ND";
    public const string CreditNote = "NC";
    public const string ProForma = "PF";
}

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
    public static string CreateCreditNoteQRCode(string qrCodeHash, string issuerTIN, string atcud, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var taxGroups = request.cbChargeItems.GroupBy(PTMappings.GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "ISE").ToList();

        var customer = new SaftExporter().GetCustomerData(request);
        var customerTIN = customer.CustomerTaxID;
        var customerCountry = customer.BillingAddress.Country;
        return new PTQrCode
        {
            IssuerTIN = issuerTIN,
            CustomerTIN = customerTIN,
            CustomerCountry = customerCountry,
            DocumentType = InvoiceType.CreditNote,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = request.cbReceiptMoment,
            UniqueIdentificationOfTheDocument = receiptResponse.ftReceiptIdentification.Split("#").Last(),
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
            SoftwareCertificateNumber = CertificationPosSystem.SoftwareCertificateNumber,
            //OtherInformation = "ftQueueId=" + receiptResponse.ftQueueID + ";ftQueueItemId=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }

    public static string CreateInvoiceQRCode(string qrCodeHash, string issuerTIN, string atcud, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var taxGroups = request.cbChargeItems.GroupBy(PTMappings.GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "ISE").ToList();

        var customer = new SaftExporter().GetCustomerData(request);
        var customerTIN = customer.CustomerTaxID;
        var customerCountry = customer.BillingAddress.Country;
        return new PTQrCode
        {
            IssuerTIN = issuerTIN,
            CustomerTIN = customerTIN,
            CustomerCountry = customerCountry,
            DocumentType = InvoiceType.Invoice,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = request.cbReceiptMoment,
            UniqueIdentificationOfTheDocument = receiptResponse.ftReceiptIdentification.Split("#").Last(),
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
            SoftwareCertificateNumber = CertificationPosSystem.SoftwareCertificateNumber,
            //OtherInformation = "ftQueueId=" + receiptResponse.ftQueueID + ";ftQueueItemId=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }

    public static string CreateProFormaQRCode(string qrCodeHash, string issuerTIN, string atcud, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var taxGroups = request.cbChargeItems.GroupBy(PTMappings.GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "ISE").ToList();

        var customer = new SaftExporter().GetCustomerData(request);
        var customerTIN = customer.CustomerTaxID;
        var customerCountry = customer.BillingAddress.Country;
        return new PTQrCode
        {
            IssuerTIN = issuerTIN,
            CustomerTIN = customerTIN,
            CustomerCountry = customerCountry,
            DocumentType = InvoiceType.ProForma,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = request.cbReceiptMoment,
            UniqueIdentificationOfTheDocument = receiptResponse.ftReceiptIdentification.Split("#").Last(),
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
            SoftwareCertificateNumber = CertificationPosSystem.SoftwareCertificateNumber,
            OtherInformation = "ftQueueId=" + receiptResponse.ftQueueID + ";ftQueueItemId=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }

    public static string CreateRGQRCode(string qrCodeHash, string issuerTIN, string atcud, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var taxGroups = request.cbChargeItems.GroupBy(PTMappings.GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "ISE").ToList();

        var customer = new SaftExporter().GetCustomerData(request);
        var customerTIN = customer.CustomerTaxID;
        var customerCountry = customer.BillingAddress.Country;
        return new PTQrCode
        {
            IssuerTIN = issuerTIN,
            CustomerTIN = customerTIN,
            CustomerCountry = customerCountry,
            DocumentType = InvoiceType.Payment,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = request.cbReceiptMoment,
            UniqueIdentificationOfTheDocument = receiptResponse.ftReceiptIdentification.Split("#").Last(),
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
            SoftwareCertificateNumber = CertificationPosSystem.SoftwareCertificateNumber,
            OtherInformation = "ftQueueId=" + receiptResponse.ftQueueID + ";ftQueueItemId=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }

    public static string CreateSimplifiedInvoiceQRCode(string qrCodeHash, string issuerTIN, string atcud, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var taxGroups = request.cbChargeItems.GroupBy(PTMappings.GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => PTMappings.GetIVATAxCode(x) == "ISE").ToList();

        var customer = new SaftExporter().GetCustomerData(request);
        var customerTIN = customer.CustomerTaxID;
        var customerCountry = customer.BillingAddress.Country;
        return new PTQrCode
        {
            IssuerTIN = issuerTIN,
            CustomerTIN = customerTIN,
            CustomerCountry = customerCountry,
            DocumentType = InvoiceType.SimplifiedInvoice,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = request.cbReceiptMoment,
            UniqueIdentificationOfTheDocument = receiptResponse.ftReceiptIdentification.Split("#").Last(),
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
            SoftwareCertificateNumber = CertificationPosSystem.SoftwareCertificateNumber,
            OtherInformation = "ftQueueId=" + receiptResponse.ftQueueID + ";ftQueueItemId=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }
}
