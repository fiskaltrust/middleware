using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuModels;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public enum PersonaFisicaJuridicaIDType
{
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
}