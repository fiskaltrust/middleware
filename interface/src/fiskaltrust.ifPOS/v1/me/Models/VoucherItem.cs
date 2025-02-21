using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#nullable enable
namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class VoucherItem
    {
        /// <summary>
        /// Expiration date of the voucher.
        /// </summary>
        [DataMember(Order = 10)]
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Nominal value of the voucher in Euro.
        /// </summary>
        [DataMember(Order = 20)]
        public decimal NominalValue { get; set; }

        /// <summary>
        /// Serial numbers of the sold vouchers, e.g. <c>2-2020-12345678</c>.
        /// </summary>
        /// <remarks>
        /// Must have the following format: <c>[1-9][0-9]{0,7}–[0-9]{4}–[0-9]{8}</c>.
        /// </remarks>
        [DataMember(Order = 30)]
        public List<string> SerialNumbers { get; set; }
    }
}
