using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class UnregisterClientIdRequest
    {
        [DataMember(Order = 10)]
        public string ClientId { get; set; }
    }
}
