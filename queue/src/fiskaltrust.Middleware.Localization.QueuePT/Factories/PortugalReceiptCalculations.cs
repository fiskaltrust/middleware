using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Models;

namespace fiskaltrust.Middleware.Localization.QueuePT.Factories
{
    public static class PortugalReceiptCalculations
    {
        public static string GetQRCodeFromReceipt(ReceiptRequest request, string hash)
        {
            var taxGroups = request.cbChargeItems.GroupBy(GetIVATAxCode);

            return new PTQrCode
            {
                IssuerTIN = "123456789",
                CustomerTIN = "999999990",
                CustomerCountry = "PT",
                DocumentType = "FS",
                DocumentStatus = "N",
                DocumentDate = request.cbReceiptMoment,
                UniqueIdentificationOfTheDocument = request.cbReceiptReference,
                ATCUD = "0",
                TaxCountryRegion = "PT",
                TaxableBasisOfVAT_ExemptRate = taxGroups.Where(x => x.Key == "ISE").ToList().SelectMany(x => x).Sum(x => x.VATAmount ?? 0.0m),
                TaxableBasisOfVAT_ReducedRate = taxGroups.Where(x => x.Key == "RED").ToList().SelectMany(x => x).Sum(x => x.Amount - x.VATAmount ?? 0.0m),
                TotalVAT_ReducedRate = taxGroups.Where(x => x.Key == "RED").ToList().SelectMany(x => x).Sum(x => x.VATAmount ?? 0.0m),
                TaxableBasisOfVAT_IntermediateRate = taxGroups.Where(x => x.Key == "INT").ToList().SelectMany(x => x).Sum(x => x.Amount - x.VATAmount ?? 0.0m),
                TotalVAT_IntermediateRate = taxGroups.Where(x => x.Key == "INT").ToList().SelectMany(x => x).Sum(x => x.VATAmount ?? 0.0m),
                TaxableBasisOfVAT_StandardRate = taxGroups.Where(x => x.Key == "NOR").ToList().SelectMany(x => x).Sum(x => x.Amount - x.VATAmount ?? 0.0m),
                TotalVAT_StandardRate = taxGroups.Where(x => x.Key == "NOR").ToList().SelectMany(x => x).Sum(x => x.VATAmount ?? 0.0m),
                TotalTaxes = request.cbChargeItems.Sum(x => x.VATAmount ?? 0.0m),
                GrossTotal = request.cbChargeItems.Sum(x => x.Amount),
                Hash = hash[..4],
                SoftwareCertificateNumber = ""
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
}
