using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class EndExportSessionResponse
    {
        [DataMember(Order = 10)]
        public string TokenId { get; set; }

        [DataMember(Order = 20)]
        public bool IsValid { get; set; }

        [DataMember(Order = 30)]
        public bool IsErased { get; set; }
    }
}
