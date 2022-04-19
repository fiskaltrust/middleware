using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class Fee
    {
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public decimal Amt { get; set; }
    }
}
