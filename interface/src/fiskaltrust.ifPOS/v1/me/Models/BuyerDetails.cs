#nullable enable
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class BuyerDetails
    {
        /// <summary>
        /// Identification type of the buyer, e.g. TIN or social security number.
        /// </summary>
        [DataMember(Order = 10)]
        public BuyerIdentificationType IdentificationType { get; set; }

        /// <summary>
        /// Identification number of the buyer as specified by the buyer's identification type.
        /// </summary>
        [DataMember(Order = 20)]
        public string IdentificationNumber { get; set; }
        
        /// <summary>
        /// Name of the buyer, either personal or company.
        /// </summary>
        [DataMember(Order = 30)]
        public string Name { get; set; }

        /// <summary>
        /// Street name and number of the buyer's address. May be null if unknown.
        /// </summary>
        [DataMember(Order = 40)]
        public string? Address { get; set; }

        /// <summary>
        /// Town of the buyer's address. May be null if unknown.
        /// </summary>
        [DataMember(Order = 50)]
        public string? Town { get; set; }

        /// <summary>
        /// Country in ISO 3166-1 Alfa-3 code, e.g. <c>MNE</c> or <c>AUT</c>. May be null if unknown.
        /// </summary>
        [DataMember(Order = 60)]
        public string? Country { get; set; }
    }

    [DataContract]
    public enum BuyerIdentificationType
    {
        /// <summary>
        /// Personal tax number.
        /// </summary>
        [EnumMember]
        TIN,
        /// <summary>
        /// Personal identification number.
        /// </summary>
        [EnumMember]
        ID,
        /// <summary>
        /// Personal passport number.
        /// </summary>
        [EnumMember]
        Passport,
        /// <summary>
        /// VAT number of the buying entity.
        /// </summary>
        [EnumMember]
        VatNumber,
        /// <summary>
        /// Tax number of the buying entity.
        /// </summary>
        [EnumMember]
        TaxNumber,
        /// <summary>
        /// Personal social security number.
        /// </summary>
        [EnumMember]
        SocialSecurityNumber
    }
}
