using System;

namespace fiskaltrust.storage.V0
{
    /// <summary>
    /// Localized fields for queue (=receiptchain)
    /// Used signing mechanism: SHA-256withECDSA using curve secp256r1, find more informations at https://www.ietf.org/rfc/rfc5480.txt
    /// Defined by secp256r1 the private key length is 256 bit.
    /// <list type="bullet">
    /// <listheader>  
    /// <term>Receiptchains</term>  
    /// <description>Multiple receiptchains are used in France</description>  
    /// </listheader>          
    /// <item>
    /// <term>Ticket</term>
    /// <description>Signed and chained, national upcounting numbering starts with "T"</description>
    /// </item>
    /// <item>
    /// <term>Payment Proove</term>
    /// <description>Signed and chained, national upcounting numbering starts with "P"</description>
    /// </item>
    /// <item>
    /// <term>Invoice</term>
    /// <description>Signed and chained, national upcounting numbering starts with "I"</description>
    /// </item>
    /// <item>
    /// <term>Grand Total / Zero-Receipt / Function-Receipt </term>
    /// <description>Signed and chained, national upcounting numbering starts with "G"</description>
    /// </item>
    /// <item>
    /// <term>Bill</term>
    /// <description>Signed and chained, national upcounting numbering starts with "B"</description>
    /// </item>
    /// <item>
    /// <term>Technical Event Log / Accounting / Audit</term>
    /// <description>Signed and chained, national upcounting numbering starts with "L"</description>
    /// </item>
    /// <item>
    /// <term>Archiv</term>
    /// <description>Signed and chained, national upcounting numbering starts with "A"</description>
    /// </item>
    /// <item>
    /// <term>Training</term>
    /// <description>Signed and chained, national upcounting numbering starts with "X"</description>
    /// </item>
    /// <item>
    /// <term>Copy of any signed receipt-type</term>
    /// <description>Signed and chained, national upcounting numbering starts with "C"</description>
    /// </item>
    /// </list> 
    /// <list type="bullet">
    /// <listheader>  
    /// <term>VAT rates</term>  
    /// <description>VAT rates according eu: https://ec.europa.eu/taxation_customs/business/vat/eu-vat-rules-topic/vat-rates_en </description>  
    /// </listheader>          
    /// <item>
    /// <term>Normal</term>
    /// <description>by 01.01.2018: 20%</description>
    /// </item>
    /// <item>
    /// <term>Reduced1</term>
    /// <description>by 01.01.2018: 5.5%</description>
    /// </item>
    /// <item>
    /// <term>Reduced2</term>
    /// <description>by 01.01.2018: 10%</description>
    /// </item>
    /// <item>
    /// <term>ReducedSuper</term>
    /// <description>by 01.01.2018: 2.1%</description>
    /// </item>
    /// <item>
    /// <term>Zero</term>
    /// <description>in special conditions, like reverse charge</description>
    /// </item>
    /// <item>
    /// <term>Unknown</term>
    /// <description>in special conditions, like mixed or differntial</description>
    /// </item>
    /// </list> 
    /// </summary>
    public partial class ftQueueFR : QueueLocalization
    {
        /// <summary>
        /// PrimaryKey to ftQueueFR and ForeignKey to Queue
        /// </summary>
        public Guid ftQueueFRId { get; set; }

        /// <summary>
        /// ForeignKey to the french SignatureCreationUnit
        /// </summary>
        public Guid ftSignaturCreationUnitFRId { get; set; }

        #region Identification

        /// <summary>
        /// french company identification
        /// </summary>
        public string Siret { get; set; }

        /// <summary>
        /// Unique identification of receiptchain by account, is also used in ReceiptResponse
        /// TBD: format, maybe related to private-key or upcounting readable
        /// </summary>
        public string CashBoxIdentification { get; set; }

        #endregion

        #region Ticket

        /// <summary>
        /// Ticket numerator
        /// is raied by 1 for each ticket. used for national numbering prefixed by T
        /// </summary>
        public long TNumerator { get; set; }

        /// <summary>
        /// Ticket totalizer
        /// </summary>
        public decimal TTotalizer { get; set; }

        /// <summary>
        /// Ticket: The total cost of items of „undefined type of service for FR normal“ (calculated with 20%)
        /// </summary>
        public decimal TCITotalNormal { get; set; }

        /// <summary>
        /// Ticket: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal TCITotalReduced1 { get; set; }

        /// <summary>
        /// Ticket: The total cost of items of „undefined type of service for FR reduced-2“ (calculated with 10%)
        /// </summary>
        public decimal TCITotalReduced2 { get; set; }

        /// <summary>
        /// Ticket: The total cost of items of „undefined type of service for FR special“ (super-reduced) with rates that are not contained in the previous ones (this can be for example 2,1%)
        /// </summary>
        public decimal TCITotalReducedS { get; set; }

        /// <summary>
        /// Ticket: The total cost of items of „undefined type of service for FR zero“ with data which are indicated with 0% sales tax and data where the sales tax is unknown
        /// </summary>
        public decimal TCITotalZero { get; set; }

        /// <summary>
        /// Ticket: The total cost of items not considered before
        /// </summary>
        public decimal TCITotalUnknown { get; set; }

        /// <summary>
        /// Ticket: The total amount of all payment types: cash, credit card, voucher
        /// </summary>
        public decimal TPITotalCash { get; set; }

        /// <summary>
        /// Ticket: The total amount of all payment types: wire-transfer, debit card, paypal
        /// </summary>
        public decimal TPITotalNonCash { get; set; }

        /// <summary>
        /// Ticket: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal TPITotalInternal { get; set; }

        /// <summary>
        /// Ticket: The total amount of payment not listed before
        /// </summary>
        public decimal TPITotalUnknown { get; set; }

        /// <summary>
        /// Ticket hash
        /// calculated only for Ticket receipts
        /// </summary>
        public string TLastHash { get; set; }

        #endregion

        #region Payment Prove

        /// <summary>
        /// Payment Prove numerator
        /// is raied by 1 for each payment proove. used for national numbering prefixed by P
        /// no VAT relation, because only payment. Totalizer is raised by payitems excluding payitemtype 0x4652000000000011 
        /// </summary>
        public long PNumerator { get; set; }

        /// <summary>
        /// Payment Prove totalizer
        /// </summary>
        public decimal PTotalizer { get; set; }

        /// <summary>
        /// Payment Prove: The total amount of all payment types: cash, credit card, voucher
        /// </summary>
        public decimal PPITotalCash { get; set; }

        /// <summary>
        /// Payment Prove: The total amount of all payment types: wire-transfer, debit card, paypal
        /// </summary>
        public decimal PPITotalNonCash { get; set; }

        /// <summary>
        /// Payment Prove: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal PPITotalInternal { get; set; }

        /// <summary>
        /// Payment Prove: The total amount of payment not listed before
        /// </summary>
        public decimal PPITotalUnknown { get; set; }

        /// <summary>
        /// Payment Prove hash
        /// calculated only for Payment Prove receipts
        /// </summary>
        public string PLastHash { get; set; }

        #endregion

        #region Invoice

        /// <summary>
        /// Invoice numerator
        /// is raied by 1 for each invoice. used for national numbering prefixed by I
        /// </summary>
        public long INumerator { get; set; }

        /// <summary>
        /// Invoice totalizer
        /// </summary>
        public decimal ITotalizer { get; set; }

        /// <summary>
        /// Invoice: The total cost of items of „undefined type of service for FR normal“ (calculated with 20%)
        /// </summary>
        public decimal ICITotalNormal { get; set; }

        /// <summary>
        /// Invoice: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal ICITotalReduced1 { get; set; }

        /// <summary>
        /// Invoice: The total cost of items of „undefined type of service for FR reduced-2“ (calculated with 10%)
        /// </summary>
        public decimal ICITotalReduced2 { get; set; }

        /// <summary>
        /// Invoice: The total cost of items of „undefined type of service for FR special“ (super-reduced) with rates that are not contained in the previous ones (this can be for example 2,1%)
        /// </summary>
        public decimal ICITotalReducedS { get; set; }

        /// <summary>
        /// Invoice: The total cost of items of „undefined type of service for FR zero“ with data which are indicated with 0% sales tax and data where the sales tax is unknown
        /// </summary>
        public decimal ICITotalZero { get; set; }

        /// <summary>
        /// Invoice: The total cost of items not considered before
        /// </summary>
        public decimal ICITotalUnknown { get; set; }

        /// <summary>
        /// Invoice: The total amount of all payment types: cash, credit card, voucher
        /// </summary>
        public decimal IPITotalCash { get; set; }

        /// <summary>
        /// Invoice: The total amount of all payment types: wire-transfer, debit card, paypal
        /// </summary>
        public decimal IPITotalNonCash { get; set; }

        /// <summary>
        /// Invoice: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal IPITotalInternal { get; set; }

        /// <summary>
        /// Invoice: The total amount of payment not listed before
        /// </summary>
        public decimal IPITotalUnknown { get; set; }

        /// <summary>
        /// Invoice hash
        /// calculated only for Invoice receipts
        /// </summary>
        public string ILastHash { get; set; }

        #endregion

        #region Function-Receipt

        /// <summary>
        /// is raied by 1 for each Grand Total / Zero-Receipt / Function-Receipt. used for national numbering prefixed by G
        /// to Grand Total only items of type T and I are added 
        /// shift and day is added parallel
        /// day is raised by dayly closing
        /// year is raised by yearly closing
        /// </summary>
        public long GNumerator { get; set; }

        /// <summary>
        /// Grand Total hash
        /// calculated only for Grand Total receipts
        /// </summary>
        public string GLastHash { get; set; }

        /// <summary>
        /// Grand Total Shift totalizer
        /// resetted on each shift closing
        /// closing has to be added to journal
        /// </summary>
        public decimal GShiftTotalizer { get; set; }

        /// <summary>
        /// Grand Total Shift: The total cost of items of „undefined type of service for FR normal“ (calculated with 20%)
        /// </summary>
        public decimal GShiftCITotalNormal { get; set; }

        /// <summary>
        /// Grand Total Shift: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal GShiftCITotalReduced1 { get; set; }

        /// <summary>
        /// Grand Total Shift: The total cost of items of „undefined type of service for FR reduced-2“ (calculated with 10%)
        /// </summary>
        public decimal GShiftCITotalReduced2 { get; set; }

        /// <summary>
        /// Grand Total Shift: The total cost of items of „undefined type of service for FR special“ (super-reduced) with rates that are not contained in the previous ones (this can be for example 2,1%)
        /// </summary>
        public decimal GShiftCITotalReducedS { get; set; }

        /// <summary>
        /// Grand Total Shift: The total cost of items of „undefined type of service for FR zero“ with data which are indicated with 0% sales tax and data where the sales tax is unknown
        /// </summary>
        public decimal GShiftCITotalZero { get; set; }

        /// <summary>
        /// Grand Total Shift: The total cost of items not considered before
        /// </summary>
        public decimal GShiftCITotalUnknown { get; set; }

        /// <summary>
        /// Grand Total Shift: The total amount of all payment types: cash, credit card, voucher
        /// </summary>
        public decimal GShiftPITotalCash { get; set; }

        /// <summary>
        /// Grand Total Shift: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal GShiftPITotalNonCash { get; set; }

        /// <summary>
        /// Grand Total Shift: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal GShiftPITotalInternal { get; set; }

        /// <summary>
        /// Grand Total Shift: The total amount of payment not listed before
        /// </summary>
        public decimal GShiftPITotalUnknown { get; set; }

        /// <summary>
        /// Grand Total Shift: queue item date and time of the last Grand Total Shift receipt
        /// </summary>
        public DateTime? GLastShiftMoment { get; set; }

        /// <summary>
        /// Grand Total Shift: queue item id of the last Grand Total Shift receipt
        /// </summary>
        public Guid? GLastShiftQueueItemId { get; set; }

        /// <summary>
        /// Grand Total Daily totalizer
        /// resetted on each day closing
        /// closing has to be added to journal
        /// </summary>
        public decimal GDayTotalizer { get; set; }

        /// <summary>
        /// Grand Total Daily: The total cost of items of „undefined type of service for FR normal“ (calculated with 20%)
        /// </summary>
        public decimal GDayCITotalNormal { get; set; }

        /// <summary>
        /// Grand Total Daily: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal GDayCITotalReduced1 { get; set; }

        /// <summary>
        /// Grand Total Daily: The total cost of items of „undefined type of service for FR reduced-2“ (calculated with 10%)
        /// </summary>
        public decimal GDayCITotalReduced2 { get; set; }

        /// <summary>
        /// Grand Total Daily: The total cost of items of „undefined type of service for FR special“ (super-reduced) with rates that are not contained in the previous ones (this can be for example 2,1%)
        /// </summary>
        public decimal GDayCITotalReducedS { get; set; }

        /// <summary>
        /// Grand Total Daily: The total cost of items of „undefined type of service for FR zero“ with data which are indicated with 0% sales tax and data where the sales tax is unknown
        /// </summary>
        public decimal GDayCITotalZero { get; set; }

        /// <summary>
        /// Grand Total Daily: The total cost of items not considered before
        /// </summary>
        public decimal GDayCITotalUnknown { get; set; }

        /// <summary>
        /// Grand Total Daily: The total amount of all payment types: cash, credit card, voucher
        /// </summary>
        public decimal GDayPITotalCash { get; set; }

        /// <summary>
        /// Grand Total Daily: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal GDayPITotalNonCash { get; set; }

        /// <summary>
        /// Grand Total Daily: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal GDayPITotalInternal { get; set; }

        /// <summary>
        /// Grand Total Daily: The total amount of payment not listed before
        /// </summary>
        public decimal GDayPITotalUnknown { get; set; }

        /// <summary>
        /// Grand Total Daily: queue item date and time of the last Grand Total Daily receipt
        /// </summary>
        public DateTime? GLastDayMoment { get; set; }

        /// <summary>
        /// Grand Total Daily: queue item id of the last Grand Total Daily receipt
        /// </summary>
        public Guid? GLastDayQueueItemId { get; set; }

        /// <summary>
        /// Grand Total Monthly totalizer
        /// resetted on each month closing
        /// closing has to be added to journal
        /// </summary>
        public decimal GMonthTotalizer { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total cost of items of „undefined type of service for FR normal“ (calculated with 20%)
        /// </summary>
        public decimal GMonthCITotalNormal { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal GMonthCITotalReduced1 { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total cost of items of „undefined type of service for FR reduced-2“ (calculated with 10%)
        /// </summary>
        public decimal GMonthCITotalReduced2 { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total cost of items of „undefined type of service for FR special“ (super-reduced) with rates that are not contained in the previous ones (this can be for example 2,1%)
        /// </summary>
        public decimal GMonthCITotalReducedS { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total cost of items of „undefined type of service for FR zero“ with data which are indicated with 0% sales tax and data where the sales tax is unknown
        /// </summary>
        public decimal GMonthCITotalZero { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total cost of items not considered before
        /// </summary>
        public decimal GMonthCITotalUnknown { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total amount of all payment types: cash, credit card, voucher
        /// </summary>
        public decimal GMonthPITotalCash { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal GMonthPITotalNonCash { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal GMonthPITotalInternal { get; set; }

        /// <summary>
        /// Grand Total Monthly: The total amount of payment not listed before
        /// </summary>
        public decimal GMonthPITotalUnknown { get; set; }

        /// <summary>
        /// Grand Total Monthly: queue item date and time of the last Grand Total Monthly receipt
        /// </summary>
        public DateTime? GLastMonthMoment { get; set; }

        /// <summary>
        /// Grand Total Monthly: queue item id of the last Grand Total Monthly receipt
        /// </summary>
        public Guid? GLastMonthQueueItemId { get; set; }

        /// <summary>
        /// Grand Total Yearly
        /// resetted on each year closing
        /// closing has to be added to journal
        /// </summary>
        public decimal GYearTotalizer { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total cost of items of „undefined type of service for FR normal“ (calculated with 20%)
        /// </summary>
        public decimal GYearCITotalNormal { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal GYearCITotalReduced1 { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total cost of items of „undefined type of service for FR reduced-2“ (calculated with 10%)
        /// </summary>
        public decimal GYearCITotalReduced2 { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total cost of items of „undefined type of service for FR special“ (super-reduced) with rates that are not contained in the previous ones (this can be for example 2,1%)
        /// </summary>
        public decimal GYearCITotalReducedS { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total cost of items of „undefined type of service for FR zero“ with data which are indicated with 0% sales tax and data where the sales tax is unknown
        /// </summary>
        public decimal GYearCITotalZero { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total cost of items not considered before
        /// </summary>
        public decimal GYearCITotalUnknown { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total amount of all payment types: cash, credit card, voucher
        /// </summary>
        public decimal GYearPITotalCash { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal GYearPITotalNonCash { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal GYearPITotalInternal { get; set; }

        /// <summary>
        /// Grand Total Yearly: The total amount of payment not listed before
        /// </summary>
        public decimal GYearPITotalUnknown { get; set; }

        /// <summary>
        /// Grand Total Yearly: queue item date and time of the last Grand Total Yearly receipt
        /// </summary>
        public DateTime? GLastYearMoment { get; set; }

        /// <summary>
        /// Grand Total Yearly: queue item id of the last Grand Total Yearly receipt
        /// </summary>
        public Guid? GLastYearQueueItemId { get; set; }

        #endregion

        #region Bill

        /// <summary>
        /// Bill numerator
        /// is raied by 1 for each bill. used for national numbering prefixed by B
        /// </summary>
        public long BNumerator { get; set; }

        /// <summary>
        /// Bill totalizer
        /// </summary>
        public decimal BTotalizer { get; set; }

        /// <summary>
        /// Bill: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal BCITotalNormal { get; set; }

        /// <summary>
        /// Bill: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal BCITotalReduced1 { get; set; }

        /// <summary>
        /// Bill: The total cost of items of „undefined type of service for FR reduced-2“ (calculated with 10%)
        /// </summary>
        public decimal BCITotalReduced2 { get; set; }

        /// <summary>
        /// Bill: The total cost of items of „undefined type of service for FR special“ (super-reduced) with rates that are not contained in the previous ones (this can be for example 2,1%)
        /// </summary>
        public decimal BCITotalReducedS { get; set; }

        /// <summary>
        /// Bill: The total cost of items of „undefined type of service for FR zero“ with data which are indicated with 0% sales tax and data where the sales tax is unknown
        /// </summary>
        public decimal BCITotalZero { get; set; }

        /// <summary>
        /// Bill: The total cost of items not considered before
        /// </summary>
        public decimal BCITotalUnknown { get; set; }

        /// <summary>
        /// Bill: The total amount of all payment types: wire-transfer, debit card, paypal
        /// </summary>
        public decimal BPITotalCash { get; set; }

        /// <summary>
        /// Bill: The total amount of all payment types: wire-transfer, debit card, paypal
        /// </summary>
        public decimal BPITotalNonCash { get; set; }

        /// <summary>
        /// Bill: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal BPITotalInternal { get; set; }

        /// <summary>
        /// Bill: The total amount of payment not listed before
        /// </summary>
        public decimal BPITotalUnknown { get; set; }

        /// <summary>
        /// Bill hash
        /// calculated only for Bill receipts
        /// </summary>
        public string BLastHash { get; set; }

        #endregion

        #region Log

        /// <summary>
        /// Log numerator
        /// is raied by 1 for each log. used for national numbering prefixed by L
        /// </summary>
        public long LNumerator { get; set; }

        /// <summary>
        /// Log hash
        /// calculated only for Log receipts
        /// </summary>
        public string LLastHash { get; set; }

        #endregion

        #region Archiv

        /// <summary>
        /// Archive numerator
        /// is raied by 1 for each archiv. used for national numbering prefixed by A
        /// archiv total is raised by dayly closing and set to zero on each archiv
        /// </summary>
        public long ANumerator { get; set; }

        /// <summary>
        /// Archive hash
        /// calculated only for Archive receipts
        /// </summary>
        public string ALastHash { get; set; }

        /// <summary>
        /// Archive totalizer
        /// resetted on each archiving
        /// archiving has to be added to journal
        /// </summary>
        public decimal ATotalizer { get; set; }

        /// <summary>
        /// Archive: The total cost of items of „undefined type of service for FR normal“ (calculated with 20%)
        /// </summary>
        public decimal ACITotalNormal { get; set; }

        /// <summary>
        /// Archive: The total cost of items of „undefined type of service for FR reduced-1“ (calculated with 5,5%)
        /// </summary>
        public decimal ACITotalReduced1 { get; set; }

        /// <summary>
        /// Archive: The total cost of items of „undefined type of service for FR reduced-2“ (calculated with 10%)
        /// </summary>
        public decimal ACITotalReduced2 { get; set; }

        /// <summary>
        /// Archive: The total cost of items of „undefined type of service for FR special“ (super-reduced) with rates that are not contained in the previous ones (this can be for example 2,1%)
        /// </summary>
        public decimal ACITotalReducedS { get; set; }

        /// <summary>
        /// Archive: The total cost of items of „undefined type of service for FR zero“ with data which are indicated with 0% sales tax and data where the sales tax is unknown
        /// </summary>
        public decimal ACITotalZero { get; set; }

        /// <summary>
        /// Archive: The total cost of items not considered before
        /// </summary>
        public decimal ACITotalUnknown { get; set; }

        /// <summary>
        /// Archive: The total amount of all payment types: cash, credit card, voucher
        /// </summary>
        public decimal APITotalCash { get; set; }

        /// <summary>
        /// Archive: The total amount of all payment types: wire-transfer, debit card, paypal
        /// </summary>
        public decimal APITotalNonCash { get; set; }

        /// <summary>
        /// Archive: The total amount of all payment types: payables, receivable
        /// </summary>
        public decimal APITotalInternal { get; set; }

        /// <summary>
        /// Archive: The total amount of payment not listed before
        /// </summary>
        public decimal APITotalUnknown { get; set; }

        /// <summary>
        /// Archive: last queue item date and time contained into the archive
        /// </summary>
        public DateTime? ALastMoment { get; set; }

        /// <summary>
        /// Archive: last queue item id contained into the archive
        /// </summary>
        public Guid? ALastQueueItemId { get; set; }

        #endregion

        #region Training

        /// <summary>
        /// Training numerator
        /// is raied by 1 for each training. used for national numbering prefixed by X
        /// </summary>
        public long XNumerator { get; set; }

        /// <summary>
        /// Training totalizer
        /// </summary>
        public decimal XTotalizer { get; set; }

        /// <summary>
        /// Training hash
        /// calculated only for Training receipts
        /// </summary>
        public string XLastHash { get; set; }

        #endregion

        #region Copy

        /// <summary>
        /// Copy numerator
        /// is raied by 1 for each copy. used for national numbering prefixed by C
        /// </summary>
        public long CNumerator { get; set; }

        /// <summary>
        /// Copy totalizer
        /// </summary>
        public decimal CTotalizer { get; set; }

        /// <summary>
        /// Copy hash
        /// calculated only for Copy receipts
        /// </summary>
        public string CLastHash { get; set; }

        #endregion

        #region Late Signing

        /// <summary>
        /// Number of receipts sent out within the failed mode state
        /// </summary>
        public int UsedFailedCount { get; set; }

        /// <summary>
        /// Date and time of the first receipt in failed mode
        /// </summary>
        public DateTime? UsedFailedMomentMin { get; set; }

        /// <summary>
        /// Date and time of the last receipt in failed mode
        /// </summary>
        public DateTime? UsedFailedMomentMax { get; set; }

        /// <summary>
        /// Queue item id of the first receipt in failed mode
        /// </summary>
        public Guid? UsedFailedQueueItemId { get; set; }

        #endregion

        #region Messages

        /// <summary>
        /// Number of messages to be sent to the POS System
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Date and time of the first message to be sent
        /// </summary>
        public DateTime? MessageMoment { get; set; }

        #endregion
    }
}
