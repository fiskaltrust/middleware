using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class Buyer
    {
        [DataMember]
        public string IDType
        {
            get;
            set;
        }
        [DataMember]
        public string IDNum
        {
            get;
            set;
        }
        [DataMember]
        public string Name
        {
            get;
            set;
        }
        [DataMember]
        public string Address
        {
            get;
            set;
        }
        [DataMember]
        public string Town
        {
            get;
            set;
        }
        [DataMember]
        public string Country
        {
            get;
            set;
        }
    }
}
