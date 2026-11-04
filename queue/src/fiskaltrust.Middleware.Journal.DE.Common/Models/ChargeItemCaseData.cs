using System.Collections.Generic;

namespace fiskaltrust.Middleware.Journal.DE.Common.Models
{
    public class ChargeItemCaseData
    {
        public string VoucherNr { get; set; }
        public string TerminalId { get; set; }
        public string AgencyId { get; set; }
        public string ProductGroupId { get; set; }
        public List<ChargeItemCaseDataSubItem> SubItems { get; set; }
        public string VatDefinitionDescription { get; set; }
    }
}