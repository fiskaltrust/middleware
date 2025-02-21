using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1
{
    /// <summary>
    /// Payment entries are used for receipt requests as well as for receipt responses.
    /// </summary>
    [DataContract]
    public class PayItem
    {
        /// <summary>
        /// Line number or positionnumber on the Receipt. Used to preserve the order of lines on the receipt.
        /// </summary>
        [DataMember(Order = 5, EmitDefaultValue = false, IsRequired = false)]
        public long Position { get; set; }

        /// <summary>
        /// Number of payments. This value will be set to 1 in most of the cases. It can be greater then 1 e.g. when paying with multiple vouchers of the same value.
        /// </summary>
        [DataMember(Order = 10, EmitDefaultValue = true, IsRequired = true)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Name or description of payment.
        /// </summary>
        [DataMember(Order = 20, EmitDefaultValue = true, IsRequired = true)]
        public string Description { get; set; }

        /// <summary>
        /// Total amount of payment.
        /// </summary>
        [DataMember(Order = 30, EmitDefaultValue = true, IsRequired = true)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Type of payment according to the reference table in the appendix. It is used in order to determine the processing logic.
        /// </summary>
        [DataMember(Order = 40, EmitDefaultValue = true, IsRequired = true)]
        public long ftPayItemCase { get; set; }

        /// <summary>
        /// Additional data about the payment, currently accepted only in JSON format.
        /// </summary>
        [DataMember(Order = 50, EmitDefaultValue = false, IsRequired = false)]
        public string ftPayItemCaseData { get; set; }

        /// <summary>
        /// Account number for transfer into bookkeeping.
        /// </summary>
        [DataMember(Order = 60, EmitDefaultValue = false, IsRequired = false)]
        public string AccountNumber { get; set; }

        /// <summary>
        /// Indicator for transfer into cost accounting (type, centre and payer)
        /// </summary>
        [DataMember(Order = 70, EmitDefaultValue = false, IsRequired = false)]
        public string CostCenter { get; set; }

        /// <summary>
        /// This value allows the logical grouping of payment types.
        /// </summary>
        [DataMember(Order = 80, EmitDefaultValue = false, IsRequired = false)]
        public string MoneyGroup { get; set; }

        /// <summary>
        /// This value identifies the payment type.
        /// </summary>
        [DataMember(Order = 90, EmitDefaultValue = false, IsRequired = false)]
        public string MoneyNumber { get; set; }

        /// <summary>
        /// Time of payment
        /// </summary>
        [DataMember(Order = 100, EmitDefaultValue = false, IsRequired = false)]
        public DateTime? Moment { get; set; }

        public PayItem()
        {
            Quantity = 1.0m;
            Description = string.Empty;
            Amount = 0.0m;
            ftPayItemCase = 0x0;
            ftPayItemCaseData = string.Empty;
            AccountNumber = string.Empty;
            CostCenter = string.Empty;
            MoneyGroup = string.Empty;
            MoneyNumber = string.Empty;
            Moment = null;
        }
    }
}
