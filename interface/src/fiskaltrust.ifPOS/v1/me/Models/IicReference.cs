using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    /// <summary>
    /// IicReference, needed for Summary Receipt on nonCash correctives.
    /// </summary>
    [DataContract]
    public class IicReference
    {
        /// <summary>
        /// Iic of the reference.
        /// </summary>
        [DataMember(Order = 10)]
        public string Iic { get; set; }

        /// <summary>
        /// Issue DateTime of the reference.
        /// </summary>
        [DataMember(Order = 20)]
        public DateTime IssueDateTime { get; set; }

        /// <summary>
        /// Amount of the reference.
        /// </summary>
        [DataMember(Order = 30)]
        public decimal Amount { get; set; }
    }
}
