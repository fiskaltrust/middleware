using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuModels;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class IDFacturaExpedidaBaja
{
    [XmlElement(Order = 0)]
    public required string IDEmisorFacturaAnulada { get; set; }

    [XmlElement(Order = 1)]
    public required string NumSerieFacturaAnulada { get; set; }

    [XmlElement(Order = 2)]
    public required string FechaExpedicionFacturaAnulada { get; set; }
}