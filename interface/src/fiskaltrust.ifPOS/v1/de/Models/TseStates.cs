using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public enum TseStates
    {
        [EnumMember]
        Uninitialized = 0,
        [EnumMember]
        Initialized = 1,
        [EnumMember]
        Terminated = 2
    }
}
