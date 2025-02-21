using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.at
{
    [DataContract]
    public class SignResponse
    {
        [DataMember(Order = 10)]
        public byte[] SignedData { get; set; }
    }
}