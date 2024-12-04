using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(AnonymousType = true, Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class RemisionRequerimiento
{
    [XmlElement(Order = 0)]
    public required string RefRequerimiento { get; set; }

    [XmlElement(Order = 1)]
    public Booleano? FinRequerimiento { get; set; }
}