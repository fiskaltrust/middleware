using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1
{
    /// <summary>
    /// Request to check if the communication to the Middleware is up and running. Body Contains a Message e.g. "Hello World!"
    /// </summary>
    [DataContract]
    public class EchoRequest
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }

    }
}
