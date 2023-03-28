using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    [XmlType("response")]
    public class StatusResponse
    {
        [XmlAttribute(AttributeName = "success")]
        public bool Success { get; set; }

        [XmlAttribute(AttributeName = "code")]
        public string? Code { get; set; }

        [XmlAttribute(AttributeName = "status")]
        public string? Status { get; set; }

        [XmlElement(ElementName = "addInfo")]
        public Printerstatus? Printerstatus { get; set; }
    }

    [XmlType("addInfo")]
    public class Printerstatus
    {
        [XmlElement(ElementName = "cpuRel")]
        public string? CpuRel { get; set; }

        [XmlElement(ElementName = "mfRel")]
        public string? MfRel { get; set; }

        [XmlElement(ElementName = "mfStatus")]
        public string? MfStatus { get; set; }

        [XmlElement(ElementName = "fpStatus")]
        public string? FpStatus { get; set; }

        [XmlElement(ElementName = "rtType")]
        public string? RtType { get; set; }

        [XmlElement(ElementName = "rtMainStatus")]
        public string? MainStatus { get; set; }

        [XmlElement(ElementName = "rtSubStatus")]
        public string? SubStatus { get; set; }

        [XmlElement(ElementName = "rtDailyOpen")]
        public string? DailyOpen { get; set; }

        [XmlElement(ElementName = "rtNoWorkingPeriod")]
        public string? NoWorkingPeriod { get; set; }

        [XmlElement(ElementName = "rtFileToSend")]
        public string? FileToSend { get; set; }

        [XmlElement(ElementName = "rtOldFileToSend")]
        public string? OldFileToSend { get; set; }

        [XmlElement(ElementName = "rtFileRejected")]
        public string? FileRejected { get; set; }

        [XmlElement(ElementName = "rtExpiryCD")]
        public string? ExpiryCD { get; set; }

        [XmlElement(ElementName = "rtExpiryCA")]
        public string? ExpiryCA { get; set; }

        [XmlElement(ElementName = "rtTrainingMode")]
        public string? TrainingMode { get; set; }

        [XmlElement(ElementName = "rtUpgradeResult")]
        public string? UpgradeResult { get; set; }

    }
}
