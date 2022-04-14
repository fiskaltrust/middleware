using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.Localization.QueueME.Models
{
    [DataContract]
    public class CorrectiveInv
    {
        [DataMember]
        public string IICRef
        {
            get;
            set;
        }
        [DataMember]
        public DateTime IssueDateTime
        {
            get;
            set;
        }
        [DataMember]
        public string Type
        {
            get;
            set;
        }
    }
}
