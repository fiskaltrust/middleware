using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocuments;
#pragma warning disable
[XmlRoot(ElementName = "ShipTo")]
public class ShipTo
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

