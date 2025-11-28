using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public enum Impuesto
{
    [XmlEnum("01")]
    Item01,

    [XmlEnum("02")]
    Item02,

    [XmlEnum("03")]
    Item03,

    [XmlEnum("05")]
    Item05,
}