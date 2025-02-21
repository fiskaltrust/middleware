using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class FinishTransactionResponse
    {
        [DataMember(Order = 10)]
        public ulong TransactionNumber { get; set; }

        [DataMember(Order = 20)]
        public DateTime StartTransactionTimeStamp { get; set; }

        [DataMember(Order = 30)]
        public DateTime TimeStamp { get; set; }

        [DataMember(Order = 40)]
        public string TseTimeStampFormat { get; set; }

        [DataMember(Order = 50)]
        public string TseSerialNumberOctet { get; set; }

        [DataMember(Order = 60)]
        public string ClientId { get; set; }

        [DataMember(Order = 70)]
        public string ProcessType { get; set; }

        [DataMember(Order = 80)]
        public string ProcessDataBase64 { get; set; }

        [DataMember(Order = 90)]
        public TseSignatureData SignatureData { get; set; }

    }
}
