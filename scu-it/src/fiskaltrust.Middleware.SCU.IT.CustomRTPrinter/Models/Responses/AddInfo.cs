using System;
using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses
{
    [XmlRoot("addInfo")]
    public class AddInfo<T>
    {
        [XmlElement("elementList")]
        public string ElementList { get; set; }

        [XmlElement("lastIdCmd")]
        public LastIdCmd LastIdCmd { get; set; }

        [XmlElement("responseBuf")]
        public T ResponseBuf { get; set; }

        [XmlElement("lastCommand")]
        public string LastCommand { get; set; }

        [XmlElement("dateTime")]
        public string DateTimeString { get; set; }

        [XmlIgnore]
        public DateTime DateTime
        {
            get => DateTime.ParseExact(DateTimeString, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            set => DateTimeString = value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        [XmlElement("printerStatus")]
        public int PrinterStatus { get; set; }

        [XmlElement("fpStatus")]
        public int FpStatus { get; set; }

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

