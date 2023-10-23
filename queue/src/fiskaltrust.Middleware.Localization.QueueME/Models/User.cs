using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    public class User
    {
        [DataMember]
        public string OperatorCode { get; set; }
    }
}
