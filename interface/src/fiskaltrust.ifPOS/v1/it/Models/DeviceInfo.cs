using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{

    /// <summary>
    /// Details about the operational status of the printer or server
    /// </summary>
    [DataContract]
    public class DeviceInfo
    {
        /// <summary>
        /// Version of firmware, memory etc
        /// </summary>
        [DataMember(Order = 10)]
        public string Version { get; set; }

        /// <summary>
        ///  Indicates the Printer status 
        /// </summary>
        [DataMember(Order = 20)]
        public string DeviceStatus { get; set; }

        /// <summary>
        ///  rtDailyOpen = indicates the logical DAY OPENED logical condition (0=closed{false} and 1=open{true})
        /// </summary>
        [DataMember(Order = 30)]
        public bool DailyOpen { get; set; }

        /// <summary>
        /// rtNoWorkingPeriod indicates whether a Z report must be performed or not (0=no and 1=yes)
        /// </summary>
        [DataMember(Order = 40)]
        public bool ZReportNeeded { get; set; }

        /// <summary>
        /// rtFileToSend indicates the number of files due to be sent to the tax authority
        /// </summary>
        [DataMember(Order = 50)]
        public int FilesToSend { get; set; }

        /// <summary>
        /// rtOldFileToSend indicates the number of files due to be sent to the tax authority but still
        /// waiting on the printer after a configurable number of days(SET 15/25)
        /// </summary>
        [DataMember(Order = 60)]
        public int OldFilesToSend { get; set; }

        /// <summary>
        /// rtFileRejected indicates the number of files rejected by the tax authority
        /// </summary>
        [DataMember(Order = 70)]
        public int FilesRejected { get; set; }

        /// <summary>
        /// rtExpiryCD indicates the device certificate expiry date in the yyyymmdd format
        /// </summary>
        [DataMember(Order = 80)]
        public string ExpireDeviceCertificateDate { get; set; }

        /// <summary>
        /// rtExpiryCA = indicates the tax authority communication certificate expiry date in the yyyymmdd format
        /// </summary>
        [DataMember(Order = 90)]
        public string ExpireTACommunicationCertificateDate { get; set; }

        /// <summary>
        /// indicates the mode *  
        /// </summary>
        [DataMember(Order = 100)]
        public bool TrainingMode { get; set; }

        /// <summary>
        /// indicates the last firmware update outcome *
        /// </summary>
        [DataMember(Order = 110)]
        public string UpgradeResult { get; set; }

        /// <summary>
        /// Serialnumber of the printer
        /// </summary>
        [DataMember(Order = 120)]
        public string SerialNumber { get; set; }
    }
}
