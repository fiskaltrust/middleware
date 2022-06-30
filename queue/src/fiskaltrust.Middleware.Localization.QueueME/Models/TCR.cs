using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class Tcr
    {
        [DataMember]
        public string TcrType { get; set; }

        [DataMember]
        public string IssuerTin { get; set; }

        [DataMember]
        public string BusinessUnitCode { get; set; }

        [DataMember]
        public string SoftwareCode { get; set; }

        [DataMember]
        public string MaintainerCode { get; set; }

        [DataMember]
        public string TcrIntId { get; set; }

        [DataMember]
        public DateTime? ValidFrom { get; set; }
    }
}
