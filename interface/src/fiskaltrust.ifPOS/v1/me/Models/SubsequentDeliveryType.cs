using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public enum SubsequentDeliveryType
    {
        /// <summary>
        /// Set when TCR works in an area without internet.
        /// </summary>
        [EnumMember]
        NoInternet,
        /// <summary>
        /// TCR was not working in the moment of creating the original receipt.
        /// </summary>
        [EnumMember]
        BoundBook,
        /// <summary>
        /// Fiscalization service was not working in the moment of creating the original receipt.
        /// </summary>
        [EnumMember]
        Service,
        /// <summary>
        /// Other unspecified technical error.
        /// </summary>
        [EnumMember]
        TechnicalError,
        /// <summary>
        /// Subsequent sending conditioned by the way of doing business.
        /// </summary>
        [EnumMember]
        BusinessNeeds
    }
}
