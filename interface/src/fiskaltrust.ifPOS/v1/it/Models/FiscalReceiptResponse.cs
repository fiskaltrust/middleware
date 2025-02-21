using fiskaltrust.ifPOS.v1.errors;
using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// Commercial Document Response File
    /// </summary>
    [DataContract]
    public class FiscalReceiptResponse
    {
        /// <summary>
        /// Indicating success
        /// </summary>
        [DataMember(Order = 10)]
        public bool Success { get; set; }

        /// <summary>
        /// Information on Error
        /// </summary>
        [DataMember(Order = 20)]
        public SSCDErrorInfo SSCDErrorInfo { get; set; }

        /// <summary>
        /// Document datetime
        /// </summary>
        [DataMember(Order = 40)]
        public DateTime ReceiptDateTime { get; set; }

        /// <summary>
        /// FiscalReceiptAmount – Document value. Zero in the case of an automatic void
        /// </summary>
        [DataMember(Order = 50)]
        public decimal Amount { get; set; }

        /// <summary>
        /// recNumber of the receipt.
        /// </summary>
        [DataMember(Order = 60)]
        public long ReceiptNumber { get; set; }

        /// <summary>
        /// zRecNumber of the receipt.
        /// </summary>
        [DataMember(Order = 70)]
        public long ZRepNumber { get; set; }

        /// <summary>
        /// Record data of the receipt.
        /// </summary>
        [DataMember(Order = 80)]
        public string ReceiptDataJson { get; set; }
    }
}
