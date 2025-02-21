using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable enable
namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class InvoiceItem
    {
        /// <summary>
        /// Name of the item (goods or services).
        /// </summary>
        [DataMember(Order = 10)]
        public string Name { get; set; }

        /// <summary>
        /// Code of the item from the barcode or similar representation.
        /// </summary>
        [DataMember(Order = 20)]
        public string? Code { get; set; }

        /// <summary>
        /// Flag that the item is an investment.
        /// </summary>
        [DataMember(Order = 30)]
        public bool? IsInvestment { get; set; }

        /// <summary>
        /// The item's unit of measure (piece, kilogram, etc.)
        /// </summary>
        [DataMember(Order = 40)]
        public string Unit { get; set; }

        /// <summary>
        /// Amount or number (quantity) of items.
        /// </summary>
        [DataMember(Order = 50)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unit price before VAT is applied.
        /// </summary>
        [DataMember(Order = 60)]
        public decimal NetUnitPrice { get; set; }

        /// <summary>
        /// Unit price after VAT is applied.
        /// </summary>
        [DataMember(Order = 70)]
        public decimal GrossUnitPrice { get; set; }

        /// <summary>
        /// Percentage of the discount or rebate (between <c>0.00</c> and <c>100.00</c>).
        /// </summary>
        [DataMember(Order = 80)]
        public decimal? DiscountPercentage { get; set; }

        /// <summary>
        /// Marks if a given discount or rebate reducing the base price or not
        /// </summary>
        [DataMember(Order = 90)]
        public bool? IsDiscountReducingBasePrice { get; set; }

        /// <summary>
        /// Total price of goods and services before the tax.
        /// </summary>
        [DataMember(Order = 100)]
        public decimal NetAmount { get; set; }

        /// <summary>
        /// Total price of goods after the tax and applying discounts.
        /// </summary>
        [DataMember(Order = 110)]
        public decimal GrossAmount { get; set; }

        /// <summary>
        /// If set, marks that this item is exempted or released from VAT. Null if not applicable.
        /// </summary>
        [DataMember(Order = 120)]
        public ExemptFromVatReasons? ExemptFromVatReason { get; set; }

        /// <summary>
        /// Rate of VAT (between 0.00 and 100.00). Mandatory if issuer is in PDV system, null if not applicable.
        /// </summary>
        [DataMember(Order = 130)]
        public decimal? VatRate { get; set; }

        /// <summary>
        /// Amount of VAT for goods and services. 
        /// Mandatory if the issuer is in the VAT system, if there is a self-charging device (vending machine) and the issuer is in the VAT system, or if reverse charging applies.
        /// </summary>
        [DataMember(Order = 140)]
        public decimal? VatAmount { get; set; }

        /// <summary>
        /// List of sold vouchers (please see <see cref="InvoiceDetails.PaymentDetails"/> if vouchers are used to pay for this invoice). Null if no vouchers were sold.
        /// </summary>
        [DataMember(Order = 150)]
        public List<VoucherItem>? Vouchers { get; set; }
    }

    [DataContract]
    public enum ExemptFromVatReasons
    {
        [EnumMember]
        PlaceOfServiceTurnover,
        [EnumMember]
        TaxBaseAndCorrection,
        [EnumMember]
        DisengagementFromPublicInterest,
        [EnumMember]
        OtherDisengagements,
        [EnumMember]
        DisengagementWhenImportingGoods,
        [EnumMember]
        DisengagementWhenImportingGoodsTemporarily,
        [EnumMember]
        SpecialDisengagements
    }
}
