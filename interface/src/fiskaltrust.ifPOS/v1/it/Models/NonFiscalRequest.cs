using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// Request for non fiscal print (e.g. Multiuse Vouchers)
    /// </summary>
    [DataContract]
    public class NonFiscalRequest
    {
        /// <summary>
        /// Operator
        /// </summary>
        [DataMember(Order = 10)]
        public string Operator { get; set; }

        /// <summary>
        /// NonFiscalPrint
        /// </summary>
        [DataMember(Order = 20)]
        public List<NonFiscalPrint> NonFiscalPrints { get; set; }

    }

    /// <summary>
    /// Request for non fiscal print (e.g. Multiuse Vouchers)
    /// </summary>
    [DataContract]
    public class NonFiscalPrint
    {
        /// <summary>
        /// Font
        /// </summary>
        [DataMember(Order = 10)]
        public int Font { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        [DataMember(Order = 20)]
        public string Data { get; set; }
    }
}
