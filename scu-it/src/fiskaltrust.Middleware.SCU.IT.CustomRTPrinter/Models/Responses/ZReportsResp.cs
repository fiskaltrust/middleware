using System;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses
{
    [XmlRoot("zReportsResp")]
    public class ZReportsResp : IResponse
    {
        [XmlElement("zReportExp")]
        public string[] ZReportExp { get; set; }

        [XmlElement("zReportNoResp")]
        public string[] ZReportNoResp { get; set; }

        [XmlElement("zReportFail")]
        public string[] ZReportFail { get; set; }

        [XmlElement("zReport")]
        public string[] ZReport { get; set; }
    }
}