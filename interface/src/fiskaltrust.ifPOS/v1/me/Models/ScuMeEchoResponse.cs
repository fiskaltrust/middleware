using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class ScuMeEchoResponse
    {
        [DataMember(Order = 10)]
        public string Message { get; set; }
    }
}
