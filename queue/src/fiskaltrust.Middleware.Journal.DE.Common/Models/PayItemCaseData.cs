namespace fiskaltrust.Middleware.Journal.DE.Common.Models
{
    public class PayItemCaseData
    {
        public string VoucherNr { get; set; }
        public string CurrencyCode { get; set; }
        public string ItemCaseName { get; set; }
        public decimal? ForeignCurrencyAmount { get; set; }
        public decimal? BaseCurrencyAmount { get; set; }
        public string AgencyId { get; set; }
        public string MoneyGroupId { get; set; }
    }
}