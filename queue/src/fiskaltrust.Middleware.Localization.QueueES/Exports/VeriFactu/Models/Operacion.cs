using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class Operacion
{
    [XmlElement(Order = 0)]
    public required TipoOperacion TipoOperacion { get; set; }

    [XmlElement(Order = 1)]
    public Booleano? Subsanacion { get; set; }

    [XmlElement(Order = 2)]
    public RechazoPrevio? RechazoPrevio { get; set; }

    [XmlElement(Order = 3)]
    public Booleano? SinRegistroPrevio { get; set; }
}