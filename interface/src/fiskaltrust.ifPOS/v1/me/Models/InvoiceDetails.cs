using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable enable
namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class InvoiceDetails
    {
        /// <summary>
        /// The type of the invoice, either cash or non-cash.
        /// </summary>
        [DataMember(Order = 10)]
        public InvoiceType InvoiceType { get; set; }

        /// <summary>
        /// The type of the invoice if it is self-issued by the end customer. Null if the invoice is not self-issued.
        /// </summary>
        [DataMember(Order = 20)]
        public SelfIssuedInvoiceType? SelfIssuedInvoiceType { get; set; }

        /// <summary>
        /// The ordinal number of the ionvoice. Each new invoice gets a new upcounting number, starting from zero on the start of each new year.
        /// </summary>
        [DataMember(Order = 30)]
        public ulong YearlyOrdinalNumber { get; set; }

        /// <summary>
        /// Deadline of the payment, if agreed on when creating the invoice. Null if not set or if the payment is made immediately.
        /// </summary>
        [DataMember(Order = 40)]
        public DateTime? PaymentDeadline { get; set; }

        /// <summary>
        /// The tax period (year and month) the invoice belongs to, if appicable. Null if not applicable.
        /// </summary>
        [DataMember(Order = 50)]
        public TaxPeriod? TaxPeriod { get; set; }

        /// <summary>
        /// If this invoice is used to correct an existing invoice, this property contains a reference to the original and the reason for the correction. Null if this invoice is not used to correct another one.
        /// </summary>
        [DataMember(Order = 60)]
        public InvoiceCorrectionDetails? InvoiceCorrectionDetails { get; set; }

        /// <summary>
        /// Total price of the invoice excluding VAT.
        /// </summary>
        [DataMember(Order = 70)]
        public decimal NetAmount { get; set; }

        /// <summary>
        /// Total price of all items including taxes and discounts.
        /// </summary>
        [DataMember(Order = 80)]
        public decimal GrossAmount { get; set; }

        /// <summary>
        /// Amount of goods for export from Montenegro. Null if not applicable.
        /// </summary>
        [DataMember(Order = 90)]
        public decimal? ExportedGoodsAmount { get; set; }

        /// <summary>
        /// Total VAT amount of the invoice. Mandatory if the issuer is in the VAT system, null if not applicable.
        /// </summary>
        [DataMember(Order = 100)]
        public decimal? TotalVatAmount { get; set; }

        /// <summary>
        /// The total amount of goods and services delivered when VAT is not charged. Null if not applicable.
        /// </summary>
        [DataMember(Order = 110)]
        public decimal? TaxFreeAmount { get; set; }

        /// <summary>
        /// Details about all payments made for this invoice.
        /// </summary>
        [DataMember(Order = 120)]
        public List<InvoicePayment> PaymentDetails { get; set; }

        /// <summary>
        /// Details about all items included in the invoice.
        /// </summary>
        [DataMember(Order = 130)]
        public List<InvoiceItem> ItemDetails { get; set; }

        /// <summary>
        /// Details about all special fees included in the invoice.
        /// </summary>
        [DataMember(Order = 140)]
        public List<InvoiceFee>? Fees { get; set; }

        /// <summary>
        /// Details about the currency in which the amount on the invoice is paid.
        /// </summary>
        [DataMember(Order = 150)]
        public CurrencyDetails? Currency { get; set; }

        /// <summary>
        /// Details about the buyer (i.e. the end customer) for whom the invoice is generated.
        /// </summary>
        [DataMember(Order = 160)]
        public BuyerDetails? Buyer { get; set; }

        /// <summary>
        /// The type of the invoicing (Invoice, Corrective, Summary).
        /// </summary>
        [DataMember(Order = 170)]
        public InvoicingType InvoicingType { get; set; }

        /// <summary>
        /// IicReference, needed for Summary Receipt on nonCash correctives.
        /// </summary>
        [DataMember(Order = 180)]
        public IicReference[]? IicReferences { get; set; }
    }

    /// <summary>
    /// Type of invoice (Cash, NonCash, Undefined)
    /// </summary>
    public enum InvoiceType
    {
        /// <summary>
        /// Undefined
        /// </summary>
        [EnumMember]
        Undefined,
        /// <summary>
        /// Cash payment in local or foreign currency.
        /// </summary>
        [EnumMember]
        Cash,
        /// <summary>
        /// Non-cash payment, e.g. credit or debit card.
        /// </summary>
        [EnumMember]
        NonCash
    }

    /// <summary>
    /// Invoicing type (Invoice, Corrective, Summary, CreditNote)
    /// </summary>
    public enum InvoicingType
    {
        /// <summary>
        /// Invoice
        /// </summary>
        [EnumMember]
        Invoice,
        /// <summary>
        /// Corrective Invoice, e.g. void.
        /// </summary>
        [EnumMember]
        Corrective,
        /// <summary>
        /// Summary Invoice, e.g. non cash partial void.
        /// </summary>
        [EnumMember]
        Summary
    }

    public enum SelfIssuedInvoiceType
    {
        /// <summary>
        /// Previous agreement by both parties.
        /// </summary>
        [EnumMember]
        Agreement,
        /// <summary>
        /// Buying from local.
        /// </summary>
        [EnumMember]
        Domestic,
        /// <summary>
        /// Buying services abroad.
        /// </summary>
        [EnumMember]
        Abroad,
        /// <summary>
        /// Other / Not matching any of the other categories.
        /// </summary>
        [EnumMember]
        Other
    }
}
