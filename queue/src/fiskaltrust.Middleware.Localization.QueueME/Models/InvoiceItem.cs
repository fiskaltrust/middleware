using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class InvoiceItem
    {       
        [DataMember]
        public bool? IN { get; set; }

        [DataMember]
        public string VD { get; set; }

        [DataMember]
        public string VSN { get; set; }

        [DataMember]
        public decimal? R { get; set; }

        [DataMember]
        public bool? RR { get; set; }

        [DataMember]
        public string EX { get; set; }
    }
}
