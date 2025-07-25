using System.Runtime.Serialization;
using System.Text.Json.Serialization;

#pragma warning disable
namespace fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;

public class GovernmentApiData
{
    [JsonPropertyName("Protocol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required string Protocol { get; set; }

    [JsonPropertyName("ProtocolVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? ProtocolVersion { get; set; }

    [JsonPropertyName("Action")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [DataMember(EmitDefaultValue = true, IsRequired = true)]
    public required string Action { get; set; }

    [JsonPropertyName("ProtocolRequest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string ProtocolRequest { get; set; }

    [JsonPropertyName("ProtocolResponse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DataMember(EmitDefaultValue = false, IsRequired = false)]
    public string? ProtocolResponse { get; set; }
}
