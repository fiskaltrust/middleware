using fiskaltrust.ifPOS.v1.errors;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// Basic Response
    /// </summary>
    [DataContract]
    public class Response
    {
        /// <summary>
        /// Indicating success
        /// </summary>
        [DataMember(Order = 10)]
        public bool Success { get; set; }

        /// <summary>
        /// Information on Error
        /// </summary>
        [DataMember(Order = 20)]
        public SSCDErrorInfo SSCDErrorInfo { get; set; }
    }
}
