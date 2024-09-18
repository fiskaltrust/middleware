using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Models;

namespace fiskaltrust.Middleware.Localization.QueuePT.Factories
{
    public static class PortugalReceiptCalculations
    {
        public static string GetQRCodeFromReceipt(ReceiptRequest request, string hash)
        {
            var taxGroups = request.cbChargeItems.GroupBy(GetIVATAxCode);

            var normalChargeItems = request.cbChargeItems.Where(x => GetIVATAxCode(x) == "NOR").ToList();
            var reducedChargeItems = request.cbChargeItems.Where(x => GetIVATAxCode(x) == "RED").ToList();
            var intermediateChargeItems = request.cbChargeItems.Where(x => GetIVATAxCode(x) == "INT").ToList();
            var exemptChargeItems = request.cbChargeItems.Where(x => GetIVATAxCode(x) == "ISE").ToList();


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
                TaxableBasisOfVAT_ExemptRate = exemptChargeItems.Sum(x => x.VATAmount ?? 0.0m),
                TaxableBasisOfVAT_ReducedRate = reducedChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
                TotalVAT_ReducedRate = reducedChargeItems.Sum(x => x.VATAmount ?? 0.0m),
                TaxableBasisOfVAT_IntermediateRate = intermediateChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
                TotalVAT_IntermediateRate = intermediateChargeItems.Sum(x => x.VATAmount ?? 0.0m),
                TaxableBasisOfVAT_StandardRate = normalChargeItems.Sum(x => x.Amount - x.VATAmount ?? 0.0m),
                TotalVAT_StandardRate = normalChargeItems.Sum(x => x.VATAmount ?? 0.0m),
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
