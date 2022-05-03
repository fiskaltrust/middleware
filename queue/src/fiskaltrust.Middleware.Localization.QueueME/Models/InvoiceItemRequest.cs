using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class InvoiceItemRequest
    {       
        [DataMember]
        public decimal? DiscountPercentage { get; set; }
        [DataMember]
        public string ExemptFromVatReason { get; set; }
        [DataMember]
        public string[] VoucherSerialNumbers { get; set; }
        [DataMember]
        public string VoucherExpirationDate { get; set; }
    }
}
