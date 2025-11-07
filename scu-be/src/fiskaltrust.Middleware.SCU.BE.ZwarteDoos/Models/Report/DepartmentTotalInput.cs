using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Report;

public class DepartmentTotalInput
{
    [JsonPropertyName("departmentId")]
    public required string DepartmentId { get; set; }

    [JsonPropertyName("departmentName")]
    public required string DepartmentName { get; set; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }
}
