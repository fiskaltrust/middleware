using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.at
{
    [DataContract]
    public class SignRequest
    {
        [DataMember(Order = 10)]
        public byte[] Data { get; set; }
    }
}