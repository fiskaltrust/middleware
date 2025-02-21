using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{

    /// <summary>
    /// Details about the operational status of the printer or server
    /// </summary>
    [DataContract]
    public class RTInfo
    {
        /// <summary>
        /// Contains a json serialized object with generic info for the given device
        /// </summary>
        [DataMember(Order = 10)]
        public string InfoData { get; set; } 

        /// <summary>
        /// Serialnumber of the printer
        /// </summary>
        [DataMember(Order = 20)]
        public string SerialNumber { get; set; }
    }
}
