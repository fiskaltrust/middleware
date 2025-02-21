using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class TseSignatureData
    {
        [DataMember(Order = 10)]
        public ulong SignatureCounter { get; set; }

        [DataMember(Order = 20)]
        public string SignatureAlgorithm { get; set; }

        [DataMember(Order = 30)]
        public string SignatureBase64 { get; set; }

        [DataMember(Order = 40)]
        public string PublicKeyBase64 { get; set; }
    }
}
