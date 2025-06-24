using System.Xml.Linq;
using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.ES.Models;

namespace fiskaltrust.Middleware.SCU.ES.Soap;

[Serializable]
[XmlRoot("Fault")]
public class Fault
{
    [XmlElement("faultcode", Namespace = "")]
    public required string FaultCode { get; set; }

    [XmlElement("faultstring", Namespace = "")]
    public required string FaultString { get; set; }

    [XmlElement("detail", Namespace = "")]
    public required DetailType Detail { get; set; }

    [Serializable]
    [XmlRoot("detail", Namespace = "")]
    public class DetailType
    {
        [XmlElement("errorcode", IsNullable = true)]
        public required int? ErrorCode { get; set; }

        [XmlElement("callstack")]
        public required string CallStack { get; set; }
        // }
    }
}

[Serializable]
[XmlRoot("Body")]
public class ResponseBody
{
    [XmlElement("Fault", Type = typeof(Fault))]
    [XmlElement("RespuestaRegFactuSistemaFacturacion", Type = typeof(RespuestaRegFactuSistemaFacturacion), Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/RespuestaSuministro.xsd")]
    public required object Content { get; set; }
}
