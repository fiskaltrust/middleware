using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    [DataContract]
    public class ScuItEchoResponse
    {
        [DataMember(Order = 10)]
        public string Message { get; set; }
    }
}
