using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/RespuestaSuministro.xsd")]
public class RespuestaExpedida
{
    [XmlElement(Order = 0)]
    public required IDFactura IDFactura { get; set; }
    [XmlElement(Order = 1)]
    public required Operacion Operacion { get; set; }
    [XmlElement(Order = 2)]
    public string? RefExterna { get; set; }
    [XmlElement(Order = 3)]
    public required EstadoRegistro EstadoRegistro { get; set; }
    [XmlElement(DataType = "integer", Order = 4)]
    public string? CodigoErrorRegistro { get; set; }
    [XmlElement(Order = 5)]
    public string? DescripcionErrorRegistro { get; set; }
    [XmlElement(Order = 6)]
    public RegistroDuplicado? RegistroDuplicado { get; set; }
}