using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class Buyer
    {
        [DataMember]
        public string BuyerIdentificationType { get; set; }

        [DataMember]
        public string IdentificationNumber { get; set; }
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Address { get; set; }

        [DataMember]
        public string Town { get; set; }

        [DataMember]
        public string Country { get; set; }
    }
}
