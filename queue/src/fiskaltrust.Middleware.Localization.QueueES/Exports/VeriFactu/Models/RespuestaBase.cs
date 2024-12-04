using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/RespuestaSuministro.xsd")]
public class RespuestaBase
{
    [XmlElement(Order = 0)]
    public string? CSV { get; set; }
    [XmlElement(Order = 1)]
    public DatosPresentacion? DatosPresentacion { get; set; }
    [XmlElement(Order = 2)]
    public required Cabecera Cabecera { get; set; }
    [XmlElement(Order = 3)]
    public required string TiempoEsperaEnvio { get; set; }
    [XmlElement(Order = 4)]
    public required EstadoEnvio EstadoEnvio { get; set; }
}