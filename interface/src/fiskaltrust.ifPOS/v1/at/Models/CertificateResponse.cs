using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.at
{
    [DataContract]
    public class CertificateResponse
    {
        [DataMember(Order = 10)]
        public byte[] Certificate { get; set; }
    }
}