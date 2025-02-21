using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class StartExportSessionByTimeStampRequest
    {
        [DataMember(Order = 10)]
        public string ClientId { get; set; }

        [DataMember(Order = 20)]
        public DateTime From { get; set; }

        [DataMember(Order = 30)]
        public DateTime To { get; set; }
    }
}
