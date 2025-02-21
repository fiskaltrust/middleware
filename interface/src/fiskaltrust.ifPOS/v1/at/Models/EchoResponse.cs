using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.at
{
    [DataContract]
    public class EchoResponse
    {
        [DataMember(Order = 10)]
        public string Message { get; set; }
    }
}