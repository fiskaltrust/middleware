using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// <summary>
    /// PaymentAdjustmentType
    /// </summary>
    [DataContract]
    public enum PaymentAdjustmentType
    {
        /// <summary>
        /// Discount or Surcharge
        /// </summary>
        [DataMember(Order = 10)]
        Adjustment = 1,
        /// <summary>
        /// Deposit (Acconto)
        /// </summary>
        [DataMember(Order = 20)]
        Acconto = 2,
        /// <summary>
        /// Omaggio (Free of Charge)
        /// </summary>
        [DataMember(Order = 30)]
        FreeOfCharge = 3,
        /// <summary>
        /// Buono monouso (single-use voucher)
        /// </summary>
        [DataMember(Order = 40)]
        SingleUseVoucher = 4,
    }
}
