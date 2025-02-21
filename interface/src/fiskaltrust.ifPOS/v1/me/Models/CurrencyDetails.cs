#nullable enable
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class CurrencyDetails
    {
        /// <summary>
        /// Currency code in which the amount of the invoice should be paid in ISO 4217 format, e.g. <c>EUR</c> or <c>USD</c>.
        /// </summary>
        [DataMember(Order = 10)]
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Exchange rate applied to calculate the equivalent amount of foreign currency for the total amount expressed in €. 
        /// </summary>
        /// <remarks>The Exchange rate express equivalent amount of € for 1 unit of the foreign currency.</remarks>
        [DataMember(Order = 20)]
        public decimal ExchangeRateToEuro { get; set; }
    }
}
