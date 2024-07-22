using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("getZRaports")]
    public class GetZReports : ICommand
    {
        public GetZReports()
        {
        }

        public GetZReports(DateTime date)
        {
            Date = date;
        }

        [XmlAttribute("date")]
        public string DateString { get; set; }

        [XmlIgnore]
        public DateTime Date
        {
            get => DateTime.ParseExact(DateString, "ddMMYYYY", System.Globalization.CultureInfo.InvariantCulture);
            set => DateString = value.ToString("ddMMYYYY");
        }
    }
}