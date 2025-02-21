using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class RegisterCashWithdrawalRequest
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
        /// The moment in which the cash withdrawal was made from the till.
        /// </summary>
        [DataMember(Order = 40)]
        public DateTime Moment { get; set; }

        /// <summary>
        /// When not null, signalizes that this receipt was late-signed because of the given reason. Null when the receipt was not processed in late-signing mode.
        /// </summary>
        [DataMember(Order = 50)]
        public SubsequentDeliveryType? SubsequentDeliveryType { get; set; }

        /// <summary>
        /// Relative amount that is withdrawn from the till.
        /// </summary>
        [DataMember(Order = 60)]
        public decimal Amount { get; set; }
    }
}
