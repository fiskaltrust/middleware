using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.v2;
using fiskaltrust.Middleware.Storage;
using Org.BouncyCastle.Asn1.Ocsp;

namespace fiskaltrust.Middleware.Localization.QueuePT.Factories;

public static class InvoiceType
{
    public const string Invoice = "FT";
    public const string SimplifiedInvoice = "FS";
    public const string Receipt = "FR";
    public const string DebitNote = "ND";
    public const string CreditNote = "NC";
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
    public static string CreateSimplifiedInvoiceQRCodeAnonymousCustomer(string hash, ftQueuePT queuePT, ftSignaturCreationUnitPT signaturCreationUnitPT, ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var taxGroups = request.cbChargeItems.GroupBy(GetIVATAxCode);
        var normalChargeItems = request.cbChargeItems.Where(x => GetIVATAxCode(x) == "NOR").ToList();
        var reducedChargeItems = request.cbChargeItems.Where(x => GetIVATAxCode(x) == "RED").ToList();
        var intermediateChargeItems = request.cbChargeItems.Where(x => GetIVATAxCode(x) == "INT").ToList();
        var exemptChargeItems = request.cbChargeItems.Where(x => GetIVATAxCode(x) == "ISE").ToList();

        return new PTQrCode
        {
            IssuerTIN = queuePT.IssuerTIN,
            CustomerTIN = PTQrCode.CUSTOMER_TIN_ANONYMOUS,
            CustomerCountry = PTQrCode.CUSTOMER_COUNTRY_ANONYMOUS,
            DocumentType = InvoiceType.SimplifiedInvoice,
            DocumentStatus = InvoiceStatus.Normal,
            DocumentDate = request.cbReceiptMoment,
            UniqueIdentificationOfTheDocument = receiptResponse.ftReceiptIdentification, 
            ATCUD = queuePT.ATCUD,
            TaxCountryRegion = queuePT.TaxRegion,
            TaxableBasisOfVAT_ExemptRate = exemptChargeItems.Sum(x => x.Amount),
            TaxableBasisOfVAT_ReducedRate = reducedChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_ReducedRate = reducedChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TaxableBasisOfVAT_IntermediateRate = intermediateChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_IntermediateRate = intermediateChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TaxableBasisOfVAT_StandardRate = normalChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
            TotalVAT_StandardRate = normalChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            TotalTaxes = request.cbChargeItems.Sum(x => x.VATAmount ?? 0.0m),
            GrossTotal = request.cbChargeItems.Sum(x => x.Amount),
            Hash = hash[..4],
            SoftwareCertificateNumber = signaturCreationUnitPT.SoftwareCertificateNumber,
            OtherInformation = "ftQueueId=" + receiptResponse.ftQueueID + ";ftQueueItemId=" + receiptResponse.ftQueueItemID
        }.GenerateQRCode();
    }

    public static string GetIVATAxCode(ChargeItem chargeItem) => (chargeItem.ftChargeItemCase & 0xF) switch
    {
        0x0 => "",
        0x1 => "RED",
        0x2 => "",
        0x3 => "NOR",
        0x4 => "",
        0x5 => "",
        0x6 => "INT",
        0x7 => "",
        0x8 => "ISE",
        _ => ""
    };
}
