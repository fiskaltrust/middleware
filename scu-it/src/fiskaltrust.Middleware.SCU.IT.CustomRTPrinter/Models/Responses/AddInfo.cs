using System;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses
{
    [XmlRoot("addInfo")]
    public class AddInfo<T>
        where T : IResponse
    {
        [XmlElement("elementList")]
        public string ElementList { get; set; }

        [XmlElement("lastIdCmd")]
        public string LastIdCmd { get; set; }

        [XmlElement("responseBuf")]
        public T ResponseBuf { get; set; }

        [XmlElement("lastCommand")]
        public string LastCommand { get; set; }

        [XmlElement("dateTime")]
        public string DateTime { get; set; }

        [XmlElement("printerStatus")]
        public string PrinterStatus { get; set; }

        [XmlElement("fpStatus")]
        public string FpStatus { get; set; }

        [XmlElement("receiptStep")]
        public string ReceiptStep { get; set; }

        [XmlElement("nClose")]
        public string NClose { get; set; }

        [XmlElement("iscalDoc")]
        public string IscalDoc { get; set; }

        [XmlElement("notFiscalDoc")]
        public string NotFiscalDoc { get; set; }

        [XmlElement("cpuRel")]
        public string? CpuRel { get; set; }

        [XmlElement("mfStatus")]
        public string? MfStatus { get; set; }
    }
}

