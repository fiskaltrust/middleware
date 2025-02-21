using fiskaltrust.ifPOS.v1.errors;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    [DataContract]
    public class ProcessRequest
    {
        [DataMember(Order = 10)]
        public ReceiptRequest ReceiptRequest { get; set; }

        [DataMember(Order = 20)]
        public ReceiptResponse ReceiptResponse { get; set; }
    }
}
