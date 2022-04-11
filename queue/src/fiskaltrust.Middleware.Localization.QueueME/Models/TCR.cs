using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class TCR
    {
        public string IssuerTIN
        {
            get;
            set;
        }

        public string BusinUnitCode
        {
            get;
            set;
        }

    }
}
