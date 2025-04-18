using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class SistemaInformatico
{
    [XmlElement(Order = 0)]
    public required string NombreRazon { get; set; }
    [XmlElement("IDOtro", typeof(IDOtro), Order = 1)]
    [XmlElement("NIF", typeof(string), Order = 1)]
    public required object Item { get; set; }
    [XmlElement(Order = 2)]
    public required string NombreSistemaInformatico { get; set; }
    [XmlElement(Order = 3)]
    public required string IdSistemaInformatico { get; set; }
    [XmlElement(Order = 4)]
    public required string Version { get; set; }
    [XmlElement(Order = 5)]
    public required string NumeroInstalacion { get; set; }
    [XmlElement(Order = 6)]
    public required Booleano TipoUsoPosibleSoloVerifactu { get; set; }
    [XmlElement(Order = 7)]
    public required Booleano TipoUsoPosibleMultiOT { get; set; }
    [XmlElement(Order = 8)]
    public required Booleano IndicadorMultiplesOT { get; set; }
}