using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class RegistroDuplicado
{
    [XmlElement(Order = 0)]
    public required string IdPeticionRegistroDuplicado { get; set; }

    [XmlElement(Order = 1)]
    public required EstadoRegistroSF EstadoRegistroDuplicado { get; set; }

    [XmlElement(DataType = "integer", Order = 2)]
    public string? CodigoErrorRegistro { get; set; }
    [XmlIgnore]
    public bool CodigoErrorRegistroSpecified => CodigoErrorRegistro is not null;

    [XmlElement(Order = 3)]
    public string? DescripcionErrorRegistro { get; set; }
    [XmlIgnore]
    public bool DescripcionErrorRegistroSpecified => DescripcionErrorRegistro is not null;
}