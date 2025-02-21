using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class TseState
    {
        [DataMember(Order = 10)]
        public TseStates CurrentState { get; set; }
    }
}
