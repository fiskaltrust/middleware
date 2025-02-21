using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class ExportDataRequest
    {
        [DataMember(Order = 10)]
        public string TokenId { get; set; }
        [DataMember(Order = 20)]
        public int MaxChunkSize { get; set; }
    }
}
