using System;
using System.Globalization;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses
{
    [XmlRoot("lastIdCmd")]
    public class LastIdCmd
    {
        [XmlAttribute("IdCmd")]
        public string IdCmd { get; set; }

        [XmlText]
        public string LastCmd { get; set; }
    }
}

