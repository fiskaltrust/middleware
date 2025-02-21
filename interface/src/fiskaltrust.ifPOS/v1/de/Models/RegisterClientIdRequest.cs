using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class RegisterClientIdRequest
    {
        [DataMember(Order = 10)]
        public string ClientId { get; set; }
    }
}
