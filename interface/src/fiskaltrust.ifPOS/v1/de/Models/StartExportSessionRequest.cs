using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class StartExportSessionRequest
    {
        [DataMember(Order = 10)]
        public string ClientId { get; set; }

        /// <summary>
        /// Prepares data deletion at session end.
        /// </summary>
        [DataMember(Order = 20)]
        public bool Erase { get; set; }
    }
}
