using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class UnregisterClientIdResponse
    {
        [DataMember(Order = 10)]
        public IEnumerable<string> ClientIds { get; set; }
    }
}
