using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class PersonaFisicaJuridicaES
{
    [XmlElement(Order = 0)]
    public required string NombreRazon { get; set; }

    [XmlElement(Order = 1)]
    public required string NIF { get; set; }
}