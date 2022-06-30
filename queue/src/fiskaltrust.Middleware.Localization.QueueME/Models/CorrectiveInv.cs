using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class CorrectiveInv
    {
        [DataMember]
        public string ReferencedIKOF { get; set; }

        [DataMember]
        public DateTime ReferencedMoment { get; set; }

        [DataMember]
        public string Type { get; set; }
    }
}
