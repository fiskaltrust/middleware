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

        private static readonly string[] _dateFormats = { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy" };

        [XmlIgnore]
        public DateTime DateTime
        {
            get
            {
                if (string.IsNullOrEmpty(DateTimeString))
                    return System.DateTime.UtcNow;
                return System.DateTime.TryParseExact(DateTimeString, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    ? dt
                    : System.DateTime.UtcNow;
            }
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

        [XmlElement("fiscalDoc")]
        public string FiscalDoc { get; set; }

        [XmlElement("notFiscalDoc")]
        public string NotFiscalDoc { get; set; }

        [XmlElement("cpuRel")]
        public string? CpuRel { get; set; }

        [XmlElement("mfStatus")]
        public string? MfStatus { get; set; }
    }
}

