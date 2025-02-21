using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    [DataContract]
    public class ScuItEchoRequest
    {
        [DataMember(Order = 10)]
        public string Message { get; set; }
    }
}
