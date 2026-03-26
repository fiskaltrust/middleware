using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class OtherDeliveryNoteHeaderTypeOverride
{
    [JsonPropertyName("loadingAddress")]
    public AddressTypeOverride? LoadingAddress { get; set; }

    [JsonPropertyName("deliveryAddress")]
    public AddressTypeOverride? DeliveryAddress { get; set; }

    [JsonPropertyName("startShippingBranch")]
    public int? StartShippingBranch { get; set; }

    [JsonPropertyName("completeShippingBranch")]
    public int? CompleteShippingBranch { get; set; }
}
