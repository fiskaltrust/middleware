using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// When printing invoices based on the last commercial document, any 38-
    /// character descriptions are truncated to 37 characters
    /// </summary>
    [DataContract]
    public class PaymentAdjustment
    {
        /// <summary>
        /// When printing invoices based on the last commercial document, any 38-
        /// character descriptions are truncated to 37 characters
        /// </summary>
        [DataMember(Order = 10)]
        public string Description { get; set; }

        /// <summary>
        ///  A zero amount will throw a printer error 16.
        /// </summary>
        [DataMember(Order = 20)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Department ID number (range 1 to 99), VAT Group
        /// </summary>
        [DataMember(Order = 30)]
        public int? VatGroup { get; set; }

        /// <summary>
        /// PaymentAdjustmentType
        /// </summary>
        [DataMember(Order = 40)]
        public PaymentAdjustmentType PaymentAdjustmentType { get; set; }

        /// <summary>
        /// AdditionalInformation
        /// </summary>
        [DataMember(Order = 50)]
        public string AdditionalInformation { get; set; }

    }
}
