using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class UnregisterTcrRequest
    {
        /// <summary>
        /// Message identifier, equal to ftQueueItemId when used by fiskaltrust.
        /// </summary>
        [DataMember(Order = 10)]
        public Guid RequestId { get; set; }

        /// <summary>
        /// Code of the business unit in which the invoice is issued.
        /// </summary>
        [DataMember(Order = 20)]
        public string BusinessUnitCode { get; set; }

        /// <summary>
        /// Element representing the internal identification of the TCR.
        /// </summary>
        [DataMember(Order = 30)]
        public string InternalTcrIdentifier { get; set; }

        /// <summary>
        /// Code of the software used for invoice issuing.
        /// </summary>
        [DataMember(Order = 40)]
        public string? TcrSoftwareCode { get; set; }

        /// <summary>
        /// Code of the maintainer of the software used for invoice issuing.
        /// </summary>
        [DataMember(Order = 50)]
        public string? TcrSoftwareMaintainerCode { get; set; }

        /// <summary>
        /// Type of the TCR, i.e. if it's a regular TCR or a self-service vending machine.
        /// </summary>
        [DataMember(Order = 60)]
        public TcrType? TcrType { get; set; }
    }
}
