using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable enable
namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class InvoicePayment
    {
        /// <summary>
        /// Type of the payment, e.g. card or voucher.
        /// </summary>
        [DataMember(Order = 10)]
        public PaymentType Type { get; set; }

        /// <summary>
        /// Total amount payed with this payment method.
        /// </summary>
        [DataMember(Order = 20)]
        public decimal Amount { get; set; }

        /// <summary>
        /// If the payment type is <c>Company</c>, this property has to contain the company card number. Null if other payment type is used.
        /// </summary>
        [DataMember(Order = 30)]
        public string? CompanyCardNumber { get; set; }

        /// <summary>
        /// If the payment type is <c>Voucher</c>, this property has to contain the used voucher numbers. Null if other payment type is used.
        /// </summary>
        /// <remarks>
        /// Must have the following format: <c>[1-9][0-9]{0,7}–[0-9]{4}–[0-9]{8}</c>.
        /// </remarks>
        [DataMember(Order = 40)]
        public List<string>? VoucherNumbers { get; set; }
    }

    [DataContract]
    public enum PaymentType
    {
        /// <summary>
        /// Notes and coins (allowed invoice type: Cash).
        /// </summary>
        [EnumMember]
        Banknote,
        /// <summary>
        /// Credit or debit card of the bank issued to a natural person (allowed invoice type: Cash).
        /// </summary>
        [EnumMember]
        Card,
        /// <summary>
        /// Credit or debit card of the bank issued to a taxpayer (allowed invoice type: Non-Cash).
        /// </summary>
        [EnumMember]
        BusinessCard,
        /// <summary>
        /// Onetime voucher (allowed invoice type: Non-Cash).
        /// </summary>
        [EnumMember]
        Voucher,
        /// <summary>
        /// Cards issued by the company, gift cards and similar prepaid cards (allowed invoice type: Non-Cash).
        /// </summary>
        [EnumMember]
        Company,
        /// <summary>
        /// Invoice to be paid in summary invoice (allowed invoice type: Cash or Non-Cash).
        /// </summary>
        [EnumMember]
        Order,
        /// <summary>
        /// Advance payment (allowed invoice type: Non-Cash).
        /// </summary>
        [EnumMember]
        Advance,
        /// <summary>
        /// Transaction account (allowed invoice type: Non-Cash).
        /// </summary>
        [EnumMember]
        Account,
        /// <summary>
        /// Factoring (allowed invoice type: Non-Cash).
        /// </summary>
        [EnumMember]
        Factoring,
        /// <summary>
        /// Other non-cash payments not covered by the other types (allowed invoice type: Non-Cash).
        /// </summary>
        [EnumMember]
        OtherNonCash,
        /// <summary>
        /// Other cash payments not covered by the other types (allowed invoice type: Cash).
        /// </summary>
        [EnumMember]
        OtherCash
    }
}
