using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// Refunds (Goods return or reso in Italy are not supported in 
    /// invoices.They are converted to corrections(storni).
    /// Modifiers are not supported in invoices
    /// </summary>
    [DataContract]
    public class Refund
    {
        /// <summary>
        /// Department ID number (range 1 to 99), VAT Group
        /// </summary>
        [DataMember(Order = 10)]
        public int VatGroup { get; set; }

        /// <summary>
        /// When printing invoices based on the last commercial document, any 38-
        /// character descriptions are truncated to 37 characters
        /// </summary>
        [DataMember(Order = 20)]
        public string Description { get; set; }

        /// <summary>
        /// Epson fiscal printers can compute quantities from 0.001 up to 9999.999
        /// </summary>
        [DataMember(Order = 30)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Epson fiscal printers can accept prices from 0.00 up to 9999999.99. The 
        /// FpMate CGI service automatically rounds down amounts with more than
        ///two decimal places.If it exceeds 9999999.99, an error is returned.Either
        ///a comma or a full stop (period) can represent the decimal point. Thousand
        ///separators should not be used.
        ///The unitPrice and payment attributes can be zero.
        /// </summary>
        [DataMember(Order = 40)]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Epson fiscal printers can accept prices from 0.00 up to 9999999.99. The 
        /// FpMate CGI service automatically rounds down amounts with more than
        ///two decimal places.If it exceeds 9999999.99, an error is returned.Either
        ///a comma or a full stop (period) can represent the decimal point. Thousand
        ///separators should not be used.
        ///The amount attribute cannot be zero!
        /// </summary>
        [DataMember(Order = 50)]
        public decimal Amount { get; set; }
    }
}
