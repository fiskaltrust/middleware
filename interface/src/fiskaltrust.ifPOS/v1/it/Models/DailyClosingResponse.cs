using fiskaltrust.ifPOS.v1.errors;
using System;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// The response generated from the data the server or printer returned after a daily closing was executed
    /// </summary>
    [DataContract]
    public class DailyClosingResponse
    {
        /// <summary>
        /// Indicating success
        /// </summary>
        [DataMember(Order = 10)]
        public bool Success { get; set; }

        /// <summary>
        /// Information on the error, if any occurred
        /// </summary>
        [DataMember(Order = 20)]
        public SSCDErrorInfo SSCDErrorInfo { get; set; }

        /// <summary>
        /// The device's current status
        /// </summary>
        [DataMember(Order = 30)]
        public string DeviceStatus { get; set; }

        /// <summary>
        /// The daily amount that was processed since the last daily closing
        /// </summary>
        [DataMember(Order = 40)]
        public decimal DailyAmount { get; set; }

        /// <summary>
        /// The ascending daily closing number
        /// </summary>
        [DataMember(Order = 50)]
        public long ZRepNumber { get; set; }

        /// <summary>
        /// ZRecord data of the daily closing receipt
        /// </summary>
        [DataMember(Order = 60)]
        public string ReportDataJson { get; set; }
    }
}
