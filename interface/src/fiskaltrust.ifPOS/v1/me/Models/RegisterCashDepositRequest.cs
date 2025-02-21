using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class RegisterCashDepositRequest
    {
        /// <summary>
        /// Message identifier, equal to ftQueueItemId when used by fiskaltrust.
        /// </summary>
        [DataMember(Order = 10)]
        public Guid RequestId { get; set; }

        /// <summary>
        /// Unique code of the cash register, assigned by the central invoice register (CIS) service when registering a cash register.
        /// </summary>
        [DataMember(Order = 20)]
        public string TcrCode { get; set; }

        /// <summary>
        /// The moment in which the cash deposit was made to the till.
        /// </summary>
        [DataMember(Order = 30)]
        public DateTime Moment { get; set; }

        /// <summary>
        /// When not null, signalizes that this receipt was late-signed because of the given reason. Null when the receipt was not processed in late-signing mode.
        /// </summary>
        [DataMember(Order = 40)]
        public SubsequentDeliveryType? SubsequentDeliveryType { get; set; }

        /// <summary>
        /// Absolute amount that is stored in the till after the cash deposit.
        /// </summary>
        [DataMember(Order = 50)]
        public decimal Amount { get; set; }
    }
}
