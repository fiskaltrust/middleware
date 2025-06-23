using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class IDOtro
{
    [XmlElement(Order = 0)]
    public Country? CodigoPais { get; set; }
    [XmlIgnore]
    public bool CodigoPaisSpecified => CodigoPais is not null;

    [XmlElement(Order = 1)]
    public required PersonaFisicaJuridicaIDType IDType { get; set; }

    [XmlElement(Order = 2)]
    public required string ID { get; set; }
}