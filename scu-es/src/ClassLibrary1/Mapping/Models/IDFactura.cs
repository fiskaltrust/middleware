using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public partial class IDFactura
{
    [XmlElement(Order = 0)]
    public required string IDEmisorFactura { get; set; }

    [XmlElement(Order = 1)]
    public required string NumSerieFactura { get; set; }

    [XmlElement(Order = 2)]
    public required string FechaExpedicionFactura { get; set; }
}
