using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class PersonaFisicaJuridica
{
    [XmlElement(Order = 0)]
    public required string NombreRazon { get; set; }

    [XmlElement("IDOtro", typeof(IDOtro), Order = 1)]
    [XmlElement("NIF", typeof(string), Order = 1)]
    public required object Item { get; set; }
}