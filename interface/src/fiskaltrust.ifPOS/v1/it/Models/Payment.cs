using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// When printing invoices based on the last commercial document, any 38-
    /// character descriptions are truncated to 37 characters
    /// </summary>
    [DataContract]
    public class Payment
    {
        /// <summary>
        ///Payment Description
        /// </summary>
        [DataMember(Order = 10)]
        public string Description { get; set; }

        /// <summary>
        ///Payment Amount
        /// </summary>
        [DataMember(Order = 20)]
        public decimal Amount { get; set; }

        /// <summary>
        ///PaymentType
        /// </summary>
        [DataMember(Order = 30)]
        public PaymentType PaymentType { get; set; }

        /// <summary>
        /// AdditionalInformation
        /// </summary>
        [DataMember(Order = 40)]
        public string AdditionalInformation { get; set; }
    }
}
