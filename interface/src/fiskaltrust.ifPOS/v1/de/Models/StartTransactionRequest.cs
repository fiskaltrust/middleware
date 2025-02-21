using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class StartTransactionRequest
    {
        [DataMember(Order = 10)]
        public string ClientId { get; set; }

        [DataMember(Order = 20)]
        public string ProcessType { get; set; }

        [DataMember(Order = 30)]
        public string ProcessDataBase64 { get; set; }

        [DataMember(Order = 40)]
        public Guid QueueItemId { get; set; }

        [DataMember(Order = 50)]
        public bool IsRetry { get; set; }
    }
}
