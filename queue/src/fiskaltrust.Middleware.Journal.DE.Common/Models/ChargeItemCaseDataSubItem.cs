using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.Common.Models
{
    public class ChargeItemCaseDataSubItem
    {
        public string ProductNumber { get; set; }
        public string GTIN { get; set; }
        public string Description { get; set; }
        public ProductGroup ProductGroup { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitQuantity { get; set; }
        public string Unit { get; set; }
        public long ftSubChargeItemCase { get; set; }
        public decimal Amount { get; set; }
    }
}