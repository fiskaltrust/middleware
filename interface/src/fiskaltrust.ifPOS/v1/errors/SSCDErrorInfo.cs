using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace fiskaltrust.ifPOS.v1.errors
{
    /// <summary>
    /// Sale items on a commercial sale document.
    /// </summary>
    [DataContract]
    public enum SSCDErrorType
    {
        /// <summary>
        /// General Error
        /// </summary>
        [EnumMember]
        General = 0,
        /// <summary>
        /// Connection Error
        /// </summary>
        [EnumMember]
        Connection = 1,
        /// <summary>
        /// Device Error
        /// </summary>
        [EnumMember]
        Device = 2
    }

    /// <summary>
    /// SSCDErrorInfo
    /// </summary>
    [DataContract]
    public class SSCDErrorInfo
    {
        /// <summary>
        /// SSCDErrorType
        /// </summary>
        [DataMember(Order = 10)]
        public SSCDErrorType Type { get; set; }
        /// <summary>
        /// SSCD Error Info
        /// </summary>
        [DataMember(Order = 20)]
        public string Info { get; set; }
    }
}