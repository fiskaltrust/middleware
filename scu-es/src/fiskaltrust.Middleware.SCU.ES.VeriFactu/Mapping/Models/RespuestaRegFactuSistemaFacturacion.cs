using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/RespuestaSuministro.xsd")]
public class RespuestaRegFactuSistemaFacturacion : RespuestaBase
{
    [XmlElement("RespuestaLinea", Order = 0)]
    public RespuestaExpedida[]? RespuestaLinea { get; set; }
    [XmlIgnore]
    public bool RespuestaLineaSpecified => RespuestaLinea is not null;
}