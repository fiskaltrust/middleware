using System;
using System.Runtime.Serialization;

#nullable enable
namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class InvoiceCorrectionDetails
    {
        /// <summary>
        /// IKOF of the original receipt that needs to be corrected.
        /// </summary>
        [DataMember(Order = 10)]
        public string ReferencedIKOF { get; set; }

        /// <summary>
        /// Issue moment of the original receipt that needs to be corrected.
        /// </summary>
        [DataMember(Order = 20)]
        public DateTime ReferencedMoment { get; set; }

        /// <summary>
        /// Type of the corrective invoice.
        /// </summary>
        [DataMember(Order = 30)]
        public InvoiceCorrectionType CorrectionType { get; set; }
    }

    [DataContract]
    public enum InvoiceCorrectionType
    {
        /// <summary>
        /// Correction invoice.
        /// </summary>
        [EnumMember]
        Corrective,
        /// <summary>
        /// Debit note.
        /// </summary>
        [EnumMember]
        Debit,
        /// <summary>
        /// Credit note
        /// </summary>
        [EnumMember]
        Credit
    }
}
