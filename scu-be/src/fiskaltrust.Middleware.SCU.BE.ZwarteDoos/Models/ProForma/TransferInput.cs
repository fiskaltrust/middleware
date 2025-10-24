using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.ProForma;

public class TransferInput 
{
    [JsonPropertyName("from")]
    public required List<TransferItemInput> From { get; set; }

    [JsonPropertyName("to")]
    public required List<TransferItemInput> To { get; set; }
}
