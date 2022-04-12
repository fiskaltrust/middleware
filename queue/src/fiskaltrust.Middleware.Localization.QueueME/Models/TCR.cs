using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class TCR
    {
        [DataMember]
        public string IssuerTIN
        {
            get;
            set;
        }
        [DataMember]
        public string BusinUnitCode
        {
            get;
            set;
        }
        [DataMember]
        public string TCRIntID
        {
            get;
            set;
        }
    }
}
