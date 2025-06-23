using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public enum IdOperacionesTrascendenciaTributaria
{
    [XmlEnum("01")]
    Item01,

    [XmlEnum("02")]
    Item02,

    [XmlEnum("03")]
    Item03,

    [XmlEnum("04")]
    Item04,

    [XmlEnum("05")]
    Item05,

    [XmlEnum("06")]
    Item06,

    [XmlEnum("07")]
    Item07,

    [XmlEnum("08")]
    Item08,

    [XmlEnum("09")]
    Item09,

    [XmlEnum("10")]
    Item10,

    [XmlEnum("11")]
    Item11,

    [XmlEnum("14")]
    Item14,

    [XmlEnum("15")]
    Item15,

    [XmlEnum("17")]
    Item17,

    [XmlEnum("18")]
    Item18,

    [XmlEnum("19")]
    Item19,

    [XmlEnum("20")]
    Item20,
}