using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class Invoice
    {
        [DataMember]
        public string OperatorCode { get; set; }

        [DataMember]
        public string TypeOfSelfiss { get; set; }

        [DataMember]
        public DateTime? PayDeadline { get; set; }

        [DataMember]
        public Fee[] Fees { get; set; }

        [DataMember]
        public string SubsequentDeliveryType { get; set; }
    }
}
