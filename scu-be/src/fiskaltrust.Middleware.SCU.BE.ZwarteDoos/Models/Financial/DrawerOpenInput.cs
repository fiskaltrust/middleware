using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Financial;

public class DrawerOpenInput : BaseInputData
{
    [JsonPropertyName("drawer")]
    public required DrawerInput Drawer { get; set; }
}
