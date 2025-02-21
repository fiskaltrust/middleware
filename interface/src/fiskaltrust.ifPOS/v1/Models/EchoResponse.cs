using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1
{
    /// <summary>
    /// Response to the EchoRequest. Returns the message provided in the EchoRequest if successfull.
    /// </summary>
    [DataContract]
    public class EchoResponse
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}
