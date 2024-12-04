using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class DesgloseRectificacion
{
    [XmlElement(Order = 0)]
    public required string BaseRectificada { get; set; }

    [XmlElement(Order = 1)]
    public required string CuotaRectificada { get; set; }

    [XmlElement(Order = 2)]
    public string? CuotaRecargoRectificado { get; set; }
}