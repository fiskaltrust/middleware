using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class ScuMeEchoRequest
    {
        [DataMember(Order = 10)]
        public string Message { get; set; }
    }
}
