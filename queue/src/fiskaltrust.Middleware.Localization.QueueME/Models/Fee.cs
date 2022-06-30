using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class Fee
    {
        [DataMember]
        public string FeeType { get; set; }

        [DataMember]
        public decimal Amount { get; set; }
    }
}
