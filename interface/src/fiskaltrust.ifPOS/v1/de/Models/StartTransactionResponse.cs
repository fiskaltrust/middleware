using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class StartTransactionResponse
    {
        [DataMember(Order = 10)]
        public ulong TransactionNumber { get; set; }

        [DataMember(Order = 20)]
        public DateTime TimeStamp { get; set; }

        [DataMember(Order = 30)]
        public string TseSerialNumberOctet { get; set; }

        [DataMember(Order = 40)]
        public string ClientId { get; set; }

        [DataMember(Order = 50)]
        public TseSignatureData SignatureData { get; set; }
    }
}
