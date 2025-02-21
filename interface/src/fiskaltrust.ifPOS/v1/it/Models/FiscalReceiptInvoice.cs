using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    ///printerFiscalReceipt
    ///Emission of commercial documents (documenti commerciali)
    /// </summary>
    [DataContract]
    public class FiscalReceiptInvoice
    {

        /// <summary>
        /// LotteryID
        /// </summary>
        [DataMember(Order = 10)]
        public string LotteryID { get; set; }

        /// <summary>
        /// Operator
        /// </summary>
        [DataMember(Order = 20)]
        public string Operator { get; set; }

        /// <summary>
        /// Sends text messages to the customer display. You cannot insert carriage returns or line feeds so use 
        /// spaces to pad out line 1 and begin line 2. This sub-element has two attributes; one to indicate the
        /// operator and one for the text itself.The maximum number of characters is 40. This reduces to 20 if 
        /// used with printerTicket files.
        /// </summary>
        [DataMember(Order = 30)]
        public string DisplayText { get; set; }

        /// <summary>
        ///Barcodes codes are printed at the end of the commercial document after the additional trailer
        ///lines but before the FOOTER. Only one barcode can be printed in a commercial document unless the
        ///paper cut native command is used(1-137).
        /// </summary>
        [DataMember(Order = 40)]
        public string Barcode { get; set; }

        /// <summary>
        ///QRcodes codes are printed at the end of the commercial document after the additional trailer
        ///lines but before the FOOTER.
        /// </summary>
        [DataMember(Order = 50)]
        public string QRcode { get; set; }

        /// <summary>
        /// printRecSubtotalAdjustment: Prints discount or surcharge applied on the subtotal.
        /// </summary>
        [DataMember(Order = 60)]
        public List<PaymentAdjustment> PaymentAdjustments { get; set; }

        /// <summary>
        /// printRecTotal: One or more commands can be sent; more than one means that the payment is composed of several 
        /// partial payments.In this case, once the total has been reached or exceeded, no more payment
        /// commands can be sent
        /// </summary>
        [DataMember(Order = 70)]
        public List<Payment> Payments { get; set; }

        /// <summary>
        /// printRecItems: Prints sale items on a commercial sale document.
        /// </summary>
        [DataMember(Order = 80)]
        public List<Item> Items { get; set; }
    }
}
