using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class RegisterClientIdResponse
    {
        [DataMember(Order = 10)]
        public IEnumerable<string> ClientIds { get; set; }
    }
}
