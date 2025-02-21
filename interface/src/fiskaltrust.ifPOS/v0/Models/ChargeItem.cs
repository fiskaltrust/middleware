using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v0
{
    /// <summary>
    /// Charge items entries are used for receipt requests as well as for receipt responses.
    /// </summary>
    [DataContract]
    public partial class ChargeItem
    {
        /// <summary>
        /// Line number or position number on the Receipt. Used to preserve the order of lines on the receipt.
        /// </summary>
        [DataMember(Order = 5, EmitDefaultValue = false, IsRequired = false)]
        public long Position { get; set; }

        /// <summary>
        /// Amount or volume (number) of service(s) or items of the entry.
        /// </summary>
        [DataMember(Order = 10, EmitDefaultValue = true, IsRequired = true)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Name, description of customary indication, or type of the service or item.
        /// </summary>
        [DataMember(Order = 20, EmitDefaultValue = true, IsRequired = true)]
        public string Description { get; set; }

        /// <summary>
        /// Gross total price of service(s). The gross individual price, net total price, and net individual price, have to be calculated using the amount and either VAT rate or VAT amount.
        /// </summary>
        [DataMember(Order = 30, EmitDefaultValue = true, IsRequired = true)]
        public decimal Amount { get; set; }

        /// <summary>
        /// VAT rate as percentage.
        /// </summary>
        [DataMember(Order = 40, EmitDefaultValue = true, IsRequired = true)]
        public decimal VATRate { get; set; }

        /// <summary>
        /// Type of service or item according to the reference table in the appendix. It is used in order to determine the processing logic for the corresopnding business transaction.
        /// </summary>
        [DataMember(Order = 50, EmitDefaultValue = true, IsRequired = true)]
        public long ftChargeItemCase { get; set; }

        /// <summary>
        /// Additional data about the service, currently accepted only in JSON format.
        /// </summary>
        [DataMember(Order = 60, EmitDefaultValue = false, IsRequired = false)]
        public string ftChargeItemCaseData { get; set; }

        /// <summary>
        /// If the VAT amount is indicated, it can be used to calculate the net amount in order to avoid rounding errors which are especially likely to appear in row-based net price additions.
        /// </summary>
        [DataMember(Order = 70, EmitDefaultValue = false, IsRequired = false)]
        public decimal? VATAmount { get; set; }

        /// <summary>
        /// Account number for transfer into bookkeeping.
        /// </summary>
        [DataMember(Order = 80, EmitDefaultValue = false, IsRequired = false)]
        public string AccountNumber { get; set; }

        /// <summary>
        /// Indicator for transfer into cost accounting (type, center, and payer).
        /// </summary>
        [DataMember(Order = 90, EmitDefaultValue = false, IsRequired = false)]
        public string CostCenter { get; set; }

        /// <summary>
        /// This value allows the customer the logical grouping of products.
        /// </summary>
        [DataMember(Order = 100, EmitDefaultValue = false, IsRequired = false)]
        public string ProductGroup { get; set; }

        /// <summary>
        /// Value used to identify the product.
        /// </summary>
        [DataMember(Order = 110, EmitDefaultValue = false, IsRequired = false)]
        public string ProductNumber { get; set; }

        /// <summary>
        /// Product’s barcode
        /// </summary>
        [DataMember(Order = 120, EmitDefaultValue = false, IsRequired = false)]
        public string ProductBarcode { get; set; }

        /// <summary>
        /// Unit of measurement
        /// </summary>
        [DataMember(Order = 130, EmitDefaultValue = false, IsRequired = false)]
        public string Unit { get; set; }

        /// <summary>
        /// Quantity of the service(s) of receipt entry, displayed in indicated units.
        /// </summary>
        [DataMember(Order = 140, EmitDefaultValue = false, IsRequired = false)]
        public decimal? UnitQuantity { get; set; }

        /// <summary>
        /// Gross price per indicated unit.
        /// </summary>
        [DataMember(Order = 150, EmitDefaultValue = false, IsRequired = false)]
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Time of service (year, month, day, hour, minute, second)
        /// </summary>
        [DataMember(Order = 160, EmitDefaultValue = false, IsRequired = false)]
        public DateTime? Moment { get; set; }

        public ChargeItem()
        {
            Quantity = 1.0m;
            Description = string.Empty;
            Amount = 0.0m;
            VATRate = 0.0m;
            ftChargeItemCase = 0x0;
            ftChargeItemCaseData = string.Empty;
            VATAmount = null;
            AccountNumber = string.Empty;
            CostCenter = string.Empty;
            ProductGroup = string.Empty;
            ProductNumber = string.Empty;
            ProductBarcode = string.Empty;
            Unit = string.Empty;
            UnitQuantity = null;
            UnitPrice = null;
            Moment = null;
        }
    }
}
