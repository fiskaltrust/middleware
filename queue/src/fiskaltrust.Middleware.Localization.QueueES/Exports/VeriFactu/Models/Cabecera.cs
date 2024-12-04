using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public partial class Cabecera
{
    [XmlElement(Order = 0)]
    public required PersonaFisicaJuridicaES ObligadoEmision { get; set; }

    [XmlElement(Order = 1)]
    public PersonaFisicaJuridicaES? Representante { get; set; }

    [XmlElement(Order = 2)]
    public RemisionVoluntaria? RemisionVoluntaria { get; set; }

    [XmlElement(Order = 3)]
    public RemisionRequerimiento? RemisionRequerimiento { get; set; }
}
