using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class OtherDeliveryNoteHeaderOverride
{
    [JsonPropertyName("loadingAddress")]
    public AddressOverride? LoadingAddress { get; set; }

    [JsonPropertyName("deliveryAddress")]
    public AddressOverride? DeliveryAddress { get; set; }

    [JsonPropertyName("startShippingBranch")]
    public int? StartShippingBranch { get; set; }

    [JsonPropertyName("completeShippingBranch")]
    public int? CompleteShippingBranch { get; set; }
}
