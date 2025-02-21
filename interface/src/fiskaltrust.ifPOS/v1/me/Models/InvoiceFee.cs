#nullable enable
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class InvoiceFee
    {
        /// <summary>
        /// The type of the fee, e.g. packing or returning of glass bottles.
        /// </summary>
        [DataMember(Order = 10)]
        public FeeType FeeType { get; set; }

        /// <summary>
        /// The amount of the fee in Euros.
        /// </summary>
        [DataMember(Order = 20)]
        public decimal Amount { get; set; }
    }

    [DataContract]
    public enum FeeType
    {
        /// <summary>
        /// Packing fee
        /// </summary>
        [EnumMember]
        Pack,
        /// <summary>
        /// Return glass bottle fee
        /// </summary>
        [EnumMember]
        Bottle,
        /// <summary>
        /// Money exchange office commission
        /// </summary>
        [EnumMember]
        Commission,
        /// <summary>
        /// Other fee not covered by the other types.
        /// </summary>
        [EnumMember]
        Other
    }
}
