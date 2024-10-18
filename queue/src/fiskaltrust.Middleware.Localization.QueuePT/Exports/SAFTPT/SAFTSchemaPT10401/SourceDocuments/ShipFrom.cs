using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
#pragma warning disable
[XmlRoot(ElementName = "ShipFrom")]
public class ShipFrom
{
    [XmlElement(ElementName = "DeliveryID")]
    public string? DeliveryID { get; set; }

    [XmlIgnore]
    public DateTime? DeliveryDate { get; set; }

    [XmlElement(ElementName = "DeliveryDate", IsNullable = false)]
    public object DeliveryDateProperty
    {
        get => DeliveryDate;
        set
        {
            if (value == null)
            {
                DeliveryDate = null;
            }
            else
            {
                DeliveryDate = DateTime.Parse(value.ToString());
            }
        }
    }

    [XmlElement(ElementName = "WarehouseID")]
    public string? WarehouseID { get; set; }

    [XmlElement(ElementName = "LocationID")]
    public string? LocationID { get; set; }

    [XmlElement(ElementName = "Address")]
    public Address? Address { get; set; }
}

