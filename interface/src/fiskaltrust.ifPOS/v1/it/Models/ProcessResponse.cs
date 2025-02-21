using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    [DataContract]
    public class ProcessResponse
    {
        [DataMember(Order = 10)]
        public ReceiptResponse ReceiptResponse { get; set; }
    }
}
